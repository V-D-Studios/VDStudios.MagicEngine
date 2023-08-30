using System.Diagnostics.CodeAnalysis;
using VDStudios.MagicEngine.Exceptions;

namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// Represents the base class for graphical operations, such as <see cref="DrawOperation{TGraphicsContext}"/>
/// </summary>
/// <remarks>
/// This class cannot be instanced or inherited by user code
/// </remarks>
public abstract class GraphicsObject<TGraphicsContext> : GameObject
    where TGraphicsContext : GraphicsContext<TGraphicsContext>
{
    internal GraphicsObject(Game game, string facility) : base(game, facility, "Rendering")
    {
    }

    /// <summary>
    /// The <see cref="GraphicsManager{TGraphicsContext}"/> this operation is registered onto
    /// </summary>
    /// <remarks>
    /// Will be null if this operation is not registered
    /// </remarks>
    public GraphicsManager<TGraphicsContext>? Manager { get; private set; }

    private bool isRegistered = false;

    internal void AssignManager(GraphicsManager<TGraphicsContext> manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        if (isRegistered)
            throw new InvalidOperationException("This GraphicsObject is already registered on a GraphicsManager<TGraphicsContext>");
        isRegistered = true;
        Manager = manager;

        GameMismatchException.ThrowIfMismatch(manager, this);
    }

    internal void VerifyManager(GraphicsManager<TGraphicsContext> manager)
    {
        if (isRegistered is false)
            throw new InvalidOperationException("This GraphicsObject was not properly assigned a GraphicsManager");
        if (!ReferenceEquals(manager, Manager))
            throw new InvalidOperationException("Cannot register a GraphicsObject under a different GraphicsManager than it was first queued to. This is likely a library bug.");

        GameMismatchException.ThrowIfMismatch(manager, this);
    }
}
