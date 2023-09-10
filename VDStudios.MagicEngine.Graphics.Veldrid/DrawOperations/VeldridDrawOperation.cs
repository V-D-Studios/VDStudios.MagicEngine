using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

    /// <summary>
    /// The <see cref="ResourceLayout"/> for all <see cref="VeldridDrawOperation"/>, containing  "DrawParameters" first for the draw operation's Transformation and Color and "FrameParameters" second for 
    /// </summary>
    protected ResourceLayout? DrawOperationLayout { get; private set; }

    /// <summary>
    /// The <see cref="ResourceSet"/> containing the resources for <see cref="DrawOperationLayout"/>
    /// </summary>
    protected ResourceSet? DrawOperationResourceSet { get; private set; }

    /// <inheritdoc/>
    [MemberNotNull(nameof(OperationParametersBuffer), nameof(DrawOperationLayout), nameof(DrawOperationResourceSet))]
    protected override void CreateGPUResources(VeldridGraphicsContext context)
    {
        opParamBuffer = context.ResourceFactory.CreateBuffer(new BufferDescription(
            DataStructuring.FitToUniformBuffer<DrawOperationParameters, uint>(),
            BufferUsage.UniformBuffer
        ));
        Debug.Assert(OperationParametersBuffer is not null);

        if (context.ContainsResourceLayout< VeldridDrawOperation>() is false)
        {
            DrawOperationLayout = context.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription()
            {
                Elements = new ResourceLayoutElementDescription[]
                {
                    new("DrawParameters", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment), // Transformation and Color
                    new("FrameParameters", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment), // Timing and Projection
                }
            });
            context.RegisterResourceLayout<VeldridDrawOperation>(DrawOperationLayout, out _);
        }
        else
            DrawOperationLayout = context.GetResourceLayout<VeldridDrawOperation>();

        DrawOperationResourceSet = context.ResourceFactory.CreateResourceSet(new ResourceSetDescription()
        {
            Layout = DrawOperationLayout,
            BoundResources = new IBindableResource[]
            {
                opParamBuffer,
                context.FrameReportBuffer
            }
        });
    }

    /// <inheritdoc/>
    protected override void UpdateGPUState(VeldridGraphicsContext context)
    {
        Debug.Assert(opParamBuffer is not null, "OperationParametersBuffer was unexpectedly null at the time of updating the GPU state");

        var doparam = new DrawOperationParameters(TransformationState.VertexTransformation, ColorTransformation);
        context.CommandList.UpdateBuffer(opParamBuffer, 0, doparam);
    }
}
