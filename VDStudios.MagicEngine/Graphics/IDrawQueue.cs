namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// The engine's Draw Queue
/// </summary>
public interface IDrawQueue<T, TGraphicsContext> where T : GraphicsObject<TGraphicsContext>
    where TGraphicsContext : GraphicsContext<TGraphicsContext>
{
    /// <summary>
    /// Enqueues a ready-to-draw object into the Draw Queue asynchronously
    /// </summary>
    /// <param name="drawing">The object that is ready to draw</param>
    /// <param name="priority">The priority of the object. Higher numbers will draw first and will appear below lower numbers</param>
    public Task EnqueueAsync(T drawing, float priority);

    /// <summary>
    /// Asynchronously enqueues a ready-to-draw object into the Draw Queue
    /// </summary>
    /// <param name="drawing">The object that is ready to draw</param>
    /// <param name="priority">The priority of the object. Higher numbers will draw first and will appear below lower numbers</param>
    public void Enqueue(T drawing, float priority);
}
