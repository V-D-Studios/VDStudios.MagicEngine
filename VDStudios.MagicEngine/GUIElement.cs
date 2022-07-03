using SDL2.NET;
using System.Numerics;
using System.Runtime.CompilerServices;
using VDStudios.MagicEngine.Internal;
using Veldrid;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents an operation that is ready to be drawn into the GUI. This object is removed from the GUI tree when disposed
/// </summary>
public abstract class GUIElement : InternalGraphicalOperation, IDisposable
{
    #region Public Properties

    /// <summary>
    /// The data context currently tied to this <see cref="GUIElement"/>
    /// </summary>
    /// <remarks>
    /// Defaults to its parent's <see cref="DataContext"/>, or null
    /// </remarks>
    public object? DataContext
    {
        get => _dc;
        protected set
        {
            if (_dc is null && value is null || _dc is not null && _dc.Equals(value))
                return;

            var prev = _dc;
            _dc = value;
            DataContextChanging(prev, value);
            DataContextChanged?.Invoke(this, Game.TotalTime, prev, value);
        }
    }
    private object? _dc;

    /// <summary>
    /// The parent <see cref="GUIElement"/> of this <see cref="GUIElement"/>, or null, if attached directly onto a <see cref="GraphicsManager"/>
    /// </summary>
    public GUIElement? Parent { get; private set; }

    #region Events

    /// <summary>
    /// 
    /// </summary>
    public GUIElementDataContextChangedEvent? DataContextChanged;

    #endregion

    #region Reaction Methods

    /// <summary>
    /// This method is called right before <see cref="DataContextChanged"/> is fired
    /// </summary>
    /// <param name="oldContext">The old DataContext this <see cref="GUIElement"/> previously had, if any</param>
    /// <param name="newContext">The old DataContext this <see cref="GUIElement"/> will not have, if any</param>
    protected virtual void DataContextChanging(object? oldContext, object? newContext) { }

    #endregion

    #endregion

    #region Registration

    #region Internal

    internal void Register(GraphicsManager manager, object? context)
    {
        ThrowIfDisposed();

        Registering(manager);

        DataContext = context;
        Manager = manager;

        Registered();
    }

    internal void Register(GUIElement parent, GraphicsManager manager, object? context)
    {
        ThrowIfDisposed();

        Registering(parent, manager);

        DataContext = context ?? parent.DataContext;
        Manager = manager;
        Parent = parent;
        parent.AddSubElement(this);

        Registered();
    }

    #endregion

    #region Reaction Methods

    /// <summary>
    /// This method is called automatically when this <see cref="GUIElement"/> is being registered onto <paramref name="manager"/>
    /// </summary>
    /// <param name="manager">The <see cref="GraphicsManager"/> this <see cref="GUIElement"/> is being registered onto</param>
    protected virtual void Registering(GraphicsManager manager) { }

    /// <summary>
    /// This method is called automatically when this <see cref="GUIElement"/> is being registered onto <paramref name="manager"/>, under <paramref name="parent"/>
    /// </summary>
    /// <param name="manager">The <see cref="GraphicsManager"/> this <see cref="GUIElement"/> is being registered onto</param>
    /// <param name="parent">The <see cref="GUIElement"/> that is the parent of this <see cref="GUIElement"/></param>
    protected virtual void Registering(GUIElement parent, GraphicsManager manager) { }

    /// <summary>
    /// This method is called automatically when this <see cref="GUIElement"/> has been registered
    /// </summary>
    protected virtual void Registered() { }

    #endregion

    #endregion

    #region Children

    #region Public Properties

    /// <summary>
    /// Represents all of this <see cref="GUIElement"/>'s sub elements, or children
    /// </summary>
    public GUIElementList SubElements { get; } = new();

    #endregion

    #region Internal

    private LinkedListNode<GUIElement>? nodeInParent;

    internal void AddSubElement(GUIElement sel)
    {
        sel.nodeInParent = SubElements.Add(sel);
    }

    #endregion

    #region Reaction Methods

    #endregion

    #endregion

    #region Drawing

    #region Internal

    internal void InternalSubmitUI(TimeSpan delta)
    {
        ThrowIfDisposed();
        SubmitUI(delta, SubElements.GetEnumerator());
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Submits all remaining sub elements in order all at once, starting from the next <see cref="GUIElement"/> in the sequence, or the first one if it hasn't begun yet
    /// </summary>
    /// <param name="delta">The amount of time that has passed since the last draw sequence</param>
    /// <param name="subElements">An IEnumerator that iterates over all of this <see cref="GUIElement"/>'s sub elements in sequence</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static void SubmitAllRemaining(TimeSpan delta, IEnumerator<GUIElement> subElements)
    {
        while (subElements.MoveNext())
            subElements.Current.InternalSubmitUI(delta);
    }

    /// <summary>
    /// Submits the next sub element in <paramref name="subElements"/>
    /// </summary>
    /// <param name="delta">The amount of time that has passed since the last draw sequence</param>
    /// <param name="subElements">An IEnumerator that iterates over all of this <see cref="GUIElement"/>'s sub elements in sequence</param>
    /// <returns><c>true</c> if a <see cref="GUIElement"/>'s UI was submitted, <c>false</c> if there are no more sub elements</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static bool SubmitNext(TimeSpan delta, IEnumerator<GUIElement> subElements)
    {
        if (subElements.MoveNext())
        {
            subElements.Current.InternalSubmitUI(delta);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Submits the current sub element in <paramref name="subElements"/>
    /// </summary>
    /// <param name="delta">The amount of time that has passed since the last draw sequence</param>
    /// <param name="subElements">An IEnumerator that iterates over all of this <see cref="GUIElement"/>'s sub elements in sequence</param>
    /// <returns><c>true</c> if a <see cref="GUIElement"/>'s UI was submitted, <c>false</c> if <see cref="IEnumerator{T}.Current"/> is <c>null</c></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static bool SubmitCurrent(TimeSpan delta, IEnumerator<GUIElement> subElements)
    {
        if (subElements.Current is GUIElement el)
        {
            el.InternalSubmitUI(delta);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Submits the current sub element and moves to the next sub element in <paramref name="subElements"/>
    /// </summary>
    /// <remarks>
    /// This method will move <paramref name="subElements"/> first if the current value is not available, and then move it again after
    /// </remarks>
    /// <param name="delta">The amount of time that has passed since the last draw sequence</param>
    /// <param name="subElements">An IEnumerator that iterates over all of this <see cref="GUIElement"/>'s sub elements in sequence</param>
    /// <returns><c>true</c> if a <see cref="GUIElement"/>'s UI was submitted, <c>false</c> if there are no more sub elements</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static bool SubmitAndMoveNext(TimeSpan delta, IEnumerator<GUIElement> subElements)
    {
        if (subElements.MoveNext())
        {
            subElements.Current.InternalSubmitUI(delta);
            return true;
        }
        return false;
    }

    #endregion

    #region Reaction Methods

    /// <summary>
    /// The method that will be used to draw the component
    /// </summary>
    /// <remarks>
    /// You can use <see cref="SubmitCurrent(TimeSpan, IEnumerator{GUIElement})"/>, <see cref="SubmitAndMoveNext(TimeSpan, IEnumerator{GUIElement})"/>, <see cref="SubmitNext(TimeSpan, IEnumerator{GUIElement})"/>, and <see cref="SubmitAllRemaining(TimeSpan, IEnumerator{GUIElement})"/>. Unfinished calls and general uncarefulness with ImGUI WILL bleed into other <see cref="GUIElement"/>s
    /// </remarks>
    /// <param name="delta">The amount of time that has passed since the last draw sequence</param>
    /// <param name="subElements">An IEnumerator that iterates over all of this <see cref="GUIElement"/>'s sub elements in sequence. The iteration begins *after* the first call to <see cref="IEnumerator.MoveNext()"/> if it returns <c>true</c>. Be careful not to skip the first one, or accidentally call <see cref="SubmitUI(TimeSpan, IEnumerator{GUIElement})"/> on a <c>null</c> value!</param>
    protected abstract void SubmitUI(TimeSpan delta, IEnumerator<GUIElement> subElements);

    #endregion

    #endregion

    #region Disposal

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> if this <see cref="GUIElement"/> has already been disposed
    /// </summary>
    /// <exception cref="ObjectDisposedException"></exception>
    protected void ThrowIfDisposed()
    {
        if (disposedValue)
            throw new ObjectDisposedException(GetType().FullName);
    }

    internal bool disposedValue;

    /// <summary>
    /// Disposes of this <see cref="GUIElement"/>'s resources
    /// </summary>
    /// <remarks>
    /// Dispose of any additional resources your subtype allocates
    /// </remarks>
    protected virtual void Dispose(bool disposing) { }

    private void InternalDispose(bool disposing)
    {
        var @lock = Manager!.LockManager();
        if (disposedValue)
        {
            @lock.Dispose();
            return;
        }

        disposedValue = true;
        try
        {
            Dispose(disposing);
        }
        finally
        {
            var next = SubElements.elements.First!;
            while (SubElements.Count > 0)
            {
                var current = next;
                next = next.Next;
                current!.Value.Dispose();
            }

            if (nodeInParent is LinkedListNode<GUIElement> el)
                Parent!.SubElements.Remove(el);

            DataContext = null;
            DataContextChanged = null;
            Parent = null;
            Manager = null;
            @lock.Dispose();
        }
    }

    /// <inheritdoc/>
    ~GUIElement()
    {
        InternalDispose(disposing: false);
    }

    /// <summary>
    /// Disposes of this <see cref="GUIElement"/>'s resources
    /// </summary>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        InternalDispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
