using VDStudios.MagicEngine.Input;

namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// A <see cref="GraphicsManager{TGraphicsContext}"/> feature
/// </summary>
/// <typeparam name="TGraphicsContext"></typeparam>
public interface IGraphicsManagerFeature<TGraphicsContext> : IDisposable
    where TGraphicsContext : GraphicsContext<TGraphicsContext>
{
    /// <summary>
    /// Called when the feature is added to <paramref name="graphicsManager"/>
    /// </summary>
    /// <param name="graphicsManager"></param>
    public void FeatureAdded(GraphicsManager<TGraphicsContext> graphicsManager);

    /// <summary>
    /// Called when the GraphicsManager that has this feature starts being run, right after <see cref="GraphicsManager{TGraphicsContext}.BeforeRun"/>
    /// </summary>
    public void ManagerStarting(GraphicsManager<TGraphicsContext> graphicsManager);

    /// <summary>
    /// Called the feature right after the rest of the frame is drawn, but before its submitted
    /// </summary>
    public void Rendering(TimeSpan delta);

    /// <summary>
    /// Called just before the event subscribers are distributed the snapshot
    /// </summary>
    public void Input(InputSnapshot inputSnapshot);
}
