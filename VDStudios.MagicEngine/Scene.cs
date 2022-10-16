using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using VDStudios.MagicEngine.Internal;
using static System.Net.Mime.MediaTypeNames;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a general state of the game
/// </summary>
/// <remarks>
/// A scene can be the MainMenu, a Room, a Dungeon, and any number of things. It is a self-contained state of the <see cref="Game"/>. It's allowed to share states across <see cref="Scene"/>s, but doing so and managing it is your responsibility
/// </remarks>
public abstract class Scene : NodeBase
{
    #region Construction

    /// <summary>
    /// Instances and Initializes the current <see cref="Scene"/>
    /// </summary>
    public Scene() : base("Game Scene")
    {
        Game.SetupScenes += OnGameSetupScenes;
        Game.StopScenes += OnGameStopScenes;
        lock (Game.scenesAwaitingSetup)
            Game.scenesAwaitingSetup.Enqueue(this, (int)QueryConfigurationAsynchronousTendency());
        Services = new(Game.GameServices);
    }

    /// <summary>
    /// Queries this scene for <see cref="ConfigureScene"/>'s tendency to be asynchronous.
    /// </summary>
    /// <remarks>
    /// This method is called exactly once INSIDE THE CONSTRUCTOR OF <see cref="Scene"/>, which is called BEFORE your derived type's constructor. Handle with care; best used by returning a single constant value
    /// </remarks>
    protected virtual AsynchronousTendency QueryConfigurationAsynchronousTendency() => AsynchronousTendency.SometimesAsynchronous;

    #endregion

    #region Scene Setup

    #region Setup

    /// <summary>
    /// This method is called automatically at the beginning of a frame in the <see cref="Game"/> and is called exactly once per instance. This method is guaranteed to only be called by the framework once, and is guaranteed to run before the <see cref="Scene"/> is used for the first time
    /// </summary>
    /// <remarks>
    /// Consider this method an asynchronous constructor for the <see cref="Scene"/>. You may attach nodes and request async services here. Exclusively synchronous work can and should be done in the type's constructor
    /// </remarks>
    protected virtual ValueTask ConfigureScene() => ValueTask.CompletedTask;

    internal ValueTask InternalConfigure()
    {
        InternalLog?.Information("Configuring");
        return ConfigureScene();
    }

    #endregion

    #region Reaction Methods

    /// <summary>
    /// This method is called automatically when the <see cref="Game"/> is loaded and this (and all other) <see cref="Scene"/>s should be set up
    /// </summary>
    /// <remarks>
    /// The <see cref="Game"/> automatically takes care of both <see cref="Scene"/>s that were instanced before the <see cref="Game"/> was loaded and <see cref="Scene"/>s that were instanced afterwards
    /// </remarks>
    protected virtual void GameLoaded() { }

    /// <summary>
    /// This method is called automatically when the <see cref="Game"/> is about to be unloaded (before <see cref="Game.GameUnloading"/> is fired) and this (and all other) <see cref="Scene"/>s should stop as well
    /// </summary>
    protected virtual void GameUnloading() { }

    #endregion

    #region Internal

    private void OnGameSetupScenes()
    {
        GameLoaded();
    }

    private void OnGameStopScenes()
    {
        GameUnloading();
    }

    #endregion

    #endregion

    #region Dependency Injection

    /// <summary>
    /// The <see cref="ServiceCollection"/> for this <see cref="Scene"/>
    /// </summary>
    /// <remarks>
    /// Services to this <see cref="Scene"/> will cascade down from <see cref="Game.GameServices"/>, if not overriden.
    /// </remarks>
    public ServiceCollection Services { get; }

    #endregion

    #region Nodes and Node Tree

    #region Attachment

    /// <summary>
    /// Attaches <paramref name="child"/> into this <see cref="Scene"/>
    /// </summary>
    /// <param name="child">The child <see cref="Node"/> to attach into this <see cref="Scene"/></param>
    public ValueTask Attach(Node child)
        => child.AttachTo(this);

    #endregion

    #endregion

    #region Scene Processing

    #region Internal

    internal async ValueTask Begin()
    {
        InternalLog?.Information("Beginning Scene");
        await Beginning();
        SceneBegan?.Invoke(this, Game.TotalTime);
    }

    internal async ValueTask End(Scene next)
    {
        InternalLog?.Information("Ending scene, to make way for {name}-{type}", next.Name, next.GetTypeName());
        await Ending(next);
        SceneEnded?.Invoke(this, Game.TotalTime);
        await next.Transitioning(this);
    }

    internal async ValueTask End()
    {
        InternalLog?.Information("Ending scene");
        await Ending();
        SceneEnded?.Invoke(this, Game.TotalTime);
    }

    #endregion

    #region Events

    /// <summary>
    /// Fired when this <see cref="Scene"/> has begun
    /// </summary>
    public SceneEvent? SceneBegan;

    /// <summary>
    /// Fired when this <see cref="Scene"/> has ended
    /// </summary>
    public SceneEvent? SceneEnded;

    #endregion

    #region Reaction Methods

    /// <summary>
    /// This method is called automatically when the <see cref="Scene"/>
    /// </summary>
    protected virtual ValueTask Beginning() => ValueTask.CompletedTask;

    /// <summary>
    /// This method is called automatically when the previous <see cref="Scene"/> is ending and this <see cref="Scene"/> is being readied
    /// </summary>
    /// <remarks>
    /// This method is called after <paramref name="previous"/>'s <see cref="Ending(Scene)"/>
    /// </remarks>
    protected virtual ValueTask Transitioning(Scene previous) => ValueTask.CompletedTask;

    /// <summary>
    /// This method is called automatically when the <see cref="Scene"/> is ending, and it's not being replaced by another <see cref="Scene"/>
    /// </summary>
    protected virtual ValueTask Ending() => ValueTask.CompletedTask;

    /// <summary>
    /// This method is called automatically when the <see cref="Scene"/> is ending and the next one is being prepared
    /// </summary>
    /// <remarks>
    /// This method is called before <paramref name="next"/>'s <see cref="Transitioning(Scene)"/>
    /// </remarks>
    /// <param name="next">The <see cref="Scene"/> that will begin next</param>
    protected virtual ValueTask Ending(Scene next) => ValueTask.CompletedTask;

    #endregion

    #endregion

    #region Updating and Drawing

    #region Propagation

    #region Update

    internal async ValueTask Update(TimeSpan delta)
    {
        if (!await Updating(delta))
            return;

        await InternalPropagateChildUpdate(delta);
    }

    #endregion

    #region Draw

    internal async ValueTask RegisterDrawOperations()
    {
#pragma warning disable CA2012 // Just like Roslyn is so kind to warn us about, this code right here has the potential to offer some nasty asynchrony bugs. Be careful here, remember ValueTasks must only ever be consumed once

        var pool = ArrayPool<ValueTask>.Shared;
        int toUpdate = Children.Count;
        ValueTask[] tasks = pool.Rent(toUpdate);
        try
        {
            int ind = 0;
            lock (sync)
            {
                for (int i = 0; i < toUpdate; i++)
                {
                    var child = Children.Get(i);
                    if (child.IsActive)
                        tasks[ind++] = InternalHandleChildDrawRegistration(child);
                }
            }
            for (int i = 0; i < ind; i++)
                await tasks[i];
        }
        finally
        {
            pool.Return(tasks, true);
        }
#pragma warning restore CA2012
    }

    #endregion

    #endregion

    #region Reaction Methods

    #region Update

    /// <summary>
    /// This method is called automatically when the <see cref="Scene"/> is to be updated
    /// </summary>
    /// <param name="delta">The amount of time that has passed since the last update batch call</param>
    /// <returns>Whether the update sequence should be propagated into this <see cref="Node"/>'s children. If this is false, Update handlers for children will also be skipped</returns>
    protected virtual ValueTask<bool> Updating(TimeSpan delta) => ValueTask.FromResult(true);

    #endregion

    #endregion

    #endregion

    #region Disposal

    internal override void InternalDispose(bool disposing)
    {
        Game.SetupScenes -= OnGameSetupScenes;
        Game.StopScenes -= OnGameStopScenes;
        base.InternalDispose(disposing);
    }

    #endregion
}
