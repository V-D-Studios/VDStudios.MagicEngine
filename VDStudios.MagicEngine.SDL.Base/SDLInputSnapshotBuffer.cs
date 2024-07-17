using System.Numerics;
using SDL2.NET.Input;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Input;

namespace VDStudios.MagicEngine.SDL.Base;

/// <summary>
/// A snapshot for input for SDL
/// </summary>
public class SDLInputSnapshotBuffer<TGraphicsContext> : InputSnapshotBuffer
    where TGraphicsContext : GraphicsContext<TGraphicsContext>
{
    /// <summary>
    /// Instances a new object of type <see cref="InputSnapshotBuffer"/>
    /// </summary>
    /// <param name="manager">The <see cref="GraphicsManager"/> that will own the resulting <see cref="InputSnapshotBuffer"/></param>
    protected internal SDLInputSnapshotBuffer(SDLGraphicsManagerBase<TGraphicsContext> manager) : base(manager) { }

    /// <inheritdoc/>
    protected override Vector2 FetchMousePosition()
        => Mouse.MouseState.Location.ToVector2();
}
