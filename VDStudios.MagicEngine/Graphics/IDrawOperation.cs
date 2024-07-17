
using System.Diagnostics.CodeAnalysis;

namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// Represents an operation that is ready to be drawn. This object is automatically deregistered when disposed of
/// </summary>
/// <remarks>
/// Try to keep an object created from this class cached somewhere in a node, as they incur a number of allocations that should be avoided in a HotPath like the rendering sequence
/// </remarks>
public interface IDrawOperation<TGraphicsContext> 
    : IGraphicsObject<TGraphicsContext>
    where TGraphicsContext : GraphicsContext<TGraphicsContext>
{
    /// <summary>
    /// The transformation matrix that represents the current transformation properties in this <see cref="DrawOperation{TGraphicsContext}"/>
    /// </summary>
    ColorTransformation ColorTransformation { get; set; }

    /// <summary>
    /// Whether or not this <see cref="DrawOperation{TGraphicsContext}"/> is active 
    /// </summary>
    /// <remarks>
    /// If <see langword="false"/>, then Drawing and resource updating for this operation is skipped
    /// </remarks>
    bool IsActive { get; set; }

    /// <summary>
    /// <see langword="true"/> if this <see cref="DrawOperation{TGraphicsContext}"/>'s Resources have been created and is ready for use
    /// </summary>
    bool IsReady { get; }

    /// <summary>
    /// The owner <see cref="DrawOperationManager{TGraphicsContext}"/> of this <see cref="DrawOperation{TGraphicsContext}"/>
    /// </summary>
    /// <remarks>
    /// Will throw if this <see cref="DrawOperation{TGraphicsContext}"/> is not registered
    /// </remarks>
    DrawOperationManager<TGraphicsContext> Owner { get; }

    /// <summary>
    /// Represents this <see cref="DrawOperation{TGraphicsContext}"/>'s preferred priority. May or may not be honored depending on the <see cref="DrawOperationManager{TGraphicsContext}"/>
    /// </summary>
    float PreferredPriority { get; set; }

    /// <summary>
    /// This <see cref="DrawOperation{TGraphicsContext}"/>'s current transformation state
    /// </summary>
    TransformationState TransformationState { get; }

    /// <summary>
    /// Fired when <see cref="ColorTransformation"/> changes
    /// </summary>
    event DrawOperationEvent<TGraphicsContext>? ColorTransformationChanged;

    /// <summary>
    /// Fired when <see cref="IsActive"/> changes
    /// </summary>
    event DrawOperationEvent<TGraphicsContext>? IsActiveChanged;

    /// <summary>
    /// Fired when <see cref="TransformationState"/> <see cref="TransformationState.ScaleTransformation"/> changes
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="TransformationState.ScaleTransformation"/>, this event belongs specifically to <see cref="DrawOperation{TGraphicsContext}"/>
    /// </remarks>
    event DrawOperationEvent<TGraphicsContext>? ScaleTransformationChanged;

    /// <summary>
    /// Fired when <see cref="TransformationState"/> <see cref="TransformationState.TranslationTransformation"/> changes
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="TransformationState.TranslationTransformation"/>, this event belongs specifically to <see cref="DrawOperation{TGraphicsContext}"/>
    /// </remarks>
    event DrawOperationEvent<TGraphicsContext>? TranslationTransformationChanged;

    /// <summary>
    /// Fired when <see cref="TransformationState"/> <see cref="TransformationState.VertexTransformation"/> changes
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="TransformationState.VertexTransformation"/>, this event belongs specifically to <see cref="DrawOperation{TGraphicsContext}"/>
    /// </remarks>
    event DrawOperationEvent<TGraphicsContext>? VertexTransformationChanged;

    /// <summary>
    /// Waits until this <see cref="DrawOperation{TGraphicsContext}"/> is ready for use
    /// </summary>
    ValueTask WaitUntilReady(CancellationToken ct = default);

    /// <summary>
    /// Waits until this <see cref="DrawOperation{TGraphicsContext}"/> is ready for use
    /// </summary>
    /// <remarks>
    /// <see langword="true"/> If the <see cref="DrawOperation{TGraphicsContext}"/> is now ready, <see langword="false"/> otherwise
    /// </remarks>
    ValueTask<bool> WaitUntilReady(TimeSpan timeout, CancellationToken ct = default);
}