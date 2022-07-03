using Microsoft.Extensions.DependencyInjection;
using SDL2.NET;
using System.Numerics;
using System.Runtime.CompilerServices;
using VDStudios.MagicEngine.Internal;
using Veldrid;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents an operation that is ready to be drawn into the GUI.
/// </summary>
/// <remarks>
/// This object is removed from the GUI tree when disposed
/// </remarks>
public abstract class GUIElement : InternalGraphicalOperation, IDisposable
{
    #region Services

    private IServiceScope scope;

    /// <summary>
    /// Represents the <see cref="IServiceProvider"/> scoped for this <see cref="GUIElement"/>
    /// </summary>
    protected IServiceProvider Services => scope.ServiceProvider;

    #endregion

    #region Construction

    /// <summary>
    /// Constructs this <see cref="GUIElement"/>
    /// </summary>
    public GUIElement()
    {
        scope = Game.services.CreateScope();
    }

    #endregion

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

    /// <summary>
    /// Adds Sub Element <paramref name="element"/> to this <see cref="GUIElement"/>
    /// </summary>
    /// <param name="element">The <see cref="GUIElement"/> to add as a sub element of this <see cref="GUIElement"/></param>
    /// <param name="context">The DataContext to give to <paramref name="element"/>, or null if it's to use its previously set DataContext or inherit it from this <see cref="GUIElement"/></param>
    public void AddElement(GUIElement element, object? context = null)
    {
        ThrowIfInvalid();
        element.RegisterOnto(this, Manager!, context);
    }

    #region Internal

    internal void RegisterOnto(GraphicsManager manager, object? context = null)
    {
        ThrowIfInvalid();

        Registering(manager);

        DataContext = context ?? DataContext;
        Manager = manager;
        nodeInParent = manager.GUIElements.Add(this);

        Registered();
    }

    internal void RegisterOnto(GUIElement parent, GraphicsManager manager, object? context = null)
    {
        ThrowIfInvalid();

        Registering(parent, manager);

        DataContext = context ?? DataContext ?? parent.DataContext;
        Manager = manager;
        Parent = parent;
        nodeInParent = parent.SubElements.Add(this);

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

    #endregion

    #region Reaction Methods

    #endregion

    #endregion

    #region Drawing

    #region Internal

    internal void InternalSubmitUI(TimeSpan delta)
    {
        ThrowIfInvalid();
        if (IsActive)
            SubmitUI(delta, SubElements.GetEnumerator());
    }

    #endregion

    #region Properties

    /// <summary>
    /// Whether or not this <see cref="GUIElement"/> is active. Defaults to <c>true</c>
    /// </summary>
    /// <remarks>
    /// If <c>true</c>, this <see cref="GUIElement"/>'s UI will be submitted along with its sub elements. If <c>false</c>, this <see cref="GUIElement"/> will be skipped altogether
    /// </remarks>
    public bool IsActive
    {
        get => isActive;
        set
        {
            if (isActive == value)
                return;

            IsActiveChanging(isActive, value);
            isActive = true;
            IsActiveChanged?.Invoke(this, Game.TotalTime, value);
        }
    }
    private bool isActive = true;

    /// <summary>
    /// Whether this element should be skipped when being enumerated during a call to <see cref="SubmitUI(TimeSpan, IEnumerator{GUIElement})"/>. Defaults to <c>false</c>
    /// </summary>
    /// <remarks>
    /// If this element is skipped from enumeration, the parent <see cref="GUIElement"/> will be entirely responsible for submiting its UI; be very careful with this property. GUIElements that are not registered, or are directly attached to a <see cref="GraphicsManager"/> cannot have this property modified
    /// </remarks>
    protected internal bool SkipInEnumeration
    {
        get => Parent is not null && skipInEnumeration;
        set
        {
            if (Parent is null)
                throw new InvalidOperationException("Cannot modify the SkipInEnumeration property of a GUIElement that is not attached to a parent GUIElement");
            skipInEnumeration = value;
        }
    }
    private bool skipInEnumeration;

    #region Reaction Methods

    /// <summary>
    /// This method is called automatically right before <see cref="IsActive"/> is changes
    /// </summary>
    /// <param name="previousValue"></param>
    /// <param name="newValue"></param>
    protected virtual void IsActiveChanging(bool previousValue, bool newValue) { }

    #endregion

    #region Events

    /// <summary>
    /// Fired when <see cref="IsActive"/> changes
    /// </summary>
    public GUIElementActiveChanged? IsActiveChanged;

    #endregion

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

    private void ThrowIfInvalid()
    {
        ThrowIfDisposed();
        if (Manager is null)
            throw new InvalidOperationException($"Cannot utilize a GUIElement that is not registered onto a GraphicsManager at somepoint in its tree");
    }

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

    private readonly object disposedValueLock = new();
    private void InternalDispose(bool disposing, bool root)
    {
        lock (disposedValueLock)
        {
            IDisposable? @lock = null;
            if (root)
                @lock = Manager!.LockManagerDrawing();

            if (disposedValue)
            {
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
                    current!.Value.InternalDispose(disposing, false);
                }

                if (nodeInParent is LinkedListNode<GUIElement> el)
                    Parent!.SubElements.Remove(el);

                DataContext = null;
                DataContextChanged = null;
                Parent = null;
                Manager = null;
                scope.Dispose();
                scope = null;
                if (root)
                    @lock!.Dispose();
            }
        }
    }

    /// <inheritdoc/>
    ~GUIElement()
    {
        InternalDispose(disposing: false, true);
    }

    private void InternalDispose(bool root)
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        InternalDispose(disposing: true, root);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of this <see cref="GUIElement"/>'s resources
    /// </summary>
    public void Dispose() => InternalDispose(true);

    #endregion
}
