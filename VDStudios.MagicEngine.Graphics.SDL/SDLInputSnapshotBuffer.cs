using System.Numerics;
using SDL2.NET.Input;
using VDStudios.MagicEngine.Input;
using VDStudios.MagicEngine.Internal;

namespace VDStudios.MagicEngine.Graphics.SDL;

/// <summary>
/// A snapshot for input for SDL
/// </summary>
public class SDLInputSnapshotBuffer : InputSnapshotBuffer
{
    /// <summary>
    /// Instances a new object of type <see cref="InputSnapshotBuffer"/>
    /// </summary>
    /// <param name="manager">The <see cref="GraphicsManager"/> that will own the resulting <see cref="InputSnapshotBuffer"/></param>
    protected internal SDLInputSnapshotBuffer(SDLGraphicsManager manager) : base(manager) { }

    /// <inheritdoc/>
    protected override Vector2 FetchMousePosition()
        => Mouse.MouseState.Location.ToVector2();
}
