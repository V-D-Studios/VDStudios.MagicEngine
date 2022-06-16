namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a general state of the game
/// </summary>
/// <remarks>
/// A scene can be the MainMenu, a Room, a Dungeon, and any number of things. It is a self-contained state of the <see cref="Game"/>
/// </remarks>
public class Scene
{
    /// <summary>
    /// Instances a new Scene
    /// </summary>
    public Scene()
    {
        Game = Game.Instance;
    }

    /// <summary>
    /// The <see cref="MagicEngine.Game"/> this <see cref="Scene"/> belongs to. Same as<see cref="SDL2.NET.SDLApplication{TApp}.Instance"/>
    /// </summary>
    protected Game Game { get; }

    internal Task InternalUpdate(TimeSpan delta);
    internal void InternalDraw(TimeSpan delta);
    internal Task InternalBegin(IServiceProvider services);
    internal Task InternalEnd(Scene next);

    protected virtual Task Update(TimeSpan delta);

    #region Events

    /// <summary>
    /// Fired when the <see cref="Scene"/> begins and is set on <see cref="Game.CurrentScene"/>
    /// </summary>
    public event SceneEvent? SceneBegan;

    /// <summary>
    /// Fired when the <see cref="Scene"/> ends and is withdrawn from <see cref="Game.CurrentScene"/>
    /// </summary>
    public event SceneEvent? SceneEnded;

    #endregion
}