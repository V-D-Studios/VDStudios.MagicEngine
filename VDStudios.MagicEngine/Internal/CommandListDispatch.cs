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
    private readonly List<DrawOperation> dops;
    private readonly CommandList cl;
    private readonly SemaphoreSlim sem;
    private readonly WaitCallback WorkMethod_del;

    private Exception? Fault;
    
    /// <param name="expectedDops">A hint to the amount of <see cref="DrawOperation"/> that is expected for this <see cref="CommandListDispatch"/> to handle per frame</param>
    /// <param name="cl">The <see cref="CommandList"/> belonging to this <see cref="CommandListDispatch"/></param>
    public CommandListDispatch(int expectedDops, CommandList cl)
    {
        dops = new(expectedDops);
        this.cl = cl;
        WorkMethod_del = WorkMethod;
        sem = new(1, 1);
    }

    public void Add(DrawOperation dop) => dops.Add(dop);

    private void WorkMethod(object? state)
    {
        var tasks = ArrayPool<ValueTask>.Shared.Rent(dops.Count);
        sem.Wait();
        try
        {
            var sp_dops = CollectionsMarshal.AsSpan(dops);
            for (int i = 0; i < sp_dops.Length; i++) ;
                #warning put 'em to work
        }
        catch(Exception e)
        {
            Fault = e;
        }
        finally
        {
            sem.Release();
        }
    }

    public void Check()
    {
        sem.Wait();
        try
        {
            if (Fault is Exception e)
                throw e;
        }
        finally
        {
            sem.Release();
        }
    }

    public void Run()
    {
        ThreadPool.QueueUserWorkItem(WorkMethod_del);
    }
}
