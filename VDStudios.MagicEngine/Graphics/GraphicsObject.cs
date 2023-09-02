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

    private List<Exception>? exceptions;

    /// <summary>
    /// Notifies this <see cref="GraphicsObject{TGraphicsContext}"/> that an exception that potentially corrupts this object's state has been thrown and should be aggregated to be thrown in the Graphics Thread
    /// </summary>
    /// <remarks>
    /// If an exception occurs from a member that is supposed to manipulate a <see cref="GraphicsObject{TGraphicsContext}"/> from outside its respective GraphicsThread (if any), it should be passed to this method and re-thrown. So that if an exception would cause the object's state to be corrupted in the Graphics thread, the exception can be thrown there as well, and avoid obscure exceptions or bugs.
    /// </remarks>
    protected void AggregateExternalException(Exception e)
    {
        ArgumentNullException.ThrowIfNull(e);
        lock (Sync)
            (exceptions ??= new()).Add(e);
    }

    /// <summary>
    /// Checks if there are any exceptions aggregated by <see cref=AggregateExternalException(Exception)"/>, and if so, throws an <see cref="AggregateException"/> containing them.
    /// </summary>
    /// <remarks>
    /// If an exception occurs from a member that is supposed to manipulate a <see cref="GraphicsObject{TGraphicsContext}"/> from outside its respective GraphicsThread (if any), it should be passed to <see cref="AggregateExternalException(Exception)"/> and re-thrown. So that if an exception would cause the object's state to be corrupted in the Graphics thread, the exception can be thrown there as well, and avoid obscure exceptions or bugs.
    /// </remarks>
    /// <exception cref="AggregateException"></exception>
    protected void ThrowIfExternalExceptionPresent()
    {
        lock (Sync)
            if (exceptions != null && exceptions.Count > 0)
            {
                try
                {
                    throw new AggregateException("Exceptions have been thrown in members that manipulate this object from outside threads, and this object's state has been corrupted.", exceptions);
                }
                finally
                {
                    exceptions.Clear();
                }
            }
    }

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
