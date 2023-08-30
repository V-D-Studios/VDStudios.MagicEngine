using System.Diagnostics.CodeAnalysis;
using VDStudios.MagicEngine.Internal;

namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// A queue collection that manages a set of <see cref="DrawOperation{TGraphicsContext}"/>
/// </summary>
/// <typeparam name="TGraphicsContext">The <see cref="GraphicsContext{TSelf}"/> that this <see cref="IDrawQueue{TGraphicsContext}"/> uses</typeparam>
public interface IDrawQueue<TGraphicsContext> : IDrawQueueEnqueuer<TGraphicsContext>
    where TGraphicsContext : GraphicsContext<TGraphicsContext>
{
    /// <summary>
    /// Clears the <see cref="IDrawQueue{TGraphicsContext}"/> of any elements
    /// </summary>
    public void Clear();

    /// <summary>
    /// Gets the current amount of queued <see cref="DrawOperation{TGraphicsContext}"/>s
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Attempts to dequeue a <see cref="DrawOperation{TGraphicsContext}"/> from the queue
    /// </summary>
    /// <param name="drawOperation">The draw operation that was dequeued</param>
    /// <returns><see langword="true"/> if the operation was succesful and <paramref name="drawOperation"/> was populated. <see langword="false"/> if the queue is empty or it otherwise fails</returns>
    public bool TryDequeue([NotNullWhen(true)][MaybeNullWhen(false)] out DrawOperation<TGraphicsContext>? drawOperation);

    /// <summary>
    /// Attempts to dequeue a <see cref="DrawOperation{TGraphicsContext}"/> from the queue
    /// </summary>
    /// <param name="ct">The <see cref="CancellationToken"/> to observe</param>
    public ValueTask<SuccessResult<DrawOperation<TGraphicsContext>>> TryDequeueAsync(CancellationToken ct = default);
}
