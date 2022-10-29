using System.Runtime.CompilerServices;
using VDStudios.MagicEngine.Internal;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents an operation that is ready to be drawn into the GUI.
/// </summary>
/// <remarks>
/// This object is removed from the GUI tree when disposed
/// </remarks>
public abstract class ImGuiElement : GraphicsObject, IDisposable
{
    #region Construction

    /// <summary>
    /// Constructs this <see cref="ImGuiElement"/>
    /// </summary>
    public ImGuiElement() : base("ImGUI")
    {
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// The data context currently tied to this <see cref="ImGuiElement"/>
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
    /// The parent <see cref="ImGuiElement"/> of this <see cref="ImGuiElement"/>, or null, if attached directly onto a <see cref="GraphicsManager"/>
    /// </summary>
    public ImGuiElement? Parent { get; private set; }

    #region Events

    /// <summary>
    /// 
    /// </summary>
    public event GUIElementDataContextChangedEvent? DataContextChanged;

    #endregion

    #region Reaction Methods

    /// <summary>
    /// This method is called right before <see cref="DataContextChanged"/> is fired
    /// </summary>
    /// <param name="oldContext">The old DataContext this <see cref="ImGuiElement"/> previously had, if any</param>
    /// <param name="newContext">The old DataContext this <see cref="ImGuiElement"/> will not have, if any</param>
    protected virtual void DataContextChanging(object? oldContext, object? newContext) { }

    #endregion

    #endregion

    #region Registration

    /// <summary>
    /// Adds Sub Element <paramref name="element"/> to this <see cref="ImGuiElement"/>
    /// </summary>
    /// <param name="element">The <see cref="ImGuiElement"/> to add as a sub element of this <see cref="ImGuiElement"/></param>
    /// <param name="context">The DataContext to give to <paramref name="element"/>, or null if it's to use its previously set DataContext or inherit it from this <see cref="ImGuiElement"/></param>
    public void AddElement(ImGuiElement element, object? context = null)
    {
        ThrowIfInvalid();
        element.RegisterOnto(this, imgui_manager!, context);
    }

    #region Internal

    private ImGuiManager? imgui_manager;

    internal void RegisterOnto(ImGuiManager manager, object? context = null)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(manager);
        VerifyManager(manager.OwnerManager);

        Registering(manager.OwnerManager);

        DataContext = context ?? DataContext;
        nodeInParent = manager.GUIElements.Add(this);
        imgui_manager = manager;
        manager.ReportNewElement();

        Registered();
        NotifyIsReady();
    }

    internal void RegisterOnto(ImGuiElement parent, ImGuiManager manager, object? context = null)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(manager.OwnerManager);
        VerifyManager(manager.OwnerManager);

        Registering(parent, manager.OwnerManager);

        DataContext = context ?? DataContext ?? parent.DataContext;
        Parent = parent;
        nodeInParent = parent.SubElements.Add(this);
        imgui_manager = manager;
        manager.ReportNewElement();

        Registered();
        NotifyIsReady();
    }

    #endregion

    #region Reaction Methods

    /// <summary>
    /// This method is called automatically when this <see cref="ImGuiElement"/> is being registered onto <paramref name="manager"/>
    /// </summary>
    /// <param name="manager">The <see cref="GraphicsManager"/> this <see cref="ImGuiElement"/> is being registered onto</param>
    protected virtual void Registering(GraphicsManager manager) { }

    /// <summary>
    /// This method is called automatically when this <see cref="ImGuiElement"/> is being registered onto <paramref name="manager"/>, under <paramref name="parent"/>
    /// </summary>
    /// <param name="manager">The <see cref="GraphicsManager"/> this <see cref="ImGuiElement"/> is being registered onto</param>
    /// <param name="parent">The <see cref="ImGuiElement"/> that is the parent of this <see cref="ImGuiElement"/></param>
    protected virtual void Registering(ImGuiElement parent, GraphicsManager manager) { }

    /// <summary>
    /// This method is called automatically when this <see cref="ImGuiElement"/> has been registered
    /// </summary>
    protected virtual void Registered() { }

    #endregion

    #endregion

    #region Children

    #region Public Properties

    /// <summary>
    /// Represents all of this <see cref="ImGuiElement"/>'s sub elements, or children
    /// </summary>
    public GUIElementList SubElements { get; } = new();

    #endregion

    #region Internal

    private LinkedListNode<ImGuiElement>? nodeInParent;

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
    /// Whether or not this <see cref="ImGuiElement"/> is active. Defaults to <c>true</c>
    /// </summary>
    /// <remarks>
    /// If <c>true</c>, this <see cref="ImGuiElement"/>'s UI will be submitted along with its sub elements. If <c>false</c>, this <see cref="ImGuiElement"/> will be skipped altogether
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
    /// Whether this element should be skipped when being enumerated during a call to <see cref="SubmitUI(TimeSpan, IReadOnlyCollection{ImGuiElement})"/>. Defaults to <c>false</c>
    /// </summary>
    /// <remarks>
    /// If this element is skipped from enumeration, the parent <see cref="ImGuiElement"/> will be entirely responsible for submiting its UI; be very careful with this property. GUIElements that are not registered, or are directly attached to a <see cref="GraphicsManager"/> cannot have this property modified
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
    public event GUIElementActiveChanged? IsActiveChanged;

    #endregion

    #endregion

    #region Helper Methods

    /// <summary>
    /// Submits the UI of the passed <see cref="ImGuiElement"/>
    /// </summary>
    /// <param name="delta">The amount of time that has passed since the last draw sequence</param>
    /// <param name="subElement">The element to submit</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static void Submit(TimeSpan delta, ImGuiElement subElement)
    {
        subElement.InternalSubmitUI(delta);
    }

    #endregion

    #region Reaction Methods

    /// <summary>
    /// The method that will be used to draw the component
    /// </summary>
    /// <remarks>
    /// Unfinished calls and general uncarefulness with ImGUI WILL bleed into other <see cref="ImGuiElement"/>s
    /// </remarks>
    /// <param name="delta">The amount of time that has passed since the last draw sequence</param>
    /// <param name="subElements">An IReadOnlyCollection containing all of this <see cref="ImGuiElement"/>'s sub elements in sequence</param>
    protected abstract void SubmitUI(TimeSpan delta, IReadOnlyCollection<ImGuiElement> subElements);

    #endregion

    #endregion

    #region Disposal

    private void ThrowIfInvalid()
    {
        ThrowIfDisposed();
        if (Manager is null || imgui_manager is null)
            throw new InvalidOperationException($"Cannot utilize a GUIElement that is not registered onto a GraphicsManager at somepoint in its tree");
    }

    internal bool disposedValue;

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
                imgui_manager?.ReportDeadElement();
                var next = SubElements.elements.First;
                while (next is not null)
                {
                    var current = next;
                    next = next.Next;
                    current!.Value.InternalDispose(disposing, false);
                }

                if (nodeInParent is LinkedListNode<ImGuiElement> el)
                    if (Parent is ImGuiElement p)
                        p.SubElements.Remove(el);
                    else
                        imgui_manager!.GUIElements.Remove(el);

                DataContext = null;
                DataContextChanged = null;
                Parent = null;
                if (root)
                    @lock!.Dispose();
            }
        }
        base.InternalDispose(disposing);
    }

    internal override void InternalDispose(bool disposing)
    {
        InternalDispose(disposing, Parent is null);
        base.InternalDispose(disposing);
    }

    #endregion
}
