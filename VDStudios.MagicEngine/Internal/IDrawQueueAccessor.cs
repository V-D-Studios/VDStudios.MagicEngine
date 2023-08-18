using System.Diagnostics.CodeAnalysis;
using VDStudios.MagicEngine.Graphics;

namespace VDStudios.MagicEngine.Internal;

/// <summary>
/// Provides a contract to access an otherwise read-only <see cref="IDrawQueue{TGraphicsContext}"/>
/// </summary>
/// <remarks>
/// This interface is meant to be used only by custom implementations of <see cref="IDrawQueue{TGraphicsContext}"/> where <see cref="DrawQueue{TGraphicsContext}"/> is insufficient. Usually, this interface is implemented by the same class that implements <see cref="IDrawQueue{TGraphicsContext}"/>
/// </remarks>
/// <typeparam name="TGraphicsContext">The <see cref="GraphicsContext{TSelf}"/> that this <see cref="IDrawQueueAccessor{TGraphicsContext}"/> uses</typeparam>
public interface IDrawQueueAccessor<TGraphicsContext>
    where TGraphicsContext : GraphicsContext<TGraphicsContext>
{
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
