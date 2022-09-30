using System.Numerics;
using Veldrid;

namespace VDStudios.MagicEngine;

/// <summary>
/// Provides parameters and other useful data to be used when drawing in a <see cref="DrawOperation"/>
/// </summary>
/// <param name="View">The transformation Matrix that represents the object in the world from the viewpoint</param>
/// <param name="Projection">The transformation Matrix that represents the object in the viewpoint from the screen</param>
public readonly record struct DrawTransformation(Matrix4x4 View, Matrix4x4 Projection);

/// <summary>
/// Represents the parameters of a <see cref="DrawTransformation"/> and its accompanying data
/// </summary>
public sealed class DrawParameters : SharedDrawResource
{
    private static BufferDescription buffDesc = new(DataStructuring.FitToUniformBuffer<DrawTransformation, uint>(), BufferUsage.UniformBuffer);

    private readonly object sync = new();
    private DrawTransformation trans;

    /// <summary>
    /// Instances a new object of type <see cref="DrawParameters"/>
    /// </summary>
    public DrawParameters()
    {
        Transformation = new(Matrix4x4.Identity, Matrix4x4.Identity);
    }

    /// <summary>
    /// The parameters represented by this <see cref="DrawParameters"/>
    /// </summary>
    public DrawTransformation Transformation
    {
        get => trans;
        set
        {
            if (trans == value) return;
            trans = value;
            NotifyPendingUpdate();
        }
    }

    /// <summary>
    /// The <see cref="ResourceLayout"/> that describes the resources all <see cref="DrawParameters"/> instances tied to <see cref="Manager"/>
    /// </summary>
    /// <remarks>
    /// Returns <see cref="GraphicsManager.DrawTransformationLayout"/>
    /// </remarks>
    public ResourceLayout ResourceLayout => Manager!.DrawTransformationLayout;

    /// <summary>
    /// The <see cref="ResourceSet"/> that represents the <see cref="TransformationBuffer"/>
    /// </summary>
    public ResourceSet ResourceSet { get; private set; }

    /// <summary>
    /// The Transformation buffer
    /// </summary>
    private DeviceBuffer TransformationBuffer { get; set; }

    /// <inheritdoc/>
    public override ValueTask Update(GraphicsManager manager, GraphicsDevice device, CommandList commandList)
    {
        DrawTransformation dtr = trans;
        commandList.UpdateBuffer(TransformationBuffer, 0, ref dtr);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    protected override ValueTask CreateResources(GraphicsDevice device, ResourceFactory factory)
    {
        TransformationBuffer = factory.CreateBuffer(ref buffDesc);

        var rescDesc = new ResourceSetDescription(ResourceLayout, TransformationBuffer);
        ResourceSet = factory.CreateResourceSet(ref rescDesc);

        device.UpdateBuffer(TransformationBuffer, 0, ref trans);
        return ValueTask.CompletedTask;
    }
}