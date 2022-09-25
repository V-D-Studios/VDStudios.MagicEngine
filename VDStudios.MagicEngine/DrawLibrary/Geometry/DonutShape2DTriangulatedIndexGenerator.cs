using System.Runtime.CompilerServices;
using VDStudios.MagicEngine.Geometry;
using Veldrid;

namespace VDStudios.MagicEngine.DrawLibrary.Geometry;

/// <summary>
/// An index generator specifically designed to properly triangulate <see cref="DonutDefinition"/>
/// </summary>
/// <remarks>
/// This triangulator is usually nested inside an instance of <see cref="Shape2DTriangulatedIndexGenerator"/>
/// </remarks>
public class DonutShape2DTriangulatedIndexGenerator : IShape2DRendererIndexGenerator
{
    /// <inheritdoc/>
    public void Start(IEnumerable<ShapeDefinition2D> allShapes, int regenCount, ref object? context)
    {
    }

    /// <inheritdoc/>
    public void GenerateUInt16(ShapeDefinition2D shape, IEnumerable<ShapeDefinition2D> allShapes, Span<ushort> indices, CommandList commandList, DeviceBuffer indexBuffer, int index, int indexCount, ElementSkip vertexSkip, uint indexStart, uint indexSize, out bool isBufferReady, ref object? context)
    {
        var donut = CheckAndThrowIfNotDonut(shape);
        var isp = donut.InnerCircleSpan.Length;
        var osp = donut.OuterCircleSpan.Length;
        var istart = checked((ushort)donut.InnerCircleStart);
        var ostart = checked((ushort)donut.OuterCircleStart);
        int bufind = 0;

        isBufferReady = false;
        if (isp == osp)
        {
            for (ushort i = 0; i < osp; i++)
            {
                ushort in1 = (ushort)((i + 1) % osp + istart);
                ushort ou0 = (ushort)(i + ostart);

                indices[bufind++] = in1;
                indices[bufind++] = ou0;
                indices[bufind++] = (ushort)(i + istart);

                indices[bufind++] = in1;
                indices[bufind++] = ou0;
                indices[bufind++] = (ushort)((i + 1) % osp + ostart);

                indices[bufind++] = in1;
            }
            return;
        }

        if (isp > osp)
            (isp, osp, istart, ostart) = (osp, isp, ostart, istart); // We swap these variables around so that we only have to write one implementation; and the functionality will be the same

        // This algorithm works under the asumption that there are more subdivisions in the outer circle than in the inner one
        // Therefore, each inner circle vertex will have more than one outer circle vertex connected to it
        ushort ratio = (ushort)(osp / isp);
        for (ushort stage = 0; stage < isp * ratio; stage += ratio) 
        {
            ushort in0 = (ushort)(stage / ratio + istart);
            ushort ou1;
            ushort ou0;
            for (ushort i = 0; i < ratio; i++)
            {
                //indices[bufind++] = (ushort)((i + 1) % isp + istart);
                //indices[bufind++] = (ushort)((i % ratio) * stage + ostart);
                //indices[bufind++] = (ushort)(i % isp + istart);
                
                ou1 = (ushort)(((i + stage) + 1) % osp + ostart);
                ou0 = (ushort)((i + stage) + ostart);

                indices[bufind++] = in0;
                indices[bufind++] = ou1;
                indices[bufind++] = ou0;
            }
            indices[bufind++] = in0;
            indices[bufind++] = (ushort)((in0 + 1) % isp);
            indices[bufind++] = (ushort)((stage + 2) % osp + ostart);
            indices[bufind++] = in0;
        }
        indices[bufind++] = 0;
    }

    /// <inheritdoc/>
    public bool QueryUInt16BufferSize(ShapeDefinition2D shape, IEnumerable<ShapeDefinition2D> allShapes, int index, ElementSkip vertexSkip, out int indexCount, out int indexSpace, ref object? context)
    {
        var donut = CheckAndThrowIfNotDonut(shape);   
        var isp = donut.InnerCircleSpan.Length;
        var osp = donut.OuterCircleSpan.Length;

        if (isp == osp)
            indexCount = indexSpace = isp * 7;
        else
        {
            indexCount = indexSpace = 1000;// (isp * 4 + (osp / isp) * 3) * 2;
        }
        return true;
    }

    private static DonutDefinition CheckAndThrowIfNotDonut(ShapeDefinition2D shape, [CallerArgumentExpression(nameof(shape))] string? expr = null) 
        => shape is not DonutDefinition donut
            ? throw new ArgumentException("The shape passed to this generator must be a DonutDefinition", expr)
            : donut;

    /// <inheritdoc/>
    public void Stop(ref object? context)
    {
    }
}
