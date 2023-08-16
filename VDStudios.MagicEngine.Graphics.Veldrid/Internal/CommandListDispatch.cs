using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using VDStudios.MagicEngine.Graphics;
using Veldrid;

namespace VDStudios.MagicEngine.Graphics.Veldrid.Internal;

/// <summary>
/// Represents a collection of <see cref="DrawOperation"/>s that are to be rendered in the current frame
/// </summary>
internal class CommandListDispatch
{
    private DrawOperation[] dops;
    private ArraySegment<RenderTargetState>? Targets;
    private int DopCount = 0;

    private readonly CommandList cl;
#if !FORCE_GM_NOPARALLEL
    private readonly SemaphoreSlim sem;
    private readonly WaitCallback WorkMethod_del;
#endif

    private TimeSpan delta;
    private Exception? Fault;

    /// <param name="expectedDops">A hint to the amount of <see cref="DrawOperation"/> that is expected for this <see cref="CommandListDispatch"/> to handle per frame</param>
    /// <param name="cl">The <see cref="CommandList"/> belonging to this <see cref="CommandListDispatch"/></param>
    public CommandListDispatch(int expectedDops, CommandList cl)
    {
        dops = new DrawOperation[expectedDops];
        this.cl = cl;
#if !FORCE_GM_NOPARALLEL
        WorkMethod_del = WorkMethod;
        sem = new(1, 1);
#endif
    }

    /// <summary>
    /// Sets the targets for the upcoming draw operations
    /// </summary>
    /// <param name="targets">The framebuffer targets the draw operation will be rendered to</param>
    public void SetTargets(ArraySegment<RenderTargetState> targets)
    {
        Debug.Assert(targets.Count > 0, "no targets were passed to the CommandListDispatch");
        Debug.Assert(targets.All(x => x.ActiveBuffer is not null && x.Target is not null), "some framebuffer targets are null");
        Targets = targets;
    }

    public void Add(DrawOperation dop)
    {
        if (DopCount + 1 >= dops.Length)
            Array.Resize(ref dops, int.Max(dops.Length * 2, 6));
        dops[DopCount++] = dop;
    }

    private void WorkMethod(object? state)
    {
        if (Targets is not ArraySegment<RenderTargetState> targets)
            throw new InvalidOperationException("Cannot begin dispatching CommandLists without targets. Was SetTargets called this frame?");

        var l_dops = dops;
        int taskCount = DopCount * targets.Count;
        var tasks = ArrayPool<ValueTask>.Shared.Rent(taskCount);
        try
        {
            cl.Begin();

            for (int i = 0; i < DopCount; i++)
                for (int t = 0; t < targets.Count; t++)
                    tasks[i] = l_dops[i].InternalDraw(delta, cl, new FramebufferTargetInfo(t, targets.Count, targets[t].ActiveBuffer, targets[i].Parameters)).Preserve();

            for (int ti = 0; ti < taskCount; ti++)
                tasks[ti].GetAwaiter().GetResult();
        }
        catch (Exception e)
        {
            Fault = e;
            throw;
        }
        finally
        {
            ArrayPool<ValueTask>.Shared.Return(tasks, true);
            cl.End();
            Targets = null;
#if !FORCE_GM_NOPARALLEL
            sem.Release();
#endif
        }
    }

    public CommandList WaitForEnd()
    {
#if !FORCE_GM_NOPARALLEL
        sem.Wait();
        try
        {
            if (Fault is Exception e)
            {
                try
                {
                    throw e;
                }
                finally
                {
                    Fault = null;
                }
            }
            Array.Clear(dops);
            DopCount = 0;
            return cl;
        }
        finally
        {
            sem.Release();
        }
#else
        WorkMethod(null);
        if (Fault is Exception e)
        {
            Fault = null;
            throw e;
        }
        Array.Clear(dops);
        DopCount = 0;
        return cl;
#endif
    }

    public void Start(TimeSpan delta)
    {
        if (DopCount is 0)
            return;
        this.delta = delta;
#if !FORCE_GM_NOPARALLEL
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
#endif
    }
}
