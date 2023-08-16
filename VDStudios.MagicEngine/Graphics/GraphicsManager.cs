using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using VDStudios.MagicEngine.Exceptions;
using VDStudios.MagicEngine.Input;
using VDStudios.MagicEngine.Internal;

namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// Represents a Thread dedicated solely to handling a window or equivalent, and managing their respective resources in a thread-safe manner
/// </summary>
/// <remarks>
/// *ALL* Graphics Managers are automatically managed by <see cref="Game"/>, registered at the time of construction
/// </remarks>
public abstract class GraphicsManager<TGraphicsContext> : GraphicsManager, IDisposable
    where TGraphicsContext : GraphicsContext<TGraphicsContext>
{
    #region Construction

    /// <summary>
    /// Represents the <see cref="DeferredExecutionSchedule"/> tied to this <see cref="GraphicsManager{TGraphicsContext}"/>, and can be used to defer calls that should run under this <see cref="GraphicsManager{TGraphicsContext}"/>'s loop. 
    /// </summary>
    /// <remarks>
    /// This schedule is updated every frame, as such, it's subject to the framerate of this <see cref="GraphicsManager{TGraphicsContext}"/> ---- Not to be confused with <see cref="Game.DeferredCallSchedule"/> (or <see cref="GameObject.GameDeferredCallSchedule"/>, which is the same)
    /// </remarks>
    public DeferredExecutionSchedule DeferredCallSchedule { get; }

    /// <summary>
    /// The updater for <see cref="DeferredCallSchedule"/>
    /// </summary>
    /// <remarks>
    /// Must be called regularly
    /// </remarks>
    protected readonly Func<ValueTask> DeferredCallScheduleUpdater;

    /// <summary>
    /// Instances and constructs a new <see cref="GraphicsManager{TGraphicsContext}"/> object
    /// </summary>
    public GraphicsManager() : base()
    {
        initLock.Wait();
        Game.graphicsManagersAwaitingSetup.Enqueue(this);

        RenderTargets = new(this);

        CurrentSnapshot = CreateNewEmptyInputSnapshot();
        snapshotBuffer = CreateNewEmptyInputSnapshot();

        framelockWaiter = new(FrameLock);
        drawlockWaiter = new(DrawLock);

        DeferredCallSchedule = new DeferredExecutionSchedule(out DeferredCallScheduleUpdater);

        IsRunningCheck = () => IsRunning;
        IsNotRenderingCheck = () => !IsRendering;
    }

    private readonly SemaphoreSlim initLock = new(1, 1);

    internal void WaitForInit()
    {
        initLock.Wait();
        initLock.Release();
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Whether the <see cref="GraphicsManager{TGraphicsContext}"/> should stop rendering when it loses input focus
    /// </summary>
    public bool PauseOnInputLoss { get; set; }

    /// <summary>
    /// This <see cref="GraphicsManager{TGraphicsContext}"/>'s render targets
    /// </summary>
    public RenderTargetList<TGraphicsContext> RenderTargets { get; }

    /// <summary>
    /// Represents the current Frames-per-second value calculated while this <see cref="GraphicsManager{TGraphicsContext}"/> is running
    /// </summary>
    public float FramesPerSecond => fak.Average;
    private readonly FloatAverageKeeper fak = new(10);

    #endregion

    #region Input Management

    private readonly InputSnapshot snapshotBuffer;
    private InputSnapshot CurrentSnapshot;

    /// <summary>
    /// The <see cref="InputSnapshot"/> pool for this <see cref="GraphicsManager{TGraphicsContext}"/>
    /// </summary>
    protected readonly Queue<InputSnapshot> SnapshotPool = new(3);

    internal void ReturnSnapshot(InputSnapshot snapshot)
    {
        lock (SnapshotPool)
        {
            snapshot.Clear();
            SnapshotPool.Enqueue(snapshot);
        }
    }

    /// <summary>
    /// Creates a new Empty <see cref="InputSnapshot"/> to be added to the SnapshotPool
    /// </summary>
    /// <returns></returns>
    protected abstract InputSnapshot CreateNewEmptyInputSnapshot();

    internal InputSnapshot FetchSnapshot()
    {
        lock (SnapshotPool)
        {
            var ret = CurrentSnapshot;
            if (!SnapshotPool.TryDequeue(out var snap))
                snap = CreateNewEmptyInputSnapshot();
            CurrentSnapshot = snap;
            ret.CopyTo(snap);
            ret.FetchLastMomentData();
            return ret;
        }
    }

    /// <summary>
    /// Reports a mouse wheel event to the current staging snapshot
    /// </summary>
    /// <param name="mouseId">The id of the mouse if applicable</param>
    /// <param name="delta">The mouse's wheel delta</param>
    protected void ReportMouseWheelEvent(uint mouseId, Vector2 delta)
    {
        lock (SnapshotPool)
            snapshotBuffer.ReportMouseWheelMoved(mouseId, delta);
    }

    /// <summary>
    /// Reports a mouse event to the current staging snapshot
    /// </summary>
    /// <param name="mouseId">The id of the mouse if applicable</param>
    /// <param name="delta">A <see cref="Vector2"/> representing how much did the mouse move in each axis</param>
    /// <param name="newPosition">The mouse's current position at the time of this event</param>
    /// <param name="pressed">The buttons that were pressed at the time of this event</param>
    protected void ReportMouseMoved(uint mouseId, Vector2 delta, Vector2 newPosition, MouseButton pressed)
    {
        lock (SnapshotPool)
            snapshotBuffer.ReportMouseMoved(mouseId, delta, newPosition, pressed);
    }

    /// <summary>
    /// Reports a mouse event to the current staging snapshot
    /// </summary>
    /// <param name="mouseId">The id of the mouse if applicable</param>
    /// <param name="clicks">The amount of clicks, if any, that were done in this event</param>
    /// <param name="state">The mouse's current state</param>
    protected void ReportMouseButtonReleased(uint mouseId, int clicks, MouseButton state)
    {
        lock (SnapshotPool)
            snapshotBuffer.ReportMouseButtonReleased(mouseId, clicks, state);
    }

    /// <summary>
    /// Reports a mouse event to the current staging snapshot
    /// </summary>
    /// <param name="mouseId">The id of the mouse if applicable</param>
    /// <param name="clicks">The amount of clicks, if any, that were done in this event</param>
    /// <param name="state">The mouse's current state</param>
    protected void ReportMouseButtonPressed(uint mouseId, int clicks, MouseButton state)
    {
        lock (SnapshotPool)
            snapshotBuffer.ReportMouseButtonPressed(mouseId, clicks, state);
    }

    /// <summary>
    /// Reports a keyboard event to the current staging snapshot
    /// </summary>
    /// <param name="keyboardId">The id of the keyboard, if applicable</param>
    /// <param name="scancode">The scancode of the key released</param>
    /// <param name="key">The keycode of the key released</param>
    /// <param name="modifiers">Any modifiers that may have been released at the time this key was released</param>
    /// <param name="isPressed">Whether the key was released at the time of this event. Usually <see langword="false"/></param>
    /// <param name="repeat">Whether this keypress is a repeat, i.e. it's being held down</param>
    /// <param name="unicode">The unicode value for key that was released</param>
    protected void ReportKeyReleased(uint keyboardId, Scancode scancode, Keycode key, KeyModifier modifiers, bool isPressed, bool repeat, uint unicode)
    {
        lock (SnapshotPool)
            snapshotBuffer.ReportKeyReleased(keyboardId, scancode, key, modifiers, false, repeat, unicode);
    }

    /// <summary>
    /// Reports a keyboard event to the current staging snapshot
    /// </summary>
    /// <param name="keyboardId">The id of the keyboard, if applicable</param>
    /// <param name="scancode">The scancode of the key pressed</param>
    /// <param name="key">The keycode of the key pressed</param>
    /// <param name="modifiers">Any modifiers that may have been pressed at the time this key was pressed</param>
    /// <param name="isPressed">Whether the key was pressed at the time of this event. Usually <see langword="true"/></param>
    /// <param name="repeat">Whether this keypress is a repeat, i.e. it's being held down</param>
    /// <param name="unicode">The unicode value for key that was pressed</param>
    protected void ReportKeyPressed(uint keyboardId, Scancode scancode, Keycode key, KeyModifier modifiers, bool isPressed, bool repeat, uint unicode)
    {
        lock (SnapshotPool)
            snapshotBuffer.ReportKeyPressed(keyboardId, scancode, key, modifiers, false, repeat, unicode);
    }

    #endregion

    #region Waiting

    #region DrawLock
    private readonly Internal_IdleWaiter drawlockWaiter;

    /// <summary>
    /// Waits until the Manager finishes drawing its <see cref="DrawOperation{TGraphicsContext}"/>s and locks it
    /// </summary>
    /// <remarks>
    /// *ALWAYS* wrap the disposable object this method returns in an using statement. The <see cref="GraphicsManager{TGraphicsContext}"/> will stay locked forever if it's not disposed of
    /// </remarks>
    /// <returns>An <see cref="IDisposable"/> object that unlocks the manager when disposed of</returns>
    public IDisposable LockManagerDrawing()
    {
        DrawLock.Wait();
        return drawlockWaiter;
    }

    /// <summary>
    /// Asynchronously waits until the Manager finishes drawing its <see cref="DrawOperation{TGraphicsContext}"/>s and locks it
    /// </summary>
    /// <remarks>
    /// *ALWAYS* wrap the disposable object this method returns in an using statement. The <see cref="GraphicsManager{TGraphicsContext}"/> will stay locked forever if it's not disposed of
    /// </remarks>
    /// <returns>An <see cref="IDisposable"/> object that unlocks the manager when disposed of</returns>
    public async Task<IDisposable> LockManagerDrawingAsync()
    {
        await DrawLock.WaitAsync();
        return drawlockWaiter;
    }

    #endregion

    #region FrameLock
    private readonly Internal_IdleWaiter framelockWaiter;

    /// <summary>
    /// Waits until the Manager finishes drawing the current frame and locks it
    /// </summary>
    /// <remarks>
    /// Neither general drawing or GUI will be drawn until the frame is released. *ALWAYS* wrap the disposable object this method returns in an using statement. The <see cref="GraphicsManager{TGraphicsContext}"/> will stay locked forever if it's not disposed of
    /// </remarks>
    /// <returns>An <see cref="IDisposable"/> object that unlocks the manager when disposed of</returns>
    public IDisposable LockManagerFrame()
    {
        FrameLock.Wait();
        return framelockWaiter;
    }

    /// <summary>
    /// Asynchronously waits until the Manager finishes drawing the current frame and locks it
    /// </summary>
    /// <remarks>
    /// Neither general drawing or GUI will be drawn until the frame is released. *ALWAYS* wrap the disposable object this method returns in an using statement. The <see cref="GraphicsManager{TGraphicsContext}"/> will stay locked forever if it's not disposed of
    /// </remarks>
    /// <returns>An <see cref="IDisposable"/> object that unlocks the manager when disposed of</returns>
    public async Task<IDisposable> LockManagerFrameAsync()
    {
        await FrameLock.WaitAsync();
        return framelockWaiter;
    }

    #endregion

    private sealed class Internal_IdleWaiter : IDisposable
    {
        private readonly SemaphoreSlim @lock;

        public Internal_IdleWaiter(SemaphoreSlim fl) => @lock = fl;

        public void Dispose()
        {
            @lock.Release();
        }
    }

    #endregion

    #region Running

    private Task graphics_thread;

    internal async ValueTask AwaitIfFaulted()
    {
        if (graphics_thread.IsFaulted)
            await graphics_thread;
    }

    #region Public Properties

    /// <summary>
    /// Whether this <see cref="GraphicsManager{TGraphicsContext}"/> is currently running
    /// </summary>
    public bool IsRunning { get; private set; } = true;

    /// <summary>
    /// The color to draw when the frame is beginning to be drawn
    /// </summary>
    public abstract RgbaVector BackgroundColor { get; set; } 

    #endregion

    #region Public Methods

    ///// <summary>
    ///// Attempts to stop the current <see cref="GraphicsManager{TGraphicsContext}"/>'s draw loop
    ///// </summary>
    ///// <returns><c>true</c> if the <see cref="GraphicsManager{TGraphicsContext}"/> was succesfully stopped, <c>false</c> otherwise</returns>
    //public bool TryPause()
    //{
    //    lock (sync)
    //        if (!IsRunning)
    //            throw new InvalidOperationException($"Can't stop a GraphicsManager<TGraphicsContext> that is not running");

    //    if (TryingToStop())
    //    {
    //        lock (sync)
    //            IsRunning = false;

    //        Stopped?.Invoke(this, Game.TotalTime, false);
    //        RunStateChanged?.Invoke(this, Game.TotalTime, false);
    //        return true;
    //    }

    //    return false;
    //}

    #endregion

    #region Reaction Methods

    ///// <summary>
    ///// This method is called automatically when this <see cref="GraphicsManager{TGraphicsContext}"/> has received a request to stop
    ///// </summary>
    ///// <returns><c>true</c> if the <see cref="GraphicsManager{TGraphicsContext}"/> should be allowed to stop, <c>false</c> otherwise</returns>
    //protected virtual bool TryingToStop() => true;

    /// <summary>
    /// This method is called automatically when this <see cref="GraphicsManager{TGraphicsContext}"/> is about to start
    /// </summary>
    /// <remarks>
    /// A <see cref="GraphicsManager{TGraphicsContext}"/> starts only at the beginning of the first frame it can, and it's done from the default thread
    /// </remarks>
    protected virtual void Starting() { }

    /// <summary>
    /// This method is called automatically every frame this <see cref="GraphicsManager{TGraphicsContext}"/> is active
    /// </summary>
    protected virtual void Running() { }

    #endregion

    #region Internal

    /// <summary>
    /// Locked while the Window is being shown
    /// </summary>
    protected readonly SemaphoreSlim WindowShownLock = new(1, 1);

    /// <summary>
    /// Locked while the frame is being processed
    /// </summary>
    protected readonly SemaphoreSlim FrameLock = new(1, 1);

    /// <summary>
    /// Locked while the frame is being drawn into
    /// </summary>
    /// <remarks>
    /// Locked while <see cref="FrameLock"/> is locked
    /// </remarks>
    protected readonly SemaphoreSlim DrawLock = new(1, 1);

    /// <summary>
    /// The current Window's Size
    /// </summary>
    public abstract IntVector2 WindowSize { get; protected set; }

    /// <summary>
    /// Handles the Window's size changing
    /// </summary>
    /// <param name="newSize"></param>
    protected internal void InternalWindowSizeChanged(IntVector2 newSize)
    {
        IntVector2 oldSize;
        FrameLock.Wait();
        try
        {
            var (ww, wh) = newSize;
            InternalLog?.Information("Window size changed to {{w:{width}, h:{height}}}", newSize.X, newSize.Y);

            oldSize = WindowSize;
            WindowSize = newSize;
            WindowView = Matrix4x4.CreateScale(wh / (float)ww, 1, 1);

            WindowSizeChangedFrameLocked(oldSize, newSize);
        }
        finally
        {
            FrameLock.Release();
        }

        WindowSizeChanged(oldSize, newSize);
    }

    /// <summary>
    /// This method is called automatically when a window changes its size
    /// </summary>
    /// <remarks>
    /// This method is called while the frame is still locked, hence before <see cref="WindowSizeChanged(IntVector2, IntVector2)"/>
    /// </remarks>
    protected virtual void WindowSizeChangedFrameLocked(IntVector2 oldSize, IntVector2 newSize) { }

    /// <summary>
    /// This method is called automatically when the window changes its size
    /// </summary>
    /// <remarks>
    /// This method is called after the frame is unlocked, hence after <see cref="WindowSizeChangedFrameLocked(IntVector2, IntVector2)"/>
    /// </remarks>
    protected virtual void WindowSizeChanged(IntVector2 oldSize, IntVector2 newSize) { }

#warning Revisit this
    ///////// <summary>
    ///////// Gets the <see cref="IDrawableNode{TGraphicsContext}"/> from <see cref="Game.CurrentScene"/> that belong to this <see cref="GraphicsManager{TGraphicsContext}"/>
    ///////// </summary>
    //////public IEnumerable<IDrawableNode<TGraphicsContext>> GetDrawableNodesFromCurrentScene()
    //////    => GetDrawableNodes(Game.CurrentScene);

    ///////// <summary>
    ///////// Gets the <see cref="IDrawableNode{TGraphicsContext}"/> from <paramref name="scene"/> that belong to this <see cref="GraphicsManager{TGraphicsContext}"/>
    ///////// </summary>
    ///////// <param name="scene">The <see cref="Scene"/> containing the drawable nodes</param>
    //////public IEnumerable<IDrawableNode<TGraphicsContext>> GetDrawableNodes(Scene scene)
    //////    => scene.GetDrawableNodes<TGraphicsContext>(this);

    /// <summary>
    /// This is run from the update thread
    /// </summary>
    [MemberNotNull(nameof(graphics_thread))]
    internal void InternalStart()
    {
        InternalLog?.Information("Starting GraphicsManager");
        Starting();

        InternalLog?.Information("Setting up Window");
        SetupGraphicsManager();
        InternalLog?.Information("Running GraphicsManager");
        graphics_thread = Task.Run(Run);

        initLock.Release();
    }

    /// <summary>
    /// Fetches a graphics context for rendering. This can either be a reused context or a brand new one.
    /// </summary>
    /// <remarks>
    /// Reference this method only to implement it -- Use it only through <see cref="GetGraphicsContext"/>
    /// </remarks>
    protected abstract ValueTask<TGraphicsContext> FetchGraphicsContext();

    /// <summary>
    /// Obtains a new <see cref="GraphicsContext{TSelf}"/>
    /// </summary>
    /// <remarks>
    /// Should be used over <see cref="FetchGraphicsContext"/>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected async ValueTask<TGraphicsContext> GetGraphicsContext()
    {
        var ctxt = await FetchGraphicsContext();
        Debug.Assert(ctxt.Manager == this, "The fetched GraphicsContext does not belong to this GraphicsManager");
        return ctxt;
    }

    /// <summary>
    /// Waits for a <see cref="SemaphoreSlim"/> lock to be unlocked and adquires the lock if possible
    /// </summary>
    /// <param name="semaphore">The <see cref="SemaphoreSlim"/> to wait on</param>
    /// <param name="condition">The condition to continue waiting for the semaphore. If <see langword="true"/> the method will continue waiting until the other parameters are up, otherwise, it will break immediately.</param>
    /// <param name="syncWait">The amount of milliseconds to wait synchronously before to checking on the condition</param>
    /// <param name="asyncWait">The amount of milliseconds to wait asynchronously before checking on the condition</param>
    /// <param name="syncRepeats">The amount of times to wait <paramref name="syncWait"/> milliseconds (and checking the condition) before switching to asynchronous waiting</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to cancel the operation</param>
    /// <returns><see langword="true"/> if <paramref name="semaphore"/>'s lock was adquired, <see langword="false"/> otherwise</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static async ValueTask<bool> WaitOn(SemaphoreSlim semaphore, Func<bool> condition, int syncWait = 15, int asyncWait = 50, int syncRepeats = 10, CancellationToken ct = default)
    {
        while (syncRepeats-- > 0)
            if (!semaphore.Wait(syncWait, ct))
            {
                if (!condition())
                    return false;
            }
            else //succesfully adquired the lock
                return true;
        // ran out of repeats without adquiring the lock

        while (!await semaphore.WaitAsync(asyncWait, ct))
            if (!condition())
                return false;
        return true;
    }

    /// <summary>
    /// A transformation Matrix that represents the current Window's dimensions
    /// </summary>
    public Matrix4x4 WindowView { get; private set; }

    /// <summary>
    /// A check to see if this <see cref="GraphicsManager{TGraphicsContext}"/> is running
    /// </summary>
    protected readonly Func<bool> IsRunningCheck;

    /// <summary>
    /// A check to see if this <see cref="GraphicsManager{TGraphicsContext}"/> is not rendering
    /// </summary>
    protected readonly Func<bool> IsNotRenderingCheck;

    /// <summary>
    /// The main running method of this <see cref="GraphicsManager{TGraphicsContext}"/>
    /// </summary>
    /// <remarks>
    /// This is a very advanced method, the entire state of the GraphicsManager is dependent on this method and thus must be understood perfectly. This method will be called strictly once, as it is expected the rendering loop to run within it
    /// </remarks>
    protected abstract Task Run();

    #endregion

    #region Events

    ///// <summary>
    ///// Fired when <see cref="GraphicsManager{TGraphicsContext}"/> start
    ///// </summary>
    ///// <remarks>
    ///// Specifically, when <see cref="IsRunning"/> is set to <c>true</c>
    ///// </remarks>
    //public GraphicsManager<TGraphicsContext>RunStateChanged? Started;

    ///// <summary>
    ///// Fired when <see cref="GraphicsManager{TGraphicsContext}"/> stops
    ///// </summary>
    ///// <remarks>
    ///// Specifically, when <see cref="IsRunning"/> is set to <c>false</c>
    ///// </remarks>
    //public GraphicsManager<TGraphicsContext>RunStateChanged? Stopped;

    ///// <summary>
    ///// Fired when <see cref="IsRunning"/> changes
    ///// </summary>
    ///// <remarks>
    ///// Fired after <see cref="Started"/> or <see cref="Stopped"/>
    ///// </remarks>
    //public GraphicsManager<TGraphicsContext>RunStateChanged? RunStateChanged;

    #endregion

    #endregion

    #region Setup Methods

    /// <summary>
    /// Performs task such as Creating and Setting up the Window
    /// </summary>
    [MemberNotNull(nameof(graphics_thread))]
    protected abstract void SetupGraphicsManager();

    /// <summary>
    /// <c>true</c> if this GraphicsManager is currently visible, or shown. <c>false</c> otherwise.
    /// </summary>
    /// <remarks>
    /// This <see cref="GraphicsManager{TGraphicsContext}"/> will *NOT* run or update while this GraphicsManager is inactive
    /// </remarks>
    public bool IsWindowAvailable { get; private set; }

    /// <summary>
    /// <c>true</c> if this GraphicsManager currently has input focus. <c>false</c> otherwise
    /// </summary>
    public bool HasFocus { get; private set; }

    /// <summary>
    /// Whether the current <see cref="GraphicsManager{TGraphicsContext}"/> should currently be updating or not
    /// </summary>
    public bool IsRendering => IsWindowAvailable && (!PauseOnInputLoss || HasFocus);

    /// <summary>
    /// Updates the window
    /// </summary>
    /// <param name="isAvailable"></param>
    protected void UpdateWindowAvailability(bool isAvailable)
    {
        IsWindowAvailable = isAvailable;
        lock (SnapshotPool)
            snapshotBuffer.butt = 0;
    }

    #endregion

    #region Disposal

    internal override void InternalDispose(bool disposing)
    {
        IsRunning = false;
        FrameLock.Wait();
        try
        {
            FramelockedDispose(disposing);
        }
        finally
        {
            FrameLock.Release();
        }
        base.InternalDispose(disposing);
    }

    /// <summary>
    /// Performs disposal tasks while the Frame is locked
    /// </summary>
    protected abstract void FramelockedDispose(bool disposing);

    #endregion
}
