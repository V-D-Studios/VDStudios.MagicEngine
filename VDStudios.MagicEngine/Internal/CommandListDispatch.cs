using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace VDStudios.MagicEngine.Internal;

/// <summary>
/// Represents a collection of <see cref="DrawOperation"/>s that are to be rendered in the current frame
/// </summary>
internal class CommandListDispatch
{
    private DrawOperation[] dops;
    private int DopCount = 0;

    private readonly CommandList cl;
    private readonly SemaphoreSlim sem;
    private readonly WaitCallback WorkMethod_del;

    private TimeSpan delta;
    private Exception? Fault;
    
    /// <param name="expectedDops">A hint to the amount of <see cref="DrawOperation"/> that is expected for this <see cref="CommandListDispatch"/> to handle per frame</param>
    /// <param name="cl">The <see cref="CommandList"/> belonging to this <see cref="CommandListDispatch"/></param>
    public CommandListDispatch(int expectedDops, CommandList cl)
    {
        dops = new DrawOperation[expectedDops];
        this.cl = cl;
        WorkMethod_del = WorkMethod;
        sem = new(1, 1);
    }

    public void Add(DrawOperation dop)
    {
        if (DopCount + 1 >= dops.Length)
            Array.Resize(ref dops, int.Max(dops.Length * 2, 6));
        dops[DopCount++] = dop;
    }

    private void WorkMethod(object? state)
    {
        var l_dops = dops;
        int taskCount = DopCount;
        var tasks = ArrayPool<ValueTask>.Shared.Rent(taskCount);
        try
        {
            cl.Begin();
            int i = 0;
            for (; i < DopCount && i < tasks.Length; i++)
                tasks[i] = l_dops[i].InternalDraw(delta, cl).Preserve();
            while (i-- > 0)
                tasks[i].GetAwaiter().GetResult();
        }
        catch(Exception e)
        {
            Fault = e;
        }
        finally
        {
            ArrayPool<ValueTask>.Shared.Return(tasks, true);
            cl.End();
            sem.Release();
        }
    }

    public CommandList WaitForEnd()
    {
        sem.Wait();
        try
        {
            if (Fault is Exception e)
            {
                Fault = null;
                throw e;
            }
            Array.Clear(dops);
            DopCount = 0;
            return cl;
        }
        finally
        {
            sem.Release();
        }
    }

    public void Start(TimeSpan delta)
    {
        if (DopCount is 0)
            return;
        this.delta = delta;
        sem.Wait();
        try
        {
            if (!ThreadPool.QueueUserWorkItem(WorkMethod_del))
                sem.Release();
        }
        catch (SemaphoreFullException)
        {
            throw;
        }
        catch
        {
            sem.Release();
            throw;
        }
    }
}
