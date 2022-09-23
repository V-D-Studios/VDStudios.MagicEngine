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

        if (isp > osp)
        {
            var ratio = isp / osp;
            for (ushort stage = 0; stage < ratio; stage++)
                for (ushort i = 0; i < isp; i++)
                {
                    indices[bufind++] = (ushort)((i + 1) % isp + istart);
                    indices[bufind++] = (ushort)((i % ratio) * stage + ostart);
                    indices[bufind++] = (ushort)(i % isp + istart);
                }
        }
        if (osp > isp)
        {
            var ratio = osp / isp;
            for (ushort stage = 0; stage < ratio; stage++)
                for (ushort i = 0; i < osp; i++)
                {
                    indices[bufind++] = (ushort)((i + 1) % osp + istart);
                    indices[bufind++] = (ushort)((i % ratio) * stage + ostart);
                    indices[bufind++] = (ushort)(i % osp + istart);
                }
        }
        else
        {
            for (ushort i = 0; i < osp; i++)
            {
                indices[bufind++] = (ushort)((i + 1) % osp + istart);
                indices[bufind++] = (ushort)(i + ostart);
                indices[bufind++] = (ushort)(i + istart);
            }
        }

        isBufferReady = false;
    }

    /// <inheritdoc/>
    public bool QueryUInt16BufferSize(ShapeDefinition2D shape, IEnumerable<ShapeDefinition2D> allShapes, int index, ElementSkip vertexSkip, out int indexCount, out int indexSpace, ref object? context)
    {
        var donut = CheckAndThrowIfNotDonut(shape);   
        var isp = donut.InnerCircleSpan.Length;
        var osp = donut.OuterCircleSpan.Length;
        indexCount = indexSpace = isp > osp ? (isp + isp / osp) * 3 : osp > isp ? (osp + osp / isp) * 3 : isp * 3;
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
