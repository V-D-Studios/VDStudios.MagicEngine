using System.Numerics;
using Veldrid;

namespace VDStudios.MagicEngine;

/// <summary>
/// Provides parameters and other useful data to be used when drawing in a <see cref="DrawOperation"/>
/// </summary>
/// <param name="Projection">The projection matrix</param>
/// <param name="View">The view matrix</param>
public readonly record struct DrawTransformation(Matrix4x4 View, Matrix4x4 Projection);

/// <summary>
/// Represents the parameters of a <see cref="DrawTransformation"/> and its accompanying data
/// </summary>
/// <param name="DrawTransformation">The actual <see cref="DrawTransformation"/> parameters</param>
/// <param name="TransformationBuffer">The buffer containing the data represented by <paramref name="DrawTransformation"/></param>
/// <param name="ResourceLayout"></param>
/// <param name="ResourceSet"></param>
public sealed class DrawParameters
{
    private static BufferDescription buffDesc = new(DataStructuring.FitToUniformBuffer<DrawTransformation>(), BufferUsage.UniformBuffer);

    private readonly object sync = new();
    private DrawTransformation trans;
    private readonly DeviceBuffer buff;
    private bool PendingBufferUpdate = true;

    public DrawParameters(DrawTransformation transformation, GraphicsManager manager)
    {
        trans = transformation;
        Manager = manager;
        var factory = manager.Device.ResourceFactory;
        buff = factory.CreateBuffer(ref buffDesc);

        var rescDesc = new ResourceSetDescription(ResourceLayout, buff);
        ResourceSet = factory.CreateResourceSet(ref rescDesc);
    }

    /// <summary>
    /// The parameters represented by this <see cref="DrawParameters"/>
    /// </summary>
    public DrawTransformation Parameters
    {
        get => trans;
        set
        {
            if (trans == value)
                return;
            lock (sync)
            {
                trans = value;
                PendingBufferUpdate = true;
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
    /// The <see cref="ResourceSet"/> that represents the TransformationBuffer returned by <see cref="FetchTransformationBuffer(CommandList)"/>
    /// </summary>
    public ResourceSet ResourceSet { get; }

    /// <summary>
    /// Fetches the transformation buffer in this object
    /// </summary>
    /// <returns>The TransformationBuffer</returns>
    public DeviceBuffer FetchTransformationBuffer()
    {
        DrawTransformation dtr;
        lock (sync)
            if (PendingBufferUpdate) 
            {
                PendingBufferUpdate = false;
                dtr = trans;
                goto UpdateBuffer; // Step out of the lock as soon as possible
            }

        return buff;

    UpdateBuffer:

        Manager.Device.UpdateBuffer(buff, 0, ref dtr);
        return buff;
    }
}