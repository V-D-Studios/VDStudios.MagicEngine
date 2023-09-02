using System.Runtime.CompilerServices;

namespace VDStudios.MagicEngine.Graphics.SDL.GUI;

/// <summary>
/// Represents an operation that is ready to be drawn into the GUI.
/// </summary>
/// <remarks>
/// This object is removed from the GUI tree when disposed
/// </remarks>
public abstract class ImGUIElement : GameObject, IDisposable
{
    #region Construction

    /// <summary>
    /// Constructs this <see cref="ImGUIElement"/>
    /// </summary>
    public ImGUIElement(Game game) : base(game, "Graphics & Input", "ImGUI") { }

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
            DataContextChanged?.Invoke(this, Game.TotalTime);
        }
    }
    private object? _dc;

    #region Events

    /// <summary>
    /// 
    /// </summary>
    public GameObjectEvent<ImGUIElement>? DataContextChanged;

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

    #region Drawing

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
            IsActiveChanged?.Invoke(this, value, Game.TotalTime);
        }
    }
    private bool isActive = true;

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
    public GameObjectEvent<bool, ImGUIElement>? IsActiveChanged;

    #endregion

    #endregion

    #region Reaction Methods

    /// <summary>
    /// The method that will be used to draw the component
    /// </summary>
    /// <remarks>
    /// Unfinished calls and general uncarefulness with ImGUI WILL bleed into other <see cref="ImGUIElement"/>s
    /// </remarks>
    /// <param name="delta">The amount of time that has passed since the last draw sequence</param>
    protected internal abstract void SubmitUI(TimeSpan delta);

    #endregion

    #endregion

    #region Disposal

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        DataContext = null;
        DataContextChanged = null;
    }

    #endregion
}
