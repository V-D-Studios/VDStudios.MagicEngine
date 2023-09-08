using Veldrid;

namespace VDStudios.MagicEngine.Graphics.Veldrid;

/// <summary>
/// Represents a resource that can be shared across multiple <see cref="DrawOperation{TGraphicsContext}"/>s, and as such should be updated independently
/// </summary>
public abstract class SharedDrawResource : GraphicsObject<VeldridGraphicsContext>
{
    /// <summary>
    /// Instances a new <see cref="SharedDrawResource"/> object
    /// </summary>
    public SharedDrawResource(Game game) : base(game, "resources")
    {
    }

    /// <summary>
    /// Flags this <see cref="SharedDrawResource"/> as needing to update GPU data the next frame
    /// </summary>
    /// <remarks>
    /// Multiple calls to this method will *not* result in this <see cref="SharedDrawResource"/> being updated multiple times
    /// </remarks>
    public void NotifyPendingUpdate() => PendingGpuUpdate = true;
    internal bool PendingGpuUpdate { get; private set; }

    /// <summary>
    /// Updates the GPU state of this Draw Resource
    /// </summary>
    /// <remarks>
    /// This method is called automatically on each frame where this <see cref="SharedDrawResource"/> is slated for updating
    /// </remarks>
    public abstract void UpdateGPUState(VeldridGraphicsContext context, CommandList commandList);
}
