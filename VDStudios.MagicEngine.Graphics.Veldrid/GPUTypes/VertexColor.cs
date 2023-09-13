using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using VDStudios.MagicEngine.Graphics.Veldrid.DrawOperations;
using VDStudios.MagicEngine.Graphics.Veldrid.Generators;
using VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes.Interfaces;
using Veldrid;

namespace VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes;

/// <summary>
/// Vertex information containing a 2D polygon position vertex and a RGBA color
/// </summary>
public readonly struct VertexColor2D : IVertexType<VertexColor2D>,
    IDefaultVertexGenerator<Vector2, VertexColor2D>
{
    /// <summary>
    /// The position of the vertex in the polygon
    /// </summary>
    public readonly Vector2 PolygonVertex;

    /// <summary>
    /// The color of the vertex
    /// </summary>
    public readonly RgbaVector Color;

    /// <summary>
    /// Creates a new <see cref="VertexColor2D"/>
    /// </summary>
    /// <param name="vertex">The position of the vertex in the polygon</param>
    /// <param name="color">The color of the vertex</param>
    public VertexColor2D(Vector2 vertex, RgbaVector color)
    {
        PolygonVertex = vertex;
        Color = color;
    }

    /// <inheritdoc/>
    public static int Size { get; } = Unsafe.SizeOf<VertexColor2D>();

    /// <inheritdoc/>
    public static VertexLayoutDescription GetDescription()
        => new(
               new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
               new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4)
           );

    static IVertexGenerator<Vector2, VertexColor2D> IDefaultVertexGenerator<Vector2, VertexColor2D>.DefaultGenerator
        => DefaultGenerator;

    /// <summary>
    /// The default <see cref="IVertexGenerator{TInputVertex, TGraphicsVertex}"/> for this type
    /// </summary>
    public static Vector2ToVertexColor2DGen DefaultGenerator { get; } = new();

    /// <summary>
    /// A <see cref="IVertexGenerator{TInputVertex, TGraphicsVertex}"/> that injects a single color into every instance of <see cref="VertexColor2D"/> it generates from position info
    /// </summary>
    public sealed class Vector2ToVertexColor2DGen : IVertexGenerator<Vector2, VertexColor2D>
    {
        /// <summary>
        /// Creates a new instance of type <see cref="Vector2ToVertexColor2DGen"/>
        /// </summary>
        public Vector2ToVertexColor2DGen() { }

        /// <summary>
        /// The color to apply to every vertex 
        /// </summary>
        public RgbaVector Color { get; set; } = RgbaVector.White;

        /// <inheritdoc/>
        public void Generate(ReadOnlySpan<Vector2> input, Span<VertexColor2D> output)
        {
            if (input.Length != output.Length)
                throw new ArgumentException("input and output length are mismatched", nameof(input));

            for (int i = 0; i < input.Length; i++)
                output[i] = new VertexColor2D(input[i], Color);
        }
    }
}