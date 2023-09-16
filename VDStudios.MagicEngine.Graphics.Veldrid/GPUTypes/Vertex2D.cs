using System.Numerics;
using System.Runtime.CompilerServices;
using VDStudios.MagicEngine.Graphics.Veldrid.DrawOperations;
using VDStudios.MagicEngine.Graphics.Veldrid.Generators;
using VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes.Interfaces;
using Veldrid;

namespace VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes;

/// <summary>
/// Represents information regarding the viewports of a <see cref="TexturedShape2DRenderer"/>
/// </summary>
public readonly record struct Vertex2D(Vector2 Vertex) : IVertexType<Vertex2D>, IDefaultVertexGenerator<Vector2, Vertex2D>
{
    /// <inheritdoc/>
    public static int Size { get; } = Unsafe.SizeOf<Vertex2D>();

    /// <inheritdoc/>
    public static VertexLayoutDescription GetDescription()
        => new(
            new VertexElementDescription("Position", VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate)
        );

    /// <inheritdoc/>
    public static IVertexGenerator<Vector2, Vertex2D> DefaultGenerator => Vertex2DSimpleGenerator.Instance;
}
