using System.Numerics;
using SDL2.NET.Input;
using VDStudios.MagicEngine.Input;

namespace VDStudios.MagicEngine.Graphics.Veldrid;

/// <summary>
/// A snapshot for input for Veldrid
/// </summary>
public class VeldridInputSnapshotBuffer : InputSnapshotBuffer
{
    /// <summary>
    /// Instances a new object of type <see cref="InputSnapshotBuffer"/>
    /// </summary>
    /// <param name="manager">The <see cref="GraphicsManager"/> that will own the resulting <see cref="InputSnapshotBuffer"/></param>
    protected internal VeldridInputSnapshotBuffer(VeldridGraphicsManager manager) : base(manager) { }

    /// <inheritdoc/>
    protected override Vector2 FetchMousePosition()
        => Mouse.MouseState.Location.ToVector2();
}
