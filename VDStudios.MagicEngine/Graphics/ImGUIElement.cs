using System.Runtime.CompilerServices;

namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// Represents an operation that is ready to be drawn into the GUI.
/// </summary>
/// <remarks>
/// This object is removed from the GUI tree when disposed
/// </remarks>
public abstract class ImGUIElement<TGraphicsContext> : GraphicsObject<TGraphicsContext>, IDisposable
{
    #region Construction

    /// <summary>
    /// Constructs this <see cref="ImGUIElement"/>
    /// </summary>
    public ImGUIElement() : base("ImGUI")
    {
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// The data context currently tied to this <see cref="ImGUIElement"/>
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
    /// The parent <see cref="ImGUIElement"/> of this <see cref="ImGUIElement"/>, or null, if attached directly onto a <see cref="GraphicsManager"/>
    /// </summary>
    public ImGUIElement? Parent { get; private set; }

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
    /// <param name="oldContext">The old DataContext this <see cref="ImGUIElement"/> previously had, if any</param>
    /// <param name="newContext">The old DataContext this <see cref="ImGUIElement"/> will not have, if any</param>
    protected virtual void DataContextChanging(object? oldContext, object? newContext) { }

    #endregion

    #endregion

    #region Registration

    /// <summary>
    /// Adds Sub Element <paramref name="element"/> to this <see cref="ImGUIElement"/>
    /// </summary>
    /// <param name="element">The <see cref="ImGUIElement"/> to add as a sub element of this <see cref="ImGUIElement"/></param>
    /// <param name="context">The DataContext to give to <paramref name="element"/>, or null if it's to use its previously set DataContext or inherit it from this <see cref="ImGUIElement"/></param>
    public void AddElement(ImGUIElement element, object? context = null)
    {
        ThrowIfInvalid();
        element.RegisterOnto(this, Manager!, context);
    }

    #region Internal

    internal void RegisterOnto(GraphicsManager manager, object? context = null)
    {
        ThrowIfDisposed();
        VerifyManager(manager);

        Registering(manager);

        DataContext = context ?? DataContext;
        nodeInParent = manager.GUIElements.Add(this);

        Registered();
        NotifyIsReady();
    }

    internal void RegisterOnto(ImGUIElement parent, GraphicsManager manager, object? context = null)
    {
        ThrowIfDisposed();
        VerifyManager(manager);

        Registering(parent, manager);

        DataContext = context ?? DataContext ?? parent.DataContext;
        Parent = parent;
        nodeInParent = parent.SubElements.Add(this);

        Registered();
        NotifyIsReady();
    }

    #endregion

    #region Reaction Methods

    /// <summary>
    /// This method is called automatically when this <see cref="ImGUIElement"/> is being registered onto <paramref name="manager"/>
    /// </summary>
    /// <param name="manager">The <see cref="GraphicsManager"/> this <see cref="ImGUIElement"/> is being registered onto</param>
    protected virtual void Registering(GraphicsManager manager) { }

    /// <summary>
    /// This method is called automatically when this <see cref="ImGUIElement"/> is being registered onto <paramref name="manager"/>, under <paramref name="parent"/>
    /// </summary>
    /// <param name="manager">The <see cref="GraphicsManager"/> this <see cref="ImGUIElement"/> is being registered onto</param>
    /// <param name="parent">The <see cref="ImGUIElement"/> that is the parent of this <see cref="ImGUIElement"/></param>
    protected virtual void Registering(ImGUIElement parent, GraphicsManager manager) { }

    /// <summary>
    /// This method is called automatically when this <see cref="ImGUIElement"/> has been registered
    /// </summary>
    protected virtual void Registered() { }

    #endregion

    #endregion

    #region Children

    #region Public Properties

    /// <summary>
    /// Represents all of this <see cref="ImGUIElement"/>'s sub elements, or children
    /// </summary>
    public GUIElementList SubElements { get; } = new();

    #endregion

    #region Internal

    private LinkedListNode<ImGUIElement>? nodeInParent;

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
            SubmitUI(delta, SubElements);
    }

    #endregion

    #region Properties

    /// <summary>
    /// Whether or not this <see cref="ImGUIElement"/> is active. Defaults to <c>true</c>
    /// </summary>
    /// <remarks>
    /// If <c>true</c>, this <see cref="ImGUIElement"/>'s UI will be submitted along with its sub elements. If <c>false</c>, this <see cref="ImGUIElement"/> will be skipped altogether
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
    /// Whether this element should be skipped when being enumerated during a call to <see cref="SubmitUI(TimeSpan, IEnumerator{ImGUIElement}, int)"/>. Defaults to <c>false</c>
    /// </summary>
    /// <remarks>
    /// If this element is skipped from enumeration, the parent <see cref="ImGUIElement"/> will be entirely responsible for submiting its UI; be very careful with this property. GUIElements that are not registered, or are directly attached to a <see cref="GraphicsManager"/> cannot have this property modified
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
    /// Submits the UI of the passed <see cref="ImGUIElement"/>
    /// </summary>
    /// <param name="delta">The amount of time that has passed since the last draw sequence</param>
    /// <param name="subElement">The element to submit</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static void Submit(TimeSpan delta, ImGUIElement subElement)
    {
        subElement.InternalSubmitUI(delta);
    }

    #endregion

    #region Reaction Methods

    /// <summary>
    /// The method that will be used to draw the component
    /// </summary>
    /// <remarks>
    /// Unfinished calls and general uncarefulness with ImGUI WILL bleed into other <see cref="ImGUIElement"/>s
    /// </remarks>
    /// <param name="delta">The amount of time that has passed since the last draw sequence</param>
    /// <param name="subElements">An IReadOnlyCollection containing all of this <see cref="ImGUIElement"/>'s sub elements in sequence</param>
    protected abstract void SubmitUI(TimeSpan delta, IReadOnlyCollection<ImGUIElement> subElements);

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
    /// Throws an <see cref="ObjectDisposedException"/> if this <see cref="ImGUIElement"/> has already been disposed
    /// </summary>
    /// <exception cref="ObjectDisposedException"></exception>
    protected void ThrowIfDisposed()
    {
        if (disposedValue)
            throw new ObjectDisposedException(GetType().FullName);
    }

    internal bool disposedValue;

    /// <summary>
    /// Disposes of this <see cref="ImGUIElement"/>'s resources
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
            if (root && Manager is GraphicsManager manager)
                @lock = manager.LockManagerDrawing();

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

                if (nodeInParent is LinkedListNode<ImGUIElement> el)
                    Parent!.SubElements.Remove(el);

                DataContext = null;
                DataContextChanged = null;
                Parent = null;
                if (root)
                    @lock!.Dispose();
            }
        }
    }

    /// <inheritdoc/>
    ~ImGUIElement()
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
    /// Disposes of this <see cref="ImGUIElement"/>'s resources
    /// </summary>
    public void Dispose() => InternalDispose(true);

    #endregion
}
