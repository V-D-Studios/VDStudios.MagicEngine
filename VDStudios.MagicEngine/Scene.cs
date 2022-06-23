using Microsoft.Extensions.DependencyInjection;
using System.Buffers;
using static VDStudios.MagicEngine.Node;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a general state of the game
/// </summary>
/// <remarks>
/// A scene can be the MainMenu, a Room, a Dungeon, and any number of things. It is a self-contained state of the <see cref="Game"/>. It's allowed to share states across <see cref="Scene"/>s, but doing so and managing it is your responsibility
/// </remarks>
public abstract class Scene : GameObject, IDisposable
{
    /// <summary>
    /// Instances and Initializes the current <see cref="Scene"/>
    /// </summary>
    public Scene()
    {
        Nodes = NodeList.Empty.Clone(sync);

        Game.SetupScenes += OnGameSetupScenes;
        Game.StopScenes += OnGameStopScenes;

        serviceScope = Game.Services.CreateScope();
    }

    #region Private (and internal) fields and methods

    private readonly object sync = new();

    private IServiceScope serviceScope;

    /// <summary>
    /// This <see cref="Scene"/>'s scoped <see cref="IServiceProvider"/>
    /// </summary>
    /// <remarks>
    /// It's better to ues it once only during the construction of this <see cref="Scene"/>
    /// </remarks>
    public IServiceProvider Services => serviceScope.ServiceProvider;

    internal void OnGameSetupScenes(Game game, IServiceProvider gamescope)
    {
        GameLoaded();
    }

    internal void OnGameStopScenes() 
    {
        GameUnloading();
    }

    internal async ValueTask Draw(IDrawQueue queue)
    {
        await Drawing(); 
        
        var pool = ArrayPool<Task>.Shared;
        int toAddToDrawQueue = Nodes.Count;
        Task[] tasks = pool.Rent(toAddToDrawQueue);
        try
        {
            lock (sync)
            {
                for (int i = 0; i < toAddToDrawQueue; i++)
                    tasks[i] = Nodes.Get(i).PropagateDraw();
            }
            for (int i = 0; i < toAddToDrawQueue; i++)
                await tasks[i];
        }
        finally
        {
            pool.Return(tasks, true);
        }
    }
    
    internal async Task Update(TimeSpan delta)
    {
        await Updating(delta);

        var pool = ArrayPool<Task>.Shared;
        int toUpdate = Nodes.Count;
        Task[] tasks = pool.Rent(toUpdate);
        try
        {
            lock (sync)
            {
                for (int i = 0; i < toUpdate; i++)
                    tasks[i] = Nodes.Get(i).PropagateUpdate();
            }
            for (int i = 0; i < toUpdate; i++)
                await tasks[i];
        }
        finally
        {
            pool.Return(tasks, true);
        }

        #region Update batching (old)

        //var t1 = RunAsyncUpdates(AsyncSceneUpdatingEvent1);
        //SceneUpdatingEvent1?.Invoke(delta);
        //if (t1 != null)
        //    await t1;

        //var t2 = RunAsyncUpdates(AsyncSceneUpdatingEvent2);
        //SceneUpdatingEvent2?.Invoke(delta);
        //if (t2 != null)
        //    await t2;

        //var t3 = RunAsyncUpdates(AsyncSceneUpdatingEvent3);
        //SceneUpdatingEvent3?.Invoke(delta);
        //if (t3 != null)
        //    await t3;

        //var t4 = RunAsyncUpdates(AsyncSceneUpdatingEvent4);
        //SceneUpdatingEvent4?.Invoke(delta);
        //if (t4 != null)
        //    await t4;

        //var t5 = RunAsyncUpdates(AsyncSceneUpdatingEvent5);
        //SceneUpdatingEvent5?.Invoke(delta);
        //if (t5 != null)
        //    await t5;

        //var t6 = RunAsyncUpdates(AsyncSceneUpdatingEvent6);
        //SceneUpdatingEvent6?.Invoke(delta);
        //if (t6 != null)
        //    await t6;

        //Task? RunAsyncUpdates(Func<TimeSpan, Task>? ev) 
        //    => ev != null
        //       ? Parallel.ForEachAsync(ev.GetInvocationList(), async (x, ct) => await ((Func<TimeSpan, Task>)x).Invoke(delta))
        //       : null;

        #endregion
    }

    internal async ValueTask Begin()
    {
        await Began();
        SceneBegan?.Invoke(this, Game.TotalTime);
    }

    internal async ValueTask End(Scene next)
    {
        await Ended(next);
        SceneEnded?.Invoke(this, Game.TotalTime);
    }

    internal async ValueTask End()
    {
        await Ended();
        SceneEnded?.Invoke(this, Game.TotalTime);
    }

    #endregion

    #region Node Related

    #region Update Handling

    #region Internally Managed Methods

    internal void InternalSort(Node node)
    {
        Sorted sorted = 0;
        if (node is IAsyncUpdateableNode aun)
        {
            node.AsyncUpdateHandler = SortNode(aun);
            sorted |= Sorted.AsyncUpdate;
        }
        else if (node is IUpdateableNode sun)
        {
            node.UpdateHandler = SortNode(sun);
            sorted |= Sorted.Update;
        }

        if (node is IAsyncDrawableNode adn)
        {
            node.AsyncDrawHandler = SortNode(adn);
            sorted |= Sorted.AsyncDraw;
        }
        else if (node is IDrawableNode sdn)
        {
            node.DrawHandler = SortNode(sdn);
            sorted |= Sorted.Draw;
        }
        node.SortedInto = sorted;
    }

    #endregion

    #endregion

    #region Public methods and properties

    #region Attachment

    /// <summary>
    /// Attaches the given <see cref="Node"/> to this <see cref="Scene"/>
    /// </summary>
    /// <param name="node"></param>
    public void AttachNode(Node node)
    {
        lock (sync)
        {
            node.AttachToScene(this);
            Nodes.Add(node);
            NodeAttached(node);
        }
        ChildAttached?.Invoke(this, Game.TotalTime, node);
    }

    internal void InternalDetachNode(Node node)
    {
        NodeDetached(node);

        Nodes.Remove(node.Index);

        for (int i = 0; i < Nodes.Count; i++)
            Nodes.Get(i).Index = i;

        ChildDetached?.Invoke(this, Game.TotalTime, node);
    }

    #endregion

    /// <summary>
    /// Represents the Children and the overall Node tree of this <see cref="Scene"/>
    /// </summary>
    /// <remarks>
    /// Due to thread safety concerns, the list is locked during reads
    /// </remarks>
    public NodeList Nodes { get; private set; }

    #endregion

    #region Sorters

    /// <summary>
    /// Sorts <paramref name="node"/> into being handled by either a custom update handler or by <see cref="UpdateSynchronousNode(IUpdateableNode)"/>
    /// </summary>
    /// <param name="node">The <see cref="IUpdateableNode"/> node to sort</param>
    /// <returns><c>null</c> to signal the use of <see cref="UpdateSynchronousNode(IUpdateableNode)"/>, a <see cref="NodeSynchronousUpdater{TNode}"/> of the appropriate type otherwise</returns>
    protected virtual NodeSynchronousUpdater? SortNode(IUpdateableNode node) => null;

    /// <summary>
    /// Sorts <paramref name="node"/> into being handled by either a custom update handler or by <see cref="UpdateAsynchronousNode(IAsyncUpdateableNode)"/>
    /// </summary>
    /// <param name="node">The <see cref="IAsyncUpdateableNode"/> node to sort</param>
    /// <returns><c>null</c> to signal the use of <see cref="UpdateAsynchronousNode(IAsyncUpdateableNode)"/>, a <see cref="NodeAsynchronousUpdater{TNode}"/> of the appropriate type otherwise</returns>
    protected virtual NodeAsynchronousUpdater? SortNode(IAsyncUpdateableNode node) => null;

    /// <summary>
    /// Sorts <paramref name="node"/> into being handled by either a custom draw handler or by <see cref="DrawSynchronousNode(IDrawableNode)"/>
    /// </summary>
    /// <param name="node">The <see cref="IDrawableNode"/> node to sort</param>
    /// <returns><c>null</c> to signal the use of <see cref="DrawSynchronousNode(IDrawableNode)"/>, a <see cref="NodeSynchronousDrawer{TNode}"/> of the appropriate type otherwise</returns>
    protected virtual NodeSynchronousDrawer? SortNode(IDrawableNode node) => null;

    /// <summary>
    /// Sorts <paramref name="node"/> into being handled by either a custom draw handler or by <see cref="DrawAsynchronousNode(IAsyncDrawableNode)"/>
    /// </summary>
    /// <param name="node">The <see cref="IAsyncDrawableNode"/> node to sort</param>
    /// <returns><c>null</c> to signal the use of <see cref="DrawAsynchronousNode(IAsyncDrawableNode)"/>, a <see cref="NodeSynchronousDrawer{TNode}"/> of the appropriate type otherwise</returns>
    protected virtual NodeAsynchronousDrawer? SortNode(IAsyncDrawableNode node) => null;

    #endregion

    #region Default Handlers

    /// <summary>
    /// The default handler for <see cref="IUpdateableNode"/>s
    /// </summary>
    /// <param name="node">The node to be updated</param>
    /// <returns><c>true</c> if the update sequence should be propagated into <paramref name="node"/>, <c>false</c> otherwise</returns>
    protected internal virtual bool UpdateSynchronousNode(IUpdateableNode node) => true;

    /// <summary>
    /// The default handler for <see cref="IAsyncUpdateableNode"/>s
    /// </summary>
    /// <param name="node">The node to be updated</param>
    /// <returns><c>true</c> if the update sequence should be propagated into <paramref name="node"/>, <c>false</c> otherwise</returns>
    protected internal virtual Task<bool> UpdateAsynchronousNode(IAsyncUpdateableNode node) => AssortedHelpers.TrueResult;

    /// <summary>
    /// The default handler for <see cref="IDrawableNode"/>s
    /// </summary>
    /// <param name="node">The node to be updated</param>
    /// <returns><c>true</c> if the update sequence should be propagated into <paramref name="node"/>, <c>false</c> otherwise</returns>
    protected internal virtual bool DrawSynchronousNode(IDrawableNode node) => true;

    /// <summary>
    /// The default handler for <see cref="IAsyncDrawableNode"/>s
    /// </summary>
    /// <param name="node">The node to be updated</param>
    /// <returns><c>true</c> if the update sequence should be propagated into <paramref name="node"/>, <c>false</c> otherwise</returns>
    protected internal virtual Task<bool> DrawAsynchronousNode(IAsyncDrawableNode node) => AssortedHelpers.TrueResult;

    #endregion

    #endregion

    #region Public overridable methods

    /// <summary>
    /// Runs when the <see cref="Game"/> is loaded and this (and all other) <see cref="Scene"/>s should be set up
    /// </summary>
    /// <remarks>
    /// The <see cref="Game"/> automatically takes care of both <see cref="Scene"/>s that were instanced before the <see cref="Game"/> was loaded and <see cref="Scene"/>s that were instanced afterwards
    /// </remarks>
    protected virtual void GameLoaded() { }

    /// <summary>
    /// Runs when the <see cref="Game"/> is about to be unloaded (before <see cref="Game.GameUnloading"/> is fired) and this (and all other) <see cref="Scene"/>s should stop as well
    /// </summary>
    protected virtual void GameUnloading() { }

    /// <summary>
    /// Runs when this <see cref="Scene"/> is about to be updated
    /// </summary>
    /// <remarks>
    /// A <see cref="Scene"/> should allow its child <see cref="Node"/>s to update themselves. As such, this method provides no power over nodes already registered to be updated. This method is called before <see cref="Node"/>s are updated
    /// </remarks>
    /// <param name="delta">The amount of time that has passed since the last time this <see cref="Scene"/> was updated</param>
    /// <returns>If this method is async, mark it as such. Otherwise, returning <see cref="ValueTask.CompletedTask"/> is enough</returns>
    protected virtual ValueTask Updating(TimeSpan delta) => ValueTask.CompletedTask;

    /// <summary>
    /// Runs when this <see cref="Scene"/>'s nodes are about to be added to the draw queue
    /// </summary>
    /// <remarks>
    /// A <see cref="Scene"/> should allow its child <see cref="Node"/>s to draw themselves. As such, this method provides no power over nodes already registered to the <see cref="IDrawQueue"/>. This method is called before <see cref="Node"/>s are added to the draw queue
    /// </remarks>
    /// <returns>If this method is async, mark it as such. Otherwise, returning <see cref="ValueTask.CompletedTask"/> is enough</returns>
    protected virtual ValueTask Drawing() => ValueTask.CompletedTask;

    /// <summary>
    /// Runs when a <see cref="Node"/> is attached into this <see cref="Scene"/>'s node tree
    /// </summary>
    /// <remarks>
    /// This method only takes into account <see cref="Node"/>s that are directly being attached to this <see cref="Scene"/>, and not <see cref="Node"/>s that are attached to other <see cref="Node"/>s
    /// </remarks>
    /// <param name="child">The newly attached child</param>
    protected virtual void NodeAttached(Node child) { }

    /// <summary>
    /// Runs when a <see cref="Node"/> is detached from this <see cref="Scene"/>'s node tree
    /// </summary>
    /// <remarks>
    /// This method only takes into account <see cref="Node"/>s that were directly attached to this <see cref="Scene"/>, and not <see cref="Node"/>s that were attached to other <see cref="Node"/>s
    /// </remarks>
    /// <param name="child">The now detached child</param>
    protected virtual void NodeDetached(Node child) { }

    /// <summary>
    /// Runs when this <see cref="Scene"/> begins
    /// </summary>
    /// <returns>If this method is async, mark it as such. Otherwise, returning <see cref="ValueTask.CompletedTask"/> is enough</returns>
    protected virtual ValueTask Began() => ValueTask.CompletedTask;

    /// <summary>
    /// Runs when this <see cref="Scene"/> is ended, and the next one is about to begin
    /// </summary>
    /// <param name="next">The next scene to begin</param>
    /// <returns>If this method is async, mark it as such. Otherwise, returning <see cref="ValueTask.CompletedTask"/> is enough</returns>
    protected virtual ValueTask Ended(Scene next) => ValueTask.CompletedTask;

    /// <summary>
    /// Runs when this <see cref="Scene"/> is ended, and the <see cref="Game"/> is stopping
    /// </summary>
    /// <returns>If this method is async, mark it as such. Otherwise, returning <see cref="ValueTask.CompletedTask"/> is enough</returns>
    protected virtual ValueTask Ended() => ValueTask.CompletedTask;

    #endregion

    #region Events

    /// <summary>
    /// Fired when the <see cref="Scene"/> begins and is set on <see cref="Game.CurrentScene"/>
    /// </summary>
    public event SceneEvent? SceneBegan;

    /// <summary>
    /// Fired when the <see cref="Scene"/> ends and is withdrawn from <see cref="Game.CurrentScene"/>
    /// </summary>
    public event SceneEvent? SceneEnded;

    /// <summary>
    /// Fired when this <see cref="Scene"/> has a <see cref="Node"/> attached as its direct child
    /// </summary>
    public event SceneNodeEvent? ChildAttached;

    /// <summary>
    /// Fired when this <see cref="Scene"/> has a <see cref="Node"/> detached from being its direct child
    /// </summary>
    public event SceneNodeEvent? ChildDetached;

    #endregion

    #region Private events

    internal event Action<IDrawQueue>? SceneDrawingEvent;
    internal event Func<IDrawQueue, Task>? AsyncSceneDrawingEvent;

    internal event Action<TimeSpan>? SceneUpdatingEvent1;
    internal event Action<TimeSpan>? SceneUpdatingEvent2;
    internal event Action<TimeSpan>? SceneUpdatingEvent3;
    internal event Action<TimeSpan>? SceneUpdatingEvent4;
    internal event Action<TimeSpan>? SceneUpdatingEvent5;
    internal event Action<TimeSpan>? SceneUpdatingEvent6;

    internal event Func<TimeSpan, Task>? AsyncSceneUpdatingEvent1;
    internal event Func<TimeSpan, Task>? AsyncSceneUpdatingEvent2;
    internal event Func<TimeSpan, Task>? AsyncSceneUpdatingEvent3;
    internal event Func<TimeSpan, Task>? AsyncSceneUpdatingEvent4;
    internal event Func<TimeSpan, Task>? AsyncSceneUpdatingEvent5;
    internal event Func<TimeSpan, Task>? AsyncSceneUpdatingEvent6;

    #endregion

    #region IDisposable

    private bool disposedValue;

    private void InternalDispose(bool disposing)
    {
        lock (sync)
        {
            if (!disposedValue)
            {
                Dispose(disposing);

                for (int i = 0; i < Nodes.Count; i++)
                    Nodes[i].Dispose();

                serviceScope.Dispose();

                Nodes = null!;

                disposedValue = true;
            }
        }
    }

    /// <summary>
    /// Runs when the object is being disposed. Don't call this! It'll be called automatically! Call <see cref="Dispose()"/> instead
    /// </summary>
    /// <remarks>
    /// This <see cref="Node"/> will dispose of all of its children (and those children of theirs)
    /// </remarks>
    protected virtual void Dispose(bool disposing) { }

    /// <summary>
    /// Disposes of this <see cref="Scene"/> and all of its currently attached children
    /// </summary>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        InternalDispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
