using System.Diagnostics;
using SDL2.NET;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.SDL.Base;

namespace VDStudios.MagicEngine.Graphics.SDL;

/// <summary>
/// Represents a context for SDL Graphics
/// </summary>
public class SDLGraphicsContext : GraphicsContext<SDLGraphicsContext>
{
    private SDLGraphicsManager SDLManager
    {
        get
        {
            Debug.Assert(Manager is SDLGraphicsManager, "SDLGraphicsContext's Manager is not a SDLGraphicsManager");
            return (SDLGraphicsManager)Manager;
        }
    }

    /// <inheritdoc/>
    public SDLGraphicsContext(SDLGraphicsManager manager) : base(manager)
    {
        Debug.Assert(SDLManager.Window is not null, "Manager's Window is unexpectedly null");
        Debug.Assert(SDLManager.Renderer is not null, "Manager's Renderer is unexpectedly null");
    }

    /// <summary>
    /// <see cref="GraphicsContext{TGraphicsContext}.Manager"/>'s Renderer
    /// </summary>
    public Renderer Renderer => SDLManager.Renderer;

    /// <summary>
    /// <see cref="GraphicsContext{TGraphicsContext}.Manager"/>'s Window
    /// </summary>
    public Window Window => SDLManager.Window;

    /// <inheritdoc/>
    public override void Update(TimeSpan delta)
    {

    }

    /// <inheritdoc/>
    public override void BeginFrame()
    {
        Renderer.Clear(Manager.BackgroundColor.ToRGBAColor());
    }

    /// <inheritdoc/>
    public override void EndAndSubmitFrame()
    {
        Renderer.Present();
    }
}
