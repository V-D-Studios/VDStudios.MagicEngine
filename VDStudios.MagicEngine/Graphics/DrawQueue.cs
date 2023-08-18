using System.Diagnostics.CodeAnalysis;
using VDStudios.MagicEngine.Internal;

namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// A default implementation for a <see cref="IDrawQueue{TGraphicsContext}"/> backed by a <see cref="PriorityQueue{TElement, TPriority}"/>
/// </summary>
/// <typeparam name="TGraphicsContext">The <see cref="GraphicsContext{TSelf}"/> that this <see cref="DrawQueue{TGraphicsContext}"/> uses</typeparam>
public class DrawQueue<TGraphicsContext> : IDrawQueue<TGraphicsContext>, IDrawQueueAccessor<TGraphicsContext>
    where TGraphicsContext : GraphicsContext<TGraphicsContext>
{
    private readonly PriorityQueue<DrawOperation<TGraphicsContext>, float> queue = new();
    private readonly SemaphoreSlim sem = new(1, 1); // Can't use ReaderWriterLock because for queues, read operations are also writes

    /// <inheritdoc/>
    public async ValueTask EnqueueAsync(DrawOperation<TGraphicsContext> drawing, float priority, CancellationToken ct = default)
    {
        if (sem.Wait(50, ct) is false)
            await sem.WaitAsync(ct);
        try
        {
            queue.Enqueue(drawing, priority);
        }
        finally
        {
            sem.Release();
        }
    }

    /// <inheritdoc/>
    public void Enqueue(DrawOperation<TGraphicsContext> drawing, float priority)
    {
        sem.Wait();
        try
        {
            queue.Enqueue(drawing, priority);
        }
        finally
        {
            sem.Release();
        }
    }

    /// <inheritdoc/>
    public async ValueTask<SuccessResult<DrawOperation<TGraphicsContext>>> TryDequeueAsync(CancellationToken ct = default)
    {
        if (sem.Wait(50, ct) is false)
            await sem.WaitAsync(ct);
        try
        {
            return queue.TryDequeue(out var drawing, out _)
                ? new SuccessResult<DrawOperation<TGraphicsContext>>(drawing, true)
                : SuccessResult<DrawOperation<TGraphicsContext>>.Failure;
        }
        finally
        {
            sem.Release();
        }
    }

    /// <inheritdoc/>
    public bool TryDequeue([MaybeNullWhen(false), NotNullWhen(true)] out DrawOperation<TGraphicsContext>? drawOperation)
    {
        sem.Wait();
        try
        {
            return queue.TryDequeue(out drawOperation, out _);
        }
        finally
        {
            sem.Release();
        }
    }
}