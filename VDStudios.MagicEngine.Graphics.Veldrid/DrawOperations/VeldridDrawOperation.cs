using System.Diagnostics;
using System.Numerics;
using Veldrid;

namespace VDStudios.MagicEngine.Graphics.Veldrid.DrawOperations;

/// <summary>
/// A draw operation for Veldrid
/// </summary>
public abstract class VeldridDrawOperation : DrawOperation<VeldridGraphicsContext>
{
    /// <inheritdoc/>
    protected VeldridDrawOperation(Game game) : base(game) { }

    /// <summary>
    /// The parameters for a <see cref="VeldridDrawOperation"/>
    /// </summary>
    /// <param name="Transformation">The transformation data for this <see cref="VeldridDrawOperation"/></param>
    /// <param name="Color">The color data for this <see cref="VeldridDrawOperation"/></param>
    public readonly record struct DrawOperationParameters(Matrix4x4 Transformation, ColorTransformation Color);

    /// <summary>
    /// The DeviceBuffer that contains the color and transformation parameters for this <see cref="VeldridDrawOperation"/>
    /// </summary>
    public DeviceBuffer OperationParametersBuffer => opParamBuffer ?? throw new InvalidOperationException("Cannot access a VeldridDrawOperation's OperationParametersBuffer before its resources have been created");
    private DeviceBuffer? opParamBuffer;

    /// <inheritdoc/>
    protected override void CreateGPUResources(VeldridGraphicsContext context)
    {
        opParamBuffer = context.ResourceFactory.CreateBuffer(new BufferDescription(
            DataStructuring.FitToUniformBuffer<DrawOperationParameters, uint>(),
            BufferUsage.UniformBuffer
        ));
    }

    /// <inheritdoc/>
    protected override void UpdateGPUState(VeldridGraphicsContext context)
    {
        Debug.Assert(opParamBuffer is not null, "OperationParametersBuffer was unexpectedly null at the time of updating the GPU state");

        var doparam = new DrawOperationParameters(TransformationState.VertexTransformation, ColorTransformation);
        context.CommandList.UpdateBuffer(opParamBuffer, 0, doparam);
    }
}
