namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// An interface that allows write-only access to a <see cref="IDrawQueue{TGraphicsContext}"/>
/// </summary>
/// <remarks>
/// While casting a reference of this type back to <see cref="IDrawQueue{TGraphicsContext}"/> is possible, directly manipulating, or rather, dequeing the <see cref="IDrawQueue{TGraphicsContext}"/> is not recommended
/// </remarks>
/// <typeparam name="TGraphicsContext">The <see cref="GraphicsContext{TSelf}"/> that this <see cref="IDrawQueueEnqueuer{TGraphicsContext}"/> uses</typeparam>
public interface IDrawQueueEnqueuer<TGraphicsContext> 
    where TGraphicsContext : GraphicsContext<TGraphicsContext>
{
    /// <summary>
    /// Enqueues a ready-to-draw object into the Draw Queue asynchronously
    /// </summary>
    /// <param name="drawing">The object that is ready to draw</param>
    /// <param name="priority">The priority of the object. Higher numbers will draw first and will appear below lower numbers</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to observe</param>
    public ValueTask EnqueueAsync(DrawOperation<TGraphicsContext> drawing, float priority, CancellationToken ct = default);

    /// <summary>
    /// Enqueues a ready-to-draw object into the Draw Queue
    /// </summary>
    /// <param name="drawing">The object that is ready to draw</param>
    /// <param name="priority">The priority of the object. Higher numbers will draw first and will appear below lower numbers</param>
    public void Enqueue(DrawOperation<TGraphicsContext> drawing, float priority);
}
