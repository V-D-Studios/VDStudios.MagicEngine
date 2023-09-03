using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using VDStudios.MagicEngine.Input;
using VDStudios.MagicEngine.Internal;

namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// Represents an object dedicated solely to handling a window or equivalent, and managing their respective resources in a thread-safe manner
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
    public GraphicsManager(Game game) : base(game)
    {
        renderTargets = new();
        Debug.Assert(CreateRenderTargetList(0), "CreateRenderTargetList unexpectedly returned false within the constructor when creating RenderLevel 0");

        framelockWaiter = new(FrameLock);
        drawlockWaiter = new(DrawLock);

        DeferredCallSchedule = new DeferredExecutionSchedule(out DeferredCallScheduleUpdater);

        IsRunningCheck = () => IsRunning;
        IsNotRenderingCheck = () => !IsRendering;
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Whether the <see cref="GraphicsManager{TGraphicsContext}"/> should stop rendering when it loses input focus
    /// </summary>
    public bool PauseOnInputLoss { get; set; }

    private readonly List<uint> renderTargetLevels = new();
    private readonly Dictionary<uint, RenderTargetList<TGraphicsContext>> renderTargets;

    /// <summary>
    /// Gets this <see cref="GraphicsManager{TGraphicsContext}"/>'s render targets for level <paramref name="renderLevel"/>
    /// </summary>
    /// <remarks>
    /// RenderLevels are a concept that permit different layers of <see cref="RenderTarget{TGraphicsContext}"/>s to work together seamlessly. These levels are ordered by their numeric key. One example of render levels would be a level 0 used for background rendering that is rendered using special targets and is always rendered first (below), level 1 for normal game objects using a camera render target, and level 2 for GUI elements using yet another set of RenderTargets (like a single render target that does not transform the operation at all)
    /// </remarks>
    /// <returns>The <see cref="RenderTargetList{TGraphicsContext}"/> associated to <paramref name="renderLevel"/>, or <see langword="null"/> if no <see cref="RenderTargetList{TGraphicsContext}"/> exists for <paramref name="renderLevel"/></returns>
    public RenderTargetList<TGraphicsContext>? GetRenderTargetList(uint renderLevel)
    {
        lock (renderTargetLevels)
            return renderTargets.TryGetValue(renderLevel, out var v) ? v : null;
    }

    /// <summary>
    /// Gets this <see cref="GraphicsManager{TGraphicsContext}"/>'s render targets for level <paramref name="renderLevel"/>, or creates one such list if it doesn't exist
    /// </summary>
    /// <remarks>
    /// RenderLevels are a concept that permit different layers of <see cref="RenderTarget{TGraphicsContext}"/>s to work together seamlessly. These levels are ordered by their numeric key. One example of render levels would be a level 0 used for background rendering that is rendered using special targets and is always rendered first (below), level 1 for normal game objects using a camera render target, and level 2 for GUI elements using yet another set of RenderTargets (like a single render target that does not transform the operation at all)
    /// </remarks>
    /// <returns>The <see cref="RenderTargetList{TGraphicsContext}"/> associated to <paramref name="renderLevel"/>, or <see langword="null"/> if no <see cref="RenderTargetList{TGraphicsContext}"/> exists for <paramref name="renderLevel"/></returns>
    public RenderTargetList<TGraphicsContext> GetOrCreateRenderTargetList(uint renderLevel)
    {
        lock (renderTargetLevels)
            return renderTargets.TryGetValue(renderLevel, out var v) ? v : AddRenderTargetList(renderLevel);
    }

    /// <summary>
    /// Throws a new <see cref="ArgumentException"/> if this <see cref="GraphicsManager"/> does not have a <see cref="RenderTargetList{TGraphicsContext}"/> for RenderLevel <paramref name="renderLevel"/>. Does nothing otherwise.
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public void ThrowIfRenderLevelNotRegistered(uint renderLevel)
    {
        if (GetRenderTargetList(renderLevel) is null)
            throw new ArgumentException($"The target GraphicsManager does not have a RenderLevel {renderLevel}", nameof(renderLevel));
    }

    /// <summary>
    /// Creates a new <see cref="RenderTargetList{TGraphicsContext}"/> for level <paramref name="renderLevel"/>
    /// </summary>
    /// <remarks>
    /// RenderLevels are a concept that permit different layers of <see cref="RenderTarget{TGraphicsContext}"/>s to work together seamlessly. These levels are ordered by their numeric key. One example of render levels would be a level 0 used for background rendering that is rendered using special targets and is always rendered first (below), level 1 for normal game objects using a camera render target, and level 2 for GUI elements using yet another set of RenderTargets (like a single render target that does not transform the operation at all)
    /// </remarks>
    /// <param name="renderLevel">The render level for the <see cref="RenderTargetList{TGraphicsContext}"/> to be created</param>
    /// <returns><see langword="true"/> if the <see cref="RenderTargetList{TGraphicsContext}"/> was succesfully created, <see langword="false"/> if a <see cref="RenderTargetList{TGraphicsContext}"/> for that <paramref name="renderLevel"/> already existed</returns>
    public bool CreateRenderTargetList(uint renderLevel)
    {
        lock (renderTargetLevels)
        {
            if (renderTargets.ContainsKey(renderLevel))
                return false;
            AddRenderTargetList(renderLevel);
            return true;
        }
    }

    private RenderTargetList<TGraphicsContext> AddRenderTargetList(uint renderLevel)
    {
        var rtl = new RenderTargetList<TGraphicsContext>(this);
        renderTargets.Add(renderLevel, rtl);
        renderTargetLevels.Add(renderLevel);
        renderTargetLevels.Sort();
        return rtl;
    }

    /// <summary>
    /// Copies all RenderTargetLists in this <see cref="GraphicsManager{TGraphicsContext}"/> over into <paramref name="buffer"/>
    /// </summary>
    /// <remarks>
    /// This method clears <paramref name="buffer"/> before using it
    /// </remarks>
    protected void BufferRenderTargets(List<(uint Level, RenderTargetList<TGraphicsContext> Targets)> buffer)
    {
        lock (renderTargetLevels)
        {
            buffer.Clear();
            foreach (var i in renderTargetLevels)
            {
                var rtl = GetRenderTargetList(i);
                Debug.Assert(rtl != null, "GetRenderTargets returned a null value for a Render Target Level that was registered");
                buffer.Add((i, rtl));
            }
        }
    }

    /// <summary>
    /// Represents the current Frames-per-second value calculated while this <see cref="GraphicsManager{TGraphicsContext}"/> is running
    /// </summary>
    public float FramesPerSecond => DeltaAverageKeeper.Average;

    /// <summary>
    /// The object that maintains the information that is fed to <see cref="FramesPerSecond"/>
    /// </summary>
    protected readonly FloatAverageKeeper DeltaAverageKeeper = new(10);

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

    #region Public Properties

    /// <summary>
    /// Whether this <see cref="GraphicsManager{TGraphicsContext}"/> is currently running
    /// </summary>
    public bool IsRunning { get; protected set; } = true;

    /// <summary>
    /// The color to draw when the frame is beginning to be drawn
    /// </summary>
    public virtual RgbaVector BackgroundColor { get; set; } = RgbaVector.CornflowerBlue;

    #endregion

    #region Reaction Methods

    /// <summary>
    /// This method is called automatically every frame this <see cref="GraphicsManager{TGraphicsContext}"/> is active, right before <see cref="WindowShownLock"/> is released
    /// </summary>
    protected virtual void BeforeWindowRelease() { }

    /// <summary>
    /// This method is called automatically every frame this <see cref="GraphicsManager{TGraphicsContext}"/> is active, right before <see cref="FrameLock"/> is released
    /// </summary>
    protected virtual void BeforeFrameRelease() { }

    /// <summary>
    /// This method is called automatically every frame this <see cref="GraphicsManager{TGraphicsContext}"/> is active, right the frame is submited through the <see cref="GraphicsContext{TSelf}.EndAndSubmitFrame"/>
    /// </summary>
    protected virtual void BeforeSubmitFrame() { }
    
    /// <summary>
    /// This method is called automatically every frame this <see cref="GraphicsManager{TGraphicsContext}"/> is active, right after <see cref="WindowShownLock"/> is locked
    /// </summary>
    protected virtual void RunningWindowLocked(TimeSpan delta) { }

    /// <summary>
    /// If <see cref="WindowShownLock"/> is locked externally, this method allows for the vital parts of the <see cref="GraphicsManager{TGraphicsContext}"/>, like processing the Window message queue, to still update
    /// </summary>
    protected virtual void UpdateWhileWaitingOnWindow() { }

    /// <summary>
    /// Executed within <see cref="GraphicsManager{TGraphicsContext}.Run"/> right before entering the main loop, while <see cref="GraphicsManager{TGraphicsContext}.FrameLock"/> is locked
    /// </summary>
    protected virtual void BeforeRun() { }

    /// <summary>
    /// This method is called automatically every frame this <see cref="GraphicsManager{TGraphicsContext}"/> is active, right after <see cref="FrameLock"/> is locked
    /// </summary>
    protected virtual void RunningFramelocked(TimeSpan delta) { }

    /// <summary>
    /// This method is called automatically every frame this <see cref="GraphicsManager{TGraphicsContext}"/> is active, right before the frame ends and before the metrics are calculated
    /// </summary>
    protected virtual void AtFrameEnd() { }

    /// <summary>
    /// Disposes of any resources that were created specifically for <see cref="Run"/>, and or are dependent on the fact that this <see cref="GraphicsManager{TGraphicsContext}"/> is running. Such as a Window object, that may need to be recreated
    /// </summary>
    protected virtual void DisposeRunningResources() { }

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
    /// Locked while the GUI is being drawn
    /// </summary>
    /// <remarks>
    /// Locked while <see cref="GraphicsManager{TGraphicsContext}.FrameLock"/> is locked. After <see cref="GraphicsManager{TGraphicsContext}.DrawLock"/>
    /// </remarks>
    protected readonly SemaphoreSlim GUILock = new(1, 1);

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

    /// <summary>
    /// Creates a <see cref="IDrawQueue{TGraphicsContext}"/> to be used by this <see cref="GraphicsManager{TGraphicsContext}"/>
    /// </summary>
    /// <remarks>
    /// This method is called only once per <see cref="Run"/> call, which should be only once per each <see cref="GraphicsManager{TGraphicsContext}"/>'s lifespan
    /// </remarks>
    protected virtual IDrawQueue<TGraphicsContext> CreateDrawQueue()
        => new DrawQueue<TGraphicsContext>();

    /// <summary>
    /// Fetches a graphics context for rendering. This can either be a reused context or a brand new one.
    /// </summary>
    /// <remarks>
    /// Reference this method only to implement it -- Use it only through <see cref="GetGraphicsContext"/>
    /// </remarks>
    protected abstract TGraphicsContext FetchGraphicsContext();

    /// <summary>
    /// Obtains a new <see cref="GraphicsContext{TSelf}"/>
    /// </summary>
    /// <remarks>
    /// Should be used over <see cref="FetchGraphicsContext"/>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected TGraphicsContext GetGraphicsContext()
    {
        var ctxt = FetchGraphicsContext();
        Debug.Assert(ctxt.Manager == this, "The fetched GraphicsContext does not belong to this GraphicsManager");
        return ctxt;
    }

    /// <summary>
    /// A transformation Matrix that represents the current Window's dimensions
    /// </summary>
    public Matrix4x4 WindowView { get; protected set; }

    /// <summary>
    /// A check to see if this <see cref="GraphicsManager{TGraphicsContext}"/> is running
    /// </summary>
    protected readonly Func<bool> IsRunningCheck;

    /// <summary>
    /// A check to see if this <see cref="GraphicsManager{TGraphicsContext}"/> is not rendering
    /// </summary>
    protected readonly Func<bool> IsNotRenderingCheck;

    /// <summary>
    /// The main method of this <see cref="GraphicsManager{TGraphicsContext}"/>. Maintains its current status as well as performs the actual drawings and other vital tasks. Ideally should run on its own thread launched by <see cref="GraphicsManager.Launch"/>
    /// </summary>
    protected void Run()
    {
        try
        {
            BeforeRun();
            IsRunning = true;
        }
        finally
        {
            FrameLock.Release();
        }

        try
        {
            bool nodopwarn = false;
            var framelock = FrameLock;
            var drawlock = DrawLock;
            var winlock = WindowShownLock;
            var guilock = GUILock;

            var isRunning = IsRunningCheck;
            var isNotRendering = IsNotRenderingCheck;

            var sw = new Stopwatch();
            var drawqueue = CreateDrawQueue();
            var renderTargetBuffer = new List<(uint Level, RenderTargetList<TGraphicsContext> Targets)>();
            TimeSpan delta = default;

            var (ww, wh) = WindowSize;

            Log?.Information("Entering main rendering loop");
            while (IsRunning) // Running Loop
            {
                WaitLockDisposable winlockwaiter;

                while (WaitOn(winlock, condition: isNotRendering, out winlockwaiter, syncWait: 500) is false)
                {
                    while (!IsRendering)
                    {
                        UpdateWhileWaitingOnWindow();
                        continue;
                    }
                }

                using (winlockwaiter)
                {
                    ClearSnapshot();
                    RunningWindowLocked(delta);

                    if (!WaitOn(framelock, condition: isRunning, out var framelockwaiter)) break; // Frame Render
                    using (framelockwaiter)
                    {
                        RunningFramelocked(delta);

                        var context = GetGraphicsContext();

                        context.Update(delta);
                        context.BeginFrame();

                        if (!WaitOn(drawlock, condition: isRunning, out var drawlockwaiter)) break; // Frame Render Stage 1: General Drawing
                        using (drawlockwaiter)
                        {
                            BufferRenderTargets(renderTargetBuffer);
                            if (renderTargetBuffer.Count <= 0)
                                Log?.Debug("This GraphicsManager has no render levels nor render targets");
                            else
                            {
                                for (int i = 0; i < renderTargetBuffer.Count; i++)
                                {
                                    drawqueue.Clear();
                                    var (renderLevel, renderTargets) = renderTargetBuffer[i];
                                    if (renderTargets.Count <= 0)
                                        Log?.Debug("The RenderTargetList for RenderLevel {level} has no render targets", renderLevel);

                                    if (Game.CurrentScene.GetDrawOperationManager<TGraphicsContext>(out var dopm) is false)
                                    {
                                        if (nodopwarn is false)
                                        {
                                            Log?.Warning($"The current Scene does not have a DrawOperationManager for context of type {(typeof(TGraphicsContext))}");
                                            nodopwarn = true;
                                        }
                                    }
                                    else
                                    {
                                        nodopwarn = false;
                                        foreach (var dop in dopm.GetDrawOperations(this, renderLevel))
                                            dopm.AddToDrawQueue(drawqueue, dop);

                                        if (drawqueue.Count <= 0)
                                            Log?.Verbose("No draw operations were registered for RenderLevel {level}", renderLevel);
                                        else
                                        {
                                            lock (renderTargets)
                                            {
                                                foreach (var target in renderTargets)
                                                {
                                                    target.BeginFrame(delta, context);
                                                    while (drawqueue.TryDequeue(out var dop)) 
                                                        target.RenderDrawOperation(delta, context, dop);
                                                    target.EndFrame(context);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        //if (GUIElements.Count > 0) // There's no need to lock neither glock nor ImGUI lock if there are no elements to render. And if it does change between this check and the second one, then tough luck and it'll have to wait until the next frame
                        //{
                        //    if (!await WaitOn(guilock, condition: isRunning, out var guilockwaiter)) break; // Frame Render Stage 2: GUI Drawing
                        //    using (guilockwaiter)
                        //    {
                        //        if (GUIElements.Count > 0) // We check twice, as it may have changed between the first check and the lock being adquired
                        //        {
                        //            managercl.Begin();
                        //            managercl.SetFramebuffer(gd.SwapchainFramebuffer); // Prepare for ImGUI
                        //            using (ImGuiController.Begin()) // Lock ImGUI from other GraphicsManagers
                        //            {
                        //                foreach (var element in GUIElements)
                        //                    element.InternalSubmitUI(delta); // Submit UIs
                        //                using (var snapshot = FetchSnapshot())
                        //                    ImGuiController.Update(1 / 60f, snapshot);
                        //                ImGuiController.Render(gd, managercl); // Render
                        //            }
                        //            managercl.End();
                        //            gd.SubmitCommands(managercl);
                        //        }
                        //    }
                        //}

                        BeforeSubmitFrame();

                        context.EndAndSubmitFrame();

                        BeforeFrameRelease();
                    }

                    BeforeWindowRelease();
                }

                SubmitInput(FetchSnapshot());

                AtFrameEnd();

                // Code that does not require any resources and is not bothered if resources are suddenly released

                CountFrame();
                delta = sw.Elapsed;
                DeltaAverageKeeper.Push(1000 / (sw.ElapsedMilliseconds + 0.0000001f));

                sw.Restart();
            }

            Debug.Assert(IsRunning is false, "The main rendering loop broke even though the GraphicsManager is still supposed to be running");
            Log?.Information("Exiting main rendering loop and disposing");
        }
        finally
        {
            DisposeRunningResources();
        }
    }

    #endregion

    #endregion

    #region Setup Methods

    /// <summary>
    /// <c>true</c> if this GraphicsManager is currently visible, or shown. <c>false</c> otherwise.
    /// </summary>
    /// <remarks>
    /// This <see cref="GraphicsManager{TGraphicsContext}"/> will *NOT* run or update while this GraphicsManager is inactive
    /// </remarks>
    public bool IsWindowAvailable { get; protected set; }

    /// <summary>
    /// <c>true</c> if this GraphicsManager currently has input focus. <c>false</c> otherwise
    /// </summary>
    public bool HasFocus { get; protected set; }

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
