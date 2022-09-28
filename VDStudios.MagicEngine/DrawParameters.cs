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
public sealed class DrawParameters
{
    private static BufferDescription buffDesc = new(DataStructuring.FitToUniformBuffer<DrawTransformation, uint>(), BufferUsage.UniformBuffer);

    private readonly object sync = new();
    private DrawTransformation trans;
    private bool PendingBufferUpdate = true;

    public DrawParameters(GraphicsManager manager)
    {
        Manager = manager;
        var factory = manager.Device.ResourceFactory;
        TransformationBuffer = factory.CreateBuffer(ref buffDesc);

        var rescDesc = new ResourceSetDescription(ResourceLayout, TransformationBuffer);
        ResourceSet = factory.CreateResourceSet(ref rescDesc);

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
            lock (sync)
            {
                PendingBufferUpdate = true;
                trans = value;
            }
        }
    }

    /// <summary>
    /// The <see cref="ResourceLayout"/> that describes the resources all <see cref="DrawParameters"/> instances tied to <see cref="Manager"/>
    /// </summary>
    /// <remarks>
    /// Returns <see cref="GraphicsManager.DrawTransformationLayout"/>
    /// </remarks>
    public ResourceLayout ResourceLayout => Manager.DrawTransformationLayout;

    /// <summary>
    /// The <see cref="GraphicsManager"/> this <see cref="DrawParameters"/> instance is tied to
    /// </summary>
    public GraphicsManager Manager { get; }

    /// <summary>
    /// The <see cref="ResourceSet"/> that represents the <see cref="TransformationBuffer"/>
    /// </summary>
    public ResourceSet ResourceSet { get; }

    /// <summary>
    /// The Transformation buffer
    /// </summary>
    private DeviceBuffer TransformationBuffer { get; }

    /// <summary>
    /// Checks if there are pending updates to the transformation buffer and if so, updates it
    /// </summary>
    public void UpdateTransformationBuffer()
    {
        DrawTransformation dtr;
        if (PendingBufferUpdate) 
            lock (sync)
                if (PendingBufferUpdate)
                {
                    PendingBufferUpdate = false;
                    dtr = trans;
                    Manager.Device.UpdateBuffer(TransformationBuffer, 0, ref dtr);
                }
    }
}