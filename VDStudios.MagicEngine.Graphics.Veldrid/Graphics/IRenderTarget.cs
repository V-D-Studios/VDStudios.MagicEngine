using Veldrid;

namespace VDStudios.MagicEngine.Graphics.Veldrid.Graphics;

/// <summary>
/// Represents a Render Target for a GraphicsManager
/// </summary>
public interface IRenderTarget
{
    /// <summary>
    /// The <see cref="GraphicsManager"/> that owns this <see cref="IRenderTarget"/>
    /// </summary>
    public GraphicsManager Owner { get; }

    /// <summary>
    /// Obtains the <see cref="Framebuffer"/> that will be used to render into this <see cref="IRenderTarget"/>
    /// </summary>
    /// <param name="device">The device managed by <see cref="Owner"/></param>
    /// <param name="targetBuffer">The resulting Framebuffer that is to be targeted</param>
    /// <param name="targetParameters">The draw parameters to be used</param>
    /// <param name="delta">The amount of time elapsed since the last frame</param>
    public void GetTarget(GraphicsDevice device, TimeSpan delta, out Framebuffer targetBuffer, out DrawParameters targetParameters);

    /// <summary>
    /// Copies the previously obtained <see cref="Framebuffer"/> from this <see cref="IRenderTarget"/> into the screen using <see cref="GraphicsDevice.SwapchainFramebuffer"/> through <paramref name="device"/>
    /// </summary>
    /// <param name="managerCommandList">The <see cref="CommandList"/> to be used, by the point this method is called, it has already begun</param>
    /// <param name="framebuffer">The <see cref="Framebuffer"/> previously obtained from this <see cref="IRenderTarget"/></param>
    /// <param name="device">The device managed by <see cref="Owner"/></param>
    public void CopyToScreen(CommandList managerCommandList, Framebuffer framebuffer, GraphicsDevice device);

    /// <summary>
    /// Queries this <see cref="IRenderTarget"/> to check if a screen copy is required. If this method returns <see langword="true"/> then <see cref="CopyToScreen(CommandList, Framebuffer, GraphicsDevice)"/> will be called with a <see cref="CommandList"/> that has already begun. Otherwise, if <see langword="false"/> is returned, the <see cref="CommandList"/> will not begin anew and <see cref="CopyToScreen(CommandList, Framebuffer, GraphicsDevice)"/> will not be called
    /// </summary>
    /// <param name="device">The device managed by <see cref="Owner"/></param>
    public bool QueryCopyToScreenRequired(GraphicsDevice device);

    /// <summary>
    /// Prepares the previously obtained <see cref="Framebuffer"/> from this <see cref="IRenderTarget"/> to be drawn into
    /// </summary>
    /// <param name="managerCommandList">The <see cref="CommandList"/> to be used</param>
    public void PrepareForDraw(CommandList managerCommandList);
}
