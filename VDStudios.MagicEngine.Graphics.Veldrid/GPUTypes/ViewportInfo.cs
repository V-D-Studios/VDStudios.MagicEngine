using System.Runtime.CompilerServices;
using VDStudios.MagicEngine.Graphics.Veldrid.DrawOperations;
using VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes.Interfaces;

namespace VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes;

/// <summary>
/// Represents information regarding the viewports of a <see cref="TexturedShape2DRenderer"/>
/// </summary>
public readonly record struct ViewportInfo(int ViewportCount, int ViewportSize, int CurrentViewport) : IGPUType<ViewportInfo>
{
    /// <inheritdoc/>
    public static int Size { get; } = Unsafe.SizeOf<ViewportInfo>();
}
