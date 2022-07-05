using SDL2.NET;
using SDL2.NET.Input;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using VDStudios.MagicEngine.Exceptions;
using VDStudios.MagicEngine.Internal;
using Veldrid;
using VeldridPixelFormat = Veldrid.PixelFormat;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a Thread dedicated solely to handling a specific pair of <see cref="SDL2.NET.Window"/> and <see cref="GraphicsDevice"/>, and managing their respective resources in a thread-safe manner
/// </summary>
/// <remarks>
/// *ALL* Graphics Managers are automatically managed by <see cref="Game"/>, registered at the time of construction
/// </remarks>
public class GraphicsManager : GameObject, IDisposable
{
    #region Construction

    /// <summary>
    /// Instances and constructs a new <see cref="GraphicsManager"/> objects
    /// </summary>
    public GraphicsManager()
    {
        initLock.Wait();
        graphics_thread = new(() => Run().Wait());
        Game.graphicsManagersAwaitingSetup.Enqueue(this);

        CurrentSnapshot = new(this);
        snapshotBuffer = new(this);
        
        framelockWaiter = new(FrameLock);
        guilockWaiter = new(GUILock);
        drawlockWaiter = new(DrawLock);
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
    /// <see cref="Window"/>'s size
    /// </summary>
    /// <remarks>
    /// May not represent the actual size of <see cref="Window"/> at any given time, since <see cref="Window"/>s are managed on the VideoThread
    /// </remarks>
    public Size WindowSize { get; private set; }

    /// <summary>
    /// Represents the current Frames-per-second value calculated while this <see cref="GraphicsManager"/> is running
    /// </summary>
    public float FPS => _fps;
    private float _fps;

    #endregion

    #region DrawOperation Registration

    #region Public

    /// <summary>
    /// Register <paramref name="operation"/> into this <see cref="GraphicsManager"/> to be drawn
    /// </summary>
    /// <remarks>
    /// Remember than any single <see cref="DrawOperation"/> can only be assigned to one <see cref="GraphicsManager"/>, and can only be deregistered after disposing. <see cref="DrawOperation"/>s should not be dropped, as <see cref="GraphicsManager"/>s only keep <see cref="WeakReference"/>s to them
    /// </remarks>
    /// <param name="node">The <see cref="Node"/> that owns <paramref name="operation"/></param>
    /// <param name="operation">The <see cref="DrawOperation"/> that will be drawn in this <see cref="GraphicsManager"/></param>
    public async Task RegisterOperation(IDrawableNode node, DrawOperation operation)
    {
        using (await LockManagerDrawingAsync())
        {
            await operation.Register(node, this);
            if (!DrawOperationRegistering(operation, out var reason))
            {
                var excp = new DrawOperationRejectedException(reason, this, operation);
                operation.Dispose();
                throw excp;
            }
            RegisteredOperations.Add(operation.Identifier, new(operation));
        }
    }

    #endregion

    #region Fields

    private readonly Dictionary<Guid, WeakReference<DrawOperation>> RegisteredOperations = new(10);

    #endregion

    #region Reaction Methods

    /// <summary>
    /// This method is called automatically when a new <see cref="DrawOperation"/> is being registered
    /// </summary>
    /// <param name="operation">The <see cref="DrawOperation"/> being registered</param>
    /// <param name="rejectionReason">An optional out parameter that should be set to the reason the change was rejected if the method returns <c>false</c></param>
    /// <returns><c>true</c> if the change is acceptable, <c>false</c> if the registration should be rejected</returns>
    protected virtual bool DrawOperationRegistering(DrawOperation operation, [NotNullWhen(false)] out string? rejectionReason)
    {
        rejectionReason = null;
        return true;
    }

    #endregion

    #endregion

    #region Input Management

    private InputSnapshot snapshotBuffer;
    private InputSnapshot CurrentSnapshot;
    private readonly Queue<InputSnapshot> SnapshotPool = new(3);

    internal void ReturnSnapshot(InputSnapshot snapshot)
    {
        lock (SnapshotPool)
        {
            snapshot.Clear();
            SnapshotPool.Enqueue(snapshot);
        }
    }

    internal InputSnapshot FetchSnapshot()
    {
        lock (SnapshotPool)
        {
            var ret = CurrentSnapshot;
            if (!SnapshotPool.TryDequeue(out var snap))
                snap = new(this);
            CurrentSnapshot = snap;
            ret.CopyTo(snap);
            ret.FetchLastMomentData();
            return ret;
        }
    }

    private void Window_MouseWheelScrolled(Window sender, TimeSpan timestamp, uint mouseId, float verticalScroll, float horizontalScroll)
    {
        lock (SnapshotPool)
        {
            snapshotBuffer.WheelVerticalDelta = verticalScroll;
            snapshotBuffer.WheelHorizontalDelta = horizontalScroll;
        }
    }

    private void Window_MouseMoved(Window sender, TimeSpan timestamp, Point delta, Point newPosition, uint mouseId, MouseButton pressed)
    {
        lock (SnapshotPool)
            snapshotBuffer.mEvs.Add(new(new(delta.X, delta.Y), new Vector2(newPosition.X, newPosition.Y), default));
    }

    private void Window_MouseButtonReleased(Window sender, TimeSpan timestamp, uint mouseId, int clicks, MouseButton state)
    {
        var mp = Mouse.MouseState.Location;
        lock (SnapshotPool)
        {
            snapshotBuffer.butt &= ~state;
            snapshotBuffer.mEvs.Add(new(Vector2.Zero, new Vector2(mp.X, mp.Y), state));
        }
    }

    private void Window_MouseButtonPressed(Window sender, TimeSpan timestamp, uint mouseId, int clicks, MouseButton state)
    {
        var mp = Mouse.MouseState.Location;
        lock (SnapshotPool)
        {
            snapshotBuffer.butt |= state;
            snapshotBuffer.mEvs.Add(new(Vector2.Zero, new Vector2(mp.X, mp.Y), state));
        }
    }

    private void Window_KeyReleased(Window sender, TimeSpan timestamp, Scancode scancode, Keycode key, KeyModifier modifiers, bool isPressed, bool repeat, uint unicode)
    {
        lock (SnapshotPool)
            snapshotBuffer.kEvs.Add(new(scancode, key, modifiers, false, repeat, unicode));
    }

    private void Window_KeyPressed(Window sender, TimeSpan timestamp, Scancode scancode, Keycode key, KeyModifier modifiers, bool isPressed, bool repeat, uint unicode)
    {
        lock (SnapshotPool)
        {
            snapshotBuffer.kEvs.Add(new(scancode, key, modifiers, true, repeat, unicode));
            snapshotBuffer.kcEvs.Add(unicode);
        }
    }

    #endregion

    #region ImGUI

    private ImGuiController ImGuiController;

    /// <summary>
    /// Represents the <see cref="GUIElement"/>s currently held by this <see cref="GraphicsManager"/>
    /// </summary>
    /// <remarks>
    /// A <see cref="GraphicsManager"/>
    /// </remarks>
    public GUIElementList GUIElements { get; } = new();

    /// <summary>
    /// Adds a <see cref="GUIElement"/> <paramref name="element"/> to this <see cref="GraphicsManager"/>
    /// </summary>
    /// <param name="element">The <see cref="GUIElement"/> to add as an element of this <see cref="GraphicsManager"/></param>
    /// <param name="context">The DataContext to give to <paramref name="element"/>, or null if it's to use its previously set DataContext or inherit it from this <see cref="GUIElement"/></param>
    public void AddElement(GUIElement element, object? context = null)
    {
        element.RegisterOnto(this, context);
    }

    #endregion

    #region Waiting

    #region DrawLock
    private readonly idleWaiter drawlockWaiter;

    /// <summary>
    /// Waits until the Manager finishes drawing its <see cref="DrawOperation"/>s and locks it
    /// </summary>
    /// <remarks>
    /// *ALWAYS* wrap the disposable object this method returns in an using statement. The GraphicsManager will stay locked forever if it's not disposed of
    /// </remarks>
    /// <returns>An <see cref="IDisposable"/> object that unlocks the manager when disposed of</returns>
    public IDisposable LockManagerDrawing()
    {
        DrawLock.Wait();
        return drawlockWaiter;
    }

    /// <summary>
    /// Asynchronously waits until the Manager finishes drawing its <see cref="DrawOperation"/>s and locks it
    /// </summary>
    /// <remarks>
    /// *ALWAYS* wrap the disposable object this method returns in an using statement. The GraphicsManager will stay locked forever if it's not disposed of
    /// </remarks>
    /// <returns>An <see cref="IDisposable"/> object that unlocks the manager when disposed of</returns>
    public async Task<IDisposable> LockManagerDrawingAsync()
    {
        await DrawLock.WaitAsync();
        return drawlockWaiter;
    }

    #endregion

    #region GUILock
    private readonly idleWaiter guilockWaiter;

    /// <summary>
    /// Waits until the Manager finishes drawing its <see cref="GUIElement"/>s and locks it
    /// </summary>
    /// <remarks>
    /// *ALWAYS* wrap the disposable object this method returns in an using statement. The GraphicsManager will stay locked forever if it's not disposed of
    /// </remarks>
    /// <returns>An <see cref="IDisposable"/> object that unlocks the manager when disposed of</returns>
    public IDisposable LockManagerGUI()
    {
        GUILock.Wait();
        return guilockWaiter;
    }

    /// <summary>
    /// Asynchronously waits until the Manager finishes drawing its <see cref="GUIElement"/>s and locks it
    /// </summary>
    /// <remarks>
    /// *ALWAYS* wrap the disposable object this method returns in an using statement. The GraphicsManager will stay locked forever if it's not disposed of
    /// </remarks>
    /// <returns>An <see cref="IDisposable"/> object that unlocks the manager when disposed of</returns>
    public async Task<IDisposable> LockManagerGUIAsync()
    {
        await GUILock.WaitAsync();
        return guilockWaiter;
    }

    #endregion

    #region FrameLock
    private readonly idleWaiter framelockWaiter;

    /// <summary>
    /// Waits until the Manager finishes drawing the current frame and locks it
    /// </summary>
    /// <remarks>
    /// Neither general drawing or GUI will be drawn until the frame is released. *ALWAYS* wrap the disposable object this method returns in an using statement. The GraphicsManager will stay locked forever if it's not disposed of
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
    /// Neither general drawing or GUI will be drawn until the frame is released. *ALWAYS* wrap the disposable object this method returns in an using statement. The GraphicsManager will stay locked forever if it's not disposed of
    /// </remarks>
    /// <returns>An <see cref="IDisposable"/> object that unlocks the manager when disposed of</returns>
    public async Task<IDisposable> LockManagerFrameAsync()
    {
        await FrameLock.WaitAsync();
        return framelockWaiter;
    }

    #endregion

    private sealed class idleWaiter : IDisposable
    {
        private SemaphoreSlim @lock;

        public idleWaiter(SemaphoreSlim fl) => @lock = fl;

        public void Dispose()
        {
            @lock.Release();
        }
    }

    #endregion

    #region Window

    /// <summary>
    /// Performs the desired <see cref="WindowAction"/> on <see cref="Window"/>, and returns immediately
    /// </summary>
    /// <remarks>
    /// Failure to perform on <see cref="Window"/> from this method will result in an exception, as <see cref="SDL2.NET.Window"/> is NOT thread-safe and, in fact, thread-protected. All <see cref="SDL2.NET.Window"/>s are created from the main thread
    /// </remarks>
    /// <param name="action">The action to perform on <see cref="Window"/></param>
    public void PerformOnWindow(WindowAction action)
    {
        Game.windowActions.Enqueue(new(Window, action));
    }

    /// <summary>
    /// Performs the desired <see cref="WindowAction"/> on <see cref="Window"/>, and waits for it to complete
    /// </summary>
    /// <remarks>
    /// Failure to perform on <see cref="Window"/> from this method will result in an exception, as <see cref="SDL2.NET.Window"/> is NOT thread-safe and, in fact, thread-protected. All <see cref="SDL2.NET.Window"/>s are created from the main thread
    /// </remarks>
    /// <param name="action">The action to perform on <see cref="Window"/></param>
    public void PerformOnWindowAndWait(WindowAction action)
    {
        SemaphoreSlim sem = new(1, 1);
        sem.Wait();
        Game.windowActions.Enqueue(new(Window, w =>
        {
            try
            {
                action(w);
            }
            finally
            {
                sem.Release();
            }
        }));
        sem.Wait();
        sem.Release();
        sem.Dispose();
    }

    /// <summary>
    /// Performs the desired <see cref="WindowAction"/> on <see cref="Window"/>, and asynchronously waits for it to complete
    /// </summary>
    /// <remarks>
    /// Failure to perform on <see cref="Window"/> from this method will result in an exception, as <see cref="SDL2.NET.Window"/> is NOT thread-safe and, in fact, thread-protected. All <see cref="SDL2.NET.Window"/>s are created from the main thread
    /// </remarks>
    /// <param name="action">The action to perform on <see cref="Window"/></param>
    public async Task PerformOnWindowAndWaitAsync(WindowAction action)
    {
        SemaphoreSlim sem = new(1, 1);
        sem.Wait();
        Game.windowActions.Enqueue(new(Window, w =>
        {
            try
            {
                action(w);
            }
            finally
            {
                sem.Release();
            }
        }));
        await sem.WaitAsync();
        sem.Release();
        sem.Dispose();
    }

    #endregion

    #region Running

    private readonly Thread graphics_thread;

    #region Public Properties

    /// <summary>
    /// Whether this <see cref="GraphicsManager"/> is currently running
    /// </summary>
    public bool IsRunning { get; private set; } = true;

    /// <summary>
    /// The color to draw when the frame is beginning to be drawn
    /// </summary>
    public RgbaFloat BackgroundColor { get; set; } = RgbaFloat.CornflowerBlue;

    #endregion

    #region Public Methods

    ///// <summary>
    ///// Attempts to stop the current <see cref="GraphicsManager"/>'s draw loop
    ///// </summary>
    ///// <returns><c>true</c> if the <see cref="GraphicsManager"/> was succesfully stopped, <c>false</c> otherwise</returns>
    //public bool TryPause()
    //{
    //    lock (sync)
    //        if (!IsRunning)
    //            throw new InvalidOperationException($"Can't stop a GraphicsManager that is not running");

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
    ///// This method is called automatically when this <see cref="GraphicsManager"/> has received a request to stop
    ///// </summary>
    ///// <returns><c>true</c> if the <see cref="GraphicsManager"/> should be allowed to stop, <c>false</c> otherwise</returns>
    //protected virtual bool TryingToStop() => true;

    /// <summary>
    /// This method is called automatically when this <see cref="GraphicsManager"/> is about to start
    /// </summary>
    /// <remarks>
    /// A <see cref="GraphicsManager"/> starts only at the beginning of the first frame it can, and it's done from the default thread
    /// </remarks>
    protected virtual void Starting() { }

    /// <summary>
    /// This method is called automatically every frame this <see cref="GraphicsManager"/> is active
    /// </summary>
    protected virtual void Running() { }

    /// <summary>
    /// Creates the <see cref="CommandList"/> to be set as this <see cref="GraphicsManager"/>'s designated <see cref="CommandList"/>, for use with <see cref="PrepareForDraw(CommandList, Framebuffer)"/>
    /// </summary>
    /// <param name="device">The device of the attached <see cref="GraphicsManager"/></param>
    /// <param name="factory"><paramref name="device"/>'s <see cref="ResourceFactory"/></param>
    protected virtual CommandList CreateCommandList(GraphicsDevice device, ResourceFactory factory)
        => factory.CreateCommandList();

    /// <summary>
    /// Executes commands before any registered <see cref="DrawOperation"/> is processed
    /// </summary>
    /// <remarks>
    /// <paramref name="commandlist"/> is begun, ended and submitted automatically. <see cref="CommandList.SetFramebuffer(Framebuffer)"/> is not called, however.
    /// </remarks>
    /// <param name="commandlist">The <see cref="CommandList"/> for this call. It's ended, begun and submitted automatically, so you don't need to worry about it. <see cref="CommandList.SetFramebuffer(Framebuffer)"/> is not called, however.</param>
    /// <param name="mainBuffer">The <see cref="GraphicsDevice"/> owned by this <see cref="GraphicsManager"/>'s main <see cref="Framebuffer"/>, to use with <see cref="CommandList.SetFramebuffer(Framebuffer)"/></param>
    protected virtual void PrepareForDraw(CommandList commandlist, Framebuffer mainBuffer)
    {
        commandlist.SetFramebuffer(mainBuffer);
        commandlist.ClearColorTarget(0, BackgroundColor);
    }

    #endregion

    #region Internal

    private readonly SemaphoreSlim WindowShownLock = new(1, 1);
    private readonly SemaphoreSlim FrameLock = new(1, 1);
    private readonly SemaphoreSlim DrawLock = new(1, 1);
    private readonly SemaphoreSlim GUILock = new(1, 1);

    internal Vector4 LastReportedWinSize = default;
    internal DeviceBuffer? ScreenSizeBuffer;
    private bool SizeChanged = false;

    private void Window_SizeChanged(Window window, TimeSpan timestamp, Size newSize)
    {
        FrameLock.Wait();
        try
        {
            var (ww, wh) = newSize;
            Device.MainSwapchain.Resize((uint)ww, (uint)wh);
            ImGuiController.WindowResized(ww, wh);
            WindowSize = newSize;
            Vector4 size;
            LastReportedWinSize = size = new(ww, wh, 0, 0);
            SizeChanged = true;
            Device!.UpdateBuffer(ScreenSizeBuffer, 0, size);
        }
        finally
        {
            FrameLock.Release();
        }
    }

    /// <summary>
    /// This is run from the update thread
    /// </summary>
    internal void InternalStart()
    {
        Starting();
        SetupWindow();
        initLock.Release();
        graphics_thread.Start();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private async ValueTask<bool> WaitOn(SemaphoreSlim semaphore, int syncWait = 200, int asyncWait = 500)
    {
        if (!semaphore.Wait(syncWait))
        {
            if (!IsRunning)
                return false;
            while (!await semaphore.WaitAsync(asyncWait))
                if (!IsRunning)
                    return false;
        }
        return true;
    }

    private async Task Run()
    {
        var framelock = FrameLock;
        var drawlock = DrawLock;
        var glock = GUILock;
        var winlock = WindowShownLock;

        var sw = new Stopwatch();
        var drawqueue = new DrawQueue<DrawOperation>();
        var removalQueue = new Queue<Guid>(10);
        TimeSpan delta = default;

        var gd = Device!;

        var managercl = CreateCommandList(gd, gd.ResourceFactory);

        long frameCount = 0;

        while (IsRunning) // Running Loop
        {
            if (!await WaitOn(winlock, 100, 5000)) break; // Window Lock is separate from FrameLock simply because it's not publicly available
            try
            {
                if (!await WaitOn(framelock)) break; // Frame Render
                try
                {
                    Vector4 winsize = LastReportedWinSize;

                    Running();

                    if (!await WaitOn(drawlock)) break; // Frame Render Stage 1: General Drawing
                    try
                    {
                        var ops = RegisteredOperations;

                        if (SizeChanged) // Handle Window Size Changes, this could potentially be refactored. Jump to the 'else' block
                        {
                            SizeChanged = false;
                            foreach (var kv in ops)
                                if (kv.Value.TryGetTarget(out var op) && !op.disposedValue)
                                {
                                    await op.InternalCreateWindowSizedResources(ScreenSizeBuffer);
                                    op.Owner.AddToDrawQueue(drawqueue, op);
                                }
                                else
                                    removalQueue.Enqueue(kv.Key);
                        }
                        else
                        {
                            foreach (var kv in ops) // Iterate through all registered operations
                                if (kv.Value.TryGetTarget(out var op) && !op.disposedValue)  // Filter out those that have been disposed or collected
                                    op.Owner.AddToDrawQueue(drawqueue, op); // And query them
                                else
                                    removalQueue.Enqueue(kv.Key); // Enqueue the object if filtered out (Enumerators forbid changes mid-enumeration)
                        }

                        while (removalQueue.Count > 0)
                            ops.Remove(removalQueue.Dequeue()); // Remove collected or disposed objects

                        managercl.Begin();
                        PrepareForDraw(managercl, gd.SwapchainFramebuffer); // Set the base of the frame: clear the background, etc.
                        managercl.End();
                        gd.SubmitCommands(managercl);

                        using (drawqueue._lock.Lock())
                        {
                            var buffers = ArrayPool<ValueTask>.Shared;
                            var calls = buffers.Rent(ops.Count);
                            try
                            {
                                int i = 0;
                                for (; drawqueue.Count > 0; i++)
                                    calls[i] = drawqueue.Dequeue().InternalDraw(delta, new Vector2(winsize.X / 2, winsize.Y / 2)); // Run operations in the DrawQueue
                                while (i > 0)
                                    await calls[--i];
                            }
                            finally
                            {
                                buffers.Return(calls, true);
                            }
                        }
                    }
                    finally
                    {
                        drawlock.Release(); // End the general drawing stage
                    }
                
                    if (GUIElements.Count > 0) // There's no need to lock neither glock nor ImGUI lock if there are no elements to render. And if it does change between this check and the second one, then tough luck and it'll have to wait until the next frame
                    {
                        if (!await WaitOn(glock)) break; // Frame Render Stage 2: GUI Drawing
                        try
                        {
                            if (GUIElements.Count > 0) // We check twice, as it may have changed between the first check and the lock being adquired
                            {
                                managercl.Begin();
                                managercl.SetFramebuffer(gd.SwapchainFramebuffer); // Prepare for ImGUI
                                using (ImGuiController.Begin()) // Lock ImGUI from other GraphicsManagers
                                {
                                    foreach (var element in GUIElements)
                                        element.InternalSubmitUI(delta); // Submit UIs
                                    using (var snapshot = FetchSnapshot())
                                        ImGuiController.Update(1 / 60f, snapshot);
                                    ImGuiController.Render(gd, managercl); // Render
                                }
                                managercl.End();
                                gd.SubmitCommands(managercl);
                            }
                        }
                        finally
                        {
                            glock.Release(); // End the GUI drawing stage
                        }
                    }

                    gd.WaitForIdle(); // Wait for operations to finish
                    gd.SwapBuffers(); // Present
                }
                finally
                {
                    framelock.Release(); // End Frame Render
                }
            }
            finally
            {
                winlock.Release();
            }

            // Code that does not require any resources and is not bothered if resources are suddenly released

            lock (SnapshotPool)
            {
                snapshotBuffer.CopyTo(CurrentSnapshot);
                snapshotBuffer.Clear();
            }
            
            _fps = 1000 / (sw.ElapsedMilliseconds + 0.0000001f);
            frameCount++;
            delta = sw.Elapsed;
            sw.Restart();
        }

        Dispose();
    }

    private void Window_Closed(Window sender, TimeSpan timestamp)
    {
        IsRunning = false;
    }

    #endregion

    #region Events

    ///// <summary>
    ///// Fired when <see cref="GraphicsManager"/> start
    ///// </summary>
    ///// <remarks>
    ///// Specifically, when <see cref="IsRunning"/> is set to <c>true</c>
    ///// </remarks>
    //public GraphicsManagerRunStateChanged? Started;

    ///// <summary>
    ///// Fired when <see cref="GraphicsManager"/> stops
    ///// </summary>
    ///// <remarks>
    ///// Specifically, when <see cref="IsRunning"/> is set to <c>false</c>
    ///// </remarks>
    //public GraphicsManagerRunStateChanged? Stopped;

    ///// <summary>
    ///// Fired when <see cref="IsRunning"/> changes
    ///// </summary>
    ///// <remarks>
    ///// Fired after <see cref="Started"/> or <see cref="Stopped"/>
    ///// </remarks>
    //public GraphicsManagerRunStateChanged? RunStateChanged;

    #endregion

    #endregion

    #region Private Fields

    private readonly object sync = new();

    #endregion

    #region Setup Methods

    [MethodImpl]
    internal void SetupWindow()
    {
        CreateWindow(out var win, out var gd);
        Window = win;
        Device = gd;

        var (ww, wh) = win.Size;
        ImGuiController = new(gd, gd.SwapchainFramebuffer.OutputDescription, ww, wh);
        ScreenSizeBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));

        Window_SizeChanged(win, Game.TotalTime, win.Size);

        win.SizeChanged += Window_SizeChanged;
        win.Closed += Window_Closed;
        win.KeyPressed += Window_KeyPressed;
        win.KeyReleased += Window_KeyReleased;
        win.MouseButtonPressed += Window_MouseButtonPressed;
        win.MouseButtonReleased += Window_MouseButtonReleased;
        win.MouseMoved += Window_MouseMoved;
        win.MouseWheelScrolled += Window_MouseWheelScrolled;
        win.Hidden += Window_Hidden;
        win.Shown += Window_Shown;
    }

    /// <summary>
    /// <c>true</c> if <see cref="Window"/> is currently visible, or shown. <c>false</c> otherwise.
    /// </summary>
    /// <remarks>
    /// This <see cref="GraphicsManager"/> will *NOT* run or update while <see cref="Window"/> is inactive
    /// </remarks>
    public bool IsWindowAvailable { get; private set; }

    private void Window_Shown(Window sender, TimeSpan timestamp)
    {
        IsWindowAvailable = true;
        if (WindowShownLock.CurrentCount == 0)
            WindowShownLock.Release();
    }

    private void Window_Hidden(Window sender, TimeSpan timestamp)
    {
        WindowShownLock.Wait();
        IsWindowAvailable = false;
    }

    /// <summary>
    /// This method is automatically called during this object's construction, creates a <see cref="SDL2.NET.Window"/> and <see cref="GraphicsDevice"/> to be used as <see cref="Window"/> and <see cref="Device"/> respectively
    /// </summary>
    /// <remarks>
    /// Do not call any <see cref="GraphicsManager"/> methods from here, it may cause a deadlock! The <see cref="Window"/> and <see cref="GraphicsDevice"/> here produced are expected to be unique and owned exclusively by this <see cref="GraphicsManager"/>. Issues may arise if this is not so. It's better if you don't call base <see cref="CreateWindow"/>. This is called, specifically, by <see cref="GraphicsManager"/>'s constructor. If this is a derived type, know that this method will be called by the first constructor in the hierarchy, and NOT after your type's constructor
    /// </remarks>
    protected virtual void CreateWindow(out Window mainWindow, out GraphicsDevice graphicsDevice)
    {
        mainWindow = new Window(Game.GameTitle, 800, 600, WindowConfig.Default);

        graphicsDevice = Veldrid.Startup.CreateGraphicsDevice(
            mainWindow,
#if !DEBUG
            new GraphicsDeviceOptions(true, null, true, ResourceBindingModel.Improved, false, false)
#else
            new GraphicsDeviceOptions(false, null, true, ResourceBindingModel.Improved, false, false)
#endif
        );
    }

    #endregion

    #region Graphic Resources

    #region Public Properties

    /// <summary>
    /// The current Main Window of the Game
    /// </summary>
    public Window Window { get; private set; }

    /// <summary>
    /// The current Main Renderer of the Game
    /// </summary>
    public GraphicsDevice Device { get; private set; }

    #endregion

    #endregion

    #region Disposal

    private bool disposedValue;

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> if this object is disposed at the time of this method being called
    /// </summary>
    /// <exception cref="ObjectDisposedException"></exception>
    protected internal void ThrowIfDisposed()
    {
        if (disposedValue)
            throw new ObjectDisposedException(GetType().Name);
    }

    /// <summary>
    /// This method is automatically called when this object is being finalized or being disposed. Do NOT call this manually!
    /// </summary>
    /// <param name="disposing"><c>true</c> if the method was called from <see cref="Dispose()"/>, <c>false</c> if it was called from the type's finalizer</param>
    /// <remarks>
    /// This will be called before the internal (non-overridable) disposal code of <see cref="GraphicsManager"/>, and it will be called inside a try/finally block, so exceptions thrown in it will be rethrown /after/ this type's code is executed. It's your responsibility how you chain these dispose calls together.
    /// </remarks>
    protected virtual void Dispose(bool disposing) { }

    private void InternalDispose(bool disposing)
    {
        IsRunning = false;
        FrameLock.Wait();
        try
        {
            Dispose(disposing);
        }
        finally
        {
            Device.Dispose();
            Window.Dispose();
            FrameLock.Release();
        }
    }

    /// <inheritdoc/>
    ~GraphicsManager()
    {
        InternalDispose(disposing: false);
    }

    /// <summary>
    /// <see cref="Dispose()"/> merely schedules the disposal, this actually goes through with it
    /// </summary>
    internal void ActuallyDispose()
    {
        lock (sync)
            if (disposedValue)
            {
                disposedValue = true;
                return;
            }

        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        InternalDispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of this <see cref="GraphicsManager"/> and its held resources
    /// </summary>
    public void Dispose()
    {
        lock (sync)
            if (disposedValue)
            {
                disposedValue = true;
                return;
            }

        Game.graphicsManagersAwaitingDestruction.Enqueue(this);
    }

    #endregion
}
