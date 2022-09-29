using SDL2.NET;
using SDL2.NET.Input;
using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using VDStudios.MagicEngine.DrawLibrary;
using VDStudios.MagicEngine.Exceptions;
using VDStudios.MagicEngine.Internal;
using Veldrid;

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
    /// Represents the <see cref="DeferredExecutionSchedule"/> tied to this <see cref="GraphicsManager"/>, and can be used to defer calls that should run under this <see cref="GraphicsManager"/>'s loop. 
    /// </summary>
    /// <remarks>
    /// This schedule is updated every frame, as such, it's subject to the framerate of this <see cref="GraphicsManager"/>, which, depending on configuration, can vary between the <see cref="GraphicsDevice"/> vertical refresh rate, or as fast as it can run. ---- Not to be confused with <see cref="Game.DeferredCallSchedule"/> (or <see cref="GameObject.GameDeferredCallSchedule"/>, which is the same)
    /// </remarks>
    public DeferredExecutionSchedule DeferredCallSchedule { get; }
    private readonly Action DeferredCallScheduleUpdater;

    /// <summary>
    /// Instances and constructs a new <see cref="GraphicsManager"/> object
    /// </summary>
    /// <param name="commandListGroups">The CommandList group definitions for this <see cref="GraphicsManager"/></param>
    public GraphicsManager(ImmutableArray<CommandListGroupDefinition> commandListGroups) : base("Graphics & Input", "Rendering")
    {
        if (commandListGroups.Length <= 0)
            throw new ArgumentException("The array of CommandListGroups cannot be empty", nameof(commandListGroups));
        for (int i = 0; i < commandListGroups.Length; i++)
            if (commandListGroups[i].Parallelism <= 0 || commandListGroups[i].ExpectedOperations <= 0)
                throw new ArgumentException($"The CommandListGroupDefinition at index {i} is improperly defined. The degree of parallelism and the amount of expected operations must both be larger than 0");

        initLock.Wait();
        Game.graphicsManagersAwaitingSetup.Enqueue(this);

        CurrentSnapshot = new(this);
        snapshotBuffer = new(this);
        
        framelockWaiter = new(FrameLock);
        guilockWaiter = new(GUILock);
        drawlockWaiter = new(DrawLock);

        CommandListGroups = commandListGroups;

        DefaultResourceCache = new(this);

        DeferredCallSchedule = DeferredExecutionSchedule.New(out DeferredCallScheduleUpdater);

        IsRunningCheck = () => IsRunning;
        IsNotRenderingCheck = () => !IsRendering;
    }

    /// <summary>
    /// Instances and constructs a new <see cref="GraphicsManager"/> object
    /// </summary>
    /// <param name="commandListGroups">The CommandList group definitions for this <see cref="GraphicsManager"/>. This array will be copied and turned into an <see cref="ImmutableArray{T}"/></param>
    public GraphicsManager(params CommandListGroupDefinition[] commandListGroups) : this(ImmutableArray.Create(commandListGroups)) { }

    /// <summary>
    /// Instances and constructs a new <see cref="GraphicsManager"/> object
    /// </summary>
    /// <param name="parallelism">The degree of parallelism to assign to the single <see cref="CommandListGroupDefinition"/> this constructor will create</param>
    public GraphicsManager(int parallelism) : this(ImmutableArray.Create(new CommandListGroupDefinition(parallelism))) { }

    private readonly SemaphoreSlim initLock = new(1, 1);

    internal void WaitForInit()
    {
        initLock.Wait();
        initLock.Release();
    }

    #endregion

    #region Parallel Rendering

    /// <summary>
    /// The maximum degree of parallel rendering to use for this <see cref="GraphicsManager"/>
    /// </summary>
    public ImmutableArray<CommandListGroupDefinition> CommandListGroups { get; }

    private CommandListDispatch[][] CLDispatchs;

    #endregion

    #region Public Properties

    /// <summary>
    /// Whether the <see cref="GraphicsManager"/> should stop rendering when it loses input focus
    /// </summary>
    public bool PauseOnInputLoss { get; set; }

    /// <summary>
    /// <see cref="Window"/>'s size
    /// </summary>
    /// <remarks>
    /// May not represent the actual size of <see cref="Window"/> at any given time, since <see cref="Window"/>s are managed on the VideoThread
    /// </remarks>
    public Size WindowSize { get; private set; }

    /// <summary>
    /// A transformation matrix to transform points in window space so that objects drawn in relative window coordinates (-1f to 1f) maintain their original aspect ratio
    /// </summary>
    public WindowTransformation WindowTransform { get; private set; }

    /// <summary>
    /// Represents the <see cref="ResourceLayout"/> that describes the usage of a <see cref="DrawTransformation"/>
    /// </summary>
    public ResourceLayout DrawTransformationLayout { get; private set; }

    /// <summary>
    /// Represents the buffer that will hold the information for window transformation
    /// </summary>
    public DeviceBuffer WindowTransformBuffer { get; private set; }

    private BindableResource[] ManagerResourceBindings;

    private static ResourceLayout? ManagerResourceLayout;

    internal ResourceLayout DrawOpTransLayout { get; private set; }

    /// <summary>
    /// The the default resource cache for this <see cref="GraphicsManager"/>
    /// </summary>
    public DefaultResourceCache DefaultResourceCache { get; }

    /// <summary>
    /// Gets or instantiates the layout of the resources relevant to this <see cref="GraphicsManager"/>
    /// </summary>
    /// <remarks>
    /// Corresponds to the <see cref="ManagerResourceSet"/> of each instance of <see cref="GraphicsManager"/>
    /// </remarks>
    public static ResourceLayout GetManagerResourceLayout(ResourceFactory factory)
    {
        if (ManagerResourceLayout is null)
            lock (typeof(GraphicsManager)) 
                ManagerResourceLayout ??= factory.CreateResourceLayout(new ResourceLayoutDescription(new ResourceLayoutElementDescription("WindowTransform", ResourceKind.UniformBuffer, ShaderStages.Vertex)));
        return ManagerResourceLayout;
    }

    /// <summary>
    /// Represents the set of the resources relevant to this <see cref="GraphicsManager"/>
    /// </summary>
    /// <remarks>
    /// Corresponds to <see cref="ManagerResourceSet"/>
    /// </remarks>
    public ResourceSet ManagerResourceSet { get; private set; }
    
    /// <summary>
    /// Adds the necessary resources to provide the Window aspect transformation buffer as a bound resource to a shader
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="device"></param>
    /// <param name="factory"></param>
    /// <param name="builder"></param>
    public static void AddWindowAspectTransform(GraphicsManager manager, GraphicsDevice device, ResourceFactory factory, ResourceSetBuilder builder)
        => builder.InsertFirst(manager.ManagerResourceSet, GetManagerResourceLayout(factory), out _);

    /// <summary>
    /// Represents the current Frames-per-second value calculated while this <see cref="GraphicsManager"/> is running
    /// </summary>
    public float FramesPerSecond => fak.Average;
    private readonly FloatAverageKeeper fak = new(10);

    #endregion

    #region DrawOperation Registration

    private readonly List<DrawOperation> DOPRegistrationBuffer = new();
    private readonly List<SharedDrawResource> SDRRegistrationBuffer = new();

    #region Public

    /// <summary>
    /// Register <paramref name="operation"/> into this <see cref="GraphicsManager"/> to be drawn
    /// </summary>
    /// <remarks>
    /// Remember than any single <see cref="DrawOperation"/> can only be assigned to one <see cref="GraphicsManager"/>, and can only be deregistered after disposing. <see cref="DrawOperation"/>s should not be dropped, as <see cref="GraphicsManager"/>s only keep <see cref="WeakReference"/>s to them
    /// </remarks>
    /// <param name="operation">The <see cref="DrawOperation"/> that will be drawn in this <see cref="GraphicsManager"/></param>
    internal void QueueOperationRegistration(DrawOperation operation)
    {
        if (operation._clga >= CommandListGroups.Length)
            throw new InvalidOperationException($"This GraphicsManager only has {CommandListGroups.Length} CommandList groups [0-{CommandListGroups.Length - 1}], an operation with a CommandListGroupAffinity of {operation._clga} cannot be registered.");

        lock (DOPRegistrationBuffer)
            DOPRegistrationBuffer.Add(operation);
    }

    /// <summary>
    /// Queues <paramref name="resource"/> for registration in this <see cref="GraphicsManager"/>
    /// </summary>
    /// <param name="resource">The <see cref="SharedDrawResource"/> that will be registered onto this <see cref="GraphicsManager"/></param>
    public void RegisterSharedDrawResource(SharedDrawResource resource)
    {
        resource.ThrowIfAlreadyRegistered();
        lock (SDRRegistrationBuffer)
            SDRRegistrationBuffer.Add(resource);
    }

    /// <summary>
    /// Queues all of the <see cref="SharedDrawResource"/>s in <paramref name="resources"/> for registration in this <see cref="GraphicsManager"/>
    /// </summary>
    /// <param name="resources">The <see cref="SharedDrawResource"/>s that will be registered onto this <see cref="GraphicsManager"/></param>
    public void RegisterSharedDrawResource(IEnumerable<SharedDrawResource> resources)
    {
        lock (SDRRegistrationBuffer)
        {
            if (resources is ICollection<SharedDrawResource> coll)
                SDRRegistrationBuffer.EnsureCapacity(coll.Count + SDRRegistrationBuffer.Count);

            if (resources is SharedDrawResource[] arr)
                for (int i = 0; i < arr.Length; i++)
                {
                    var obj = arr[i];
                    obj.ThrowIfAlreadyRegistered();
                    SDRRegistrationBuffer.Add(obj);
                }
            else
                foreach (var obj in resources)
                {
                    obj.ThrowIfAlreadyRegistered();
                    SDRRegistrationBuffer.Add(obj);
                }
        }
    }
    
    #endregion

    #region Fields

    private readonly Dictionary<Guid, WeakReference<DrawOperation>> RegisteredOperations = new(10);
    private readonly Dictionary<Guid, WeakReference<SharedDrawResource>> RegisteredResources = new(10);

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

    private readonly InputSnapshot snapshotBuffer;
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
        private readonly SemaphoreSlim @lock;

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

    private Task graphics_thread;

    internal async ValueTask AwaitIfFaulted()
    {
        if (graphics_thread.IsFaulted)
            await graphics_thread;
    }

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
    /// Creates a <see cref="CommandList"/> for internal use. This method may be used to create multiple <see cref="CommandList"/>s at discretion
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
    private readonly SemaphoreSlim ResLock = new(1, 1);
    private readonly SemaphoreSlim DopLock = new(1, 1);

    internal Vector4 LastReportedWinSize = default;

    private void Window_SizeChanged(Window window, TimeSpan timestamp, Size newSize)
    {
        FrameLock.Wait();
        try
        {
            var (ww, wh) = newSize;
            InternalLog?.Information("Window size changed to {{w:{width}, h:{height}}}", newSize.Width, newSize.Height);
            InternalLog?.Verbose("Resizing MainSwapchain");
            Device.MainSwapchain.Resize((uint)ww, (uint)wh);

            InternalLog?.Verbose("Resizing ImGuiController");
            ImGuiController.WindowResized(ww, wh);

            WindowSize = newSize;
            WindowTransformation wintrans;
            WindowTransform = wintrans = new WindowTransformation()
            {
                WindowScale = Matrix4x4.CreateScale(wh / (float)ww, 1, 1)
            };
            Device!.UpdateBuffer(WindowTransformBuffer, 0, ref wintrans);
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
        InternalLog?.Information("Starting GraphicsManager");
        Starting();

        InternalLog?.Information("Setting up Window");
        SetupWindow();
        initLock.Release();

        InternalLog?.Information("Running GraphicsManager");
        graphics_thread = Run();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async ValueTask<bool> WaitOn(SemaphoreSlim semaphore, Func<bool> condition, int syncWait = 15, int asyncWait = 50, int syncRepeats = 10)
    {
        while (syncRepeats-- > 0)
            if (!semaphore.Wait(syncWait))
            {
                if (!condition())
                    return false;
            }
            else //succesfully adquired the lock
                return true;
        // ran out of repeats without adquiring the lock

        while (!await semaphore.WaitAsync(asyncWait))
            if (!condition())
                return false;
        return true;
    }

    /// <summary>
    /// The default <see cref="DataDependencySource{T}"/> containing <see cref="DrawTransformation"/> for all <see cref="DrawOperation"/>s that don't already have one
    /// </summary>
    protected internal DrawParameters DrawParameters { get; private set; }

    private void UpdateWindowTransformationBuffer(CommandList cl)
    {
        Span<WindowTransformation> trans = stackalloc WindowTransformation[1] { WindowTransform };
        cl.UpdateBuffer(WindowTransformBuffer, 0, trans);
    }

    private async ValueTask ProcessSharedResourceRegistrationBuffer()
    {
        int count = SDRRegistrationBuffer.Count;
        SharedDrawResource[] regbuf;
        lock (SDRRegistrationBuffer)
        {
            if (SDRRegistrationBuffer.Count <= 0) return;
            regbuf = ArrayPool<SharedDrawResource>.Shared.Rent(count);
            SDRRegistrationBuffer.CopyTo(regbuf);
            SDRRegistrationBuffer.Clear();
        }

        if (!await WaitOn(ResLock, IsRunningCheck)) return;
        try
        {
            for (int i = 0; i < count; i++)
            {
                var resource = regbuf[i];
                InternalLog?.Verbose("Registering SharedDrawResource {objName}-{type}", resource.Name ?? "", resource.GetTypeName());
                await resource.Register(this);
                RegisteredResources.Add(resource.Identifier, new(resource));
            }
        }
        finally
        {
            ResLock.Release();
            ArrayPool<SharedDrawResource>.Shared.Return(regbuf);
        }
    }

    private async ValueTask ProcessDrawOpRegistrationBuffer()
    {
        int count = DOPRegistrationBuffer.Count;
        DrawOperation[] regbuf;
        lock (DOPRegistrationBuffer)
        {
            if (DOPRegistrationBuffer.Count <= 0) return;
            regbuf = ArrayPool<DrawOperation>.Shared.Rent(count);
            DOPRegistrationBuffer.CopyTo(regbuf);
            DOPRegistrationBuffer.Clear();
        }

        if (!await WaitOn(DopLock, IsRunningCheck)) return;
        try
        {
            for (int i = 0; i < count; i++)
            {
                var operation = regbuf[i];
                InternalLog?.Verbose("Registering DrawOperation {objName}-{type}", operation.Name ?? "", operation.GetTypeName());
                await operation.Register(this);
                if (!DrawOperationRegistering(operation, out var reason))
                {
                    var excp = new DrawOperationRejectedException(reason, this, operation);
                    operation.Dispose();
                    throw excp;
                }
                RegisteredOperations.Add(operation.Identifier, new(operation));
            }
        }
        finally
        {
            DopLock.Release();
            ArrayPool<DrawOperation>.Shared.Return(regbuf);
        }
    }

    private void InitCLD()
    {
        var gd = Device!;
        var factory = gd.ResourceFactory;
        var count = CommandListGroups.Length;
        CLDispatchs = new CommandListDispatch[count][];
        for (int i1 = 0; i1 < count; i1++)
        {
            var(paral, exops) = CommandListGroups[i1];

#if FORCE_GM_NOPARALLEL
            paral = 1;
#endif

            ref var cld = ref CLDispatchs[i1];
            cld = new CommandListDispatch[paral];
            int div = int.Max(exops / paral, exops);
            for (int i2 = 0; i2 < cld.Length; i2++)
                cld[i2] = new(div, CreateCommandList(gd, factory));
            InternalLog?.Debug("Created a CommandList group with a degree of parallelism of {paralellism}, and an amount of expected operations of {exops}", paral, exops);
        }
        if (count is 1)
            InternalLog?.Information("Started with {count} CommandList group", count);
        else
            InternalLog?.Information("Started with {count} CommandList groups", count);
    }

    private readonly Func<bool> IsRunningCheck;
    private readonly Func<bool> IsNotRenderingCheck;

    private async Task Run()
    {
        await Task.Yield();

        var framelock = FrameLock;
        var drawlock = DrawLock;
        var glock = GUILock;
        var winlock = WindowShownLock;
        var reslock = ResLock;
        var doplock = DopLock;

        var isRunning = IsRunningCheck;
        var isNotRendering = IsNotRenderingCheck;

        var sw = new Stopwatch();
        var drawqueue = new DrawQueue(CommandListGroups);
        var removalQueue = new Queue<Guid>(10);
        CommandListDispatch[] activeDispatchs = Array.Empty<CommandListDispatch>();
        SharedDrawResource[] resBuffer = Array.Empty<SharedDrawResource>();
        int resBufferFill = 0;
        TimeSpan delta = default;

        DrawParameters = new(this);

        var gd = Device!;
        
        var managercl = CreateCommandList(gd, gd.ResourceFactory);

        ulong frameCount = 0;

        InitCLD();
        InternalLog?.Debug("Querying WindowFlags");
        await PerformOnWindowAndWaitAsync(w =>
        {
            var flags = w.Flags;
            IsWindowAvailable = flags.HasFlag(WindowFlags.Shown);
            HasFocus = flags.HasFlag(WindowFlags.InputFocus);
        });

        var (ww, wh) = WindowSize;

        InternalLog?.Information("Entering main rendering loop");
        while (IsRunning) // Running Loop
        {
            for (; ; )
            {
                while (!IsRendering)
                    await Task.Delay(1000);
                if (await WaitOn(winlock, condition: isNotRendering, syncWait: 500, asyncWait: 1000)) break;
            }

            try
            {
                if (!await WaitOn(framelock, condition: isRunning)) break; // Frame Render
                try
                {
                    Vector4 winsize = LastReportedWinSize;

                    Running();

                    if (!await WaitOn(drawlock, condition: isRunning)) break; // Frame Render Stage 1: General Drawing
                    try
                    {
                        #region Draw Operations

                        if (!await WaitOn(doplock, condition: isRunning)) break;
                        try
                        {
                            var ops = RegisteredOperations;

                            foreach (var kv in ops) // Iterate through all registered operations
                                if (kv.Value.TryGetTarget(out var op) && !op.disposedValue)  // Filter out those that have been disposed or collected
                                    op.Owner.AddToDrawQueue(drawqueue, op); // And query them
                                else
                                    removalQueue.Enqueue(kv.Key); // Enqueue the object if filtered out (Enumerators forbid changes mid-enumeration)

                            while (removalQueue.Count > 0)
                                ops.Remove(removalQueue.Dequeue()); // Remove collected or disposed objects
                        }
                        finally
                        {
                            doplock.Release();
                        }

                        #endregion

                        #region Draw Resources

                        if (!await WaitOn(reslock, isRunning)) break;
                        try
                        {
                            var res = RegisteredResources;

                            if (resBuffer.Length < res.Count)
                                resBuffer = new SharedDrawResource[int.Max(resBuffer.Length * 2, res.Count + 1)];

                            foreach (var kv in res) // Iterate through all registered operations
                                if (kv.Value.TryGetTarget(out var sdr) && !sdr.disposedValue)  // Filter out those that have been disposed or collected
                                    if (sdr.PendingGpuUpdate)
                                        resBuffer[resBufferFill++] = sdr;
                                    else
                                        removalQueue.Enqueue(kv.Key); // Enqueue the object if filtered out (Enumerators forbid changes mid-enumeration)

                            while (removalQueue.Count > 0)
                                res.Remove(removalQueue.Dequeue()); // Remove collected or disposed objects
                        }
                        finally
                        {
                            reslock.Release();
                        }

                        #endregion

                        using (drawqueue._lock.Lock())
                        {
                            int totalcount = drawqueue.GetTotalCount();
                            int dispatchs = 0;
                            if (activeDispatchs.Length < totalcount)
                                activeDispatchs = new CommandListDispatch[int.Max(totalcount, activeDispatchs.Length * 2)];

                            for (int cld_g_i = 0; cld_g_i < drawqueue.QueueCount; cld_g_i++) 
                            {
                                var queue = drawqueue.GetQueue(cld_g_i);
                                if (queue.Count <= 0) continue;
                                int dqc = queue.Count;
                                var cld_g = CLDispatchs[cld_g_i];
                                var perCL = dqc / cld_g.Length;
                                if (perCL is 0 || queue.Count == cld_g.Length)
                                {
                                    int i = 0;
                                    for (; i < dqc; i++)
                                    {
                                        var cld = cld_g[i];
                                        cld.Add(queue.Dequeue());
                                        cld.Start(delta);
                                        activeDispatchs[dispatchs++] = cld;
                                    }
                                }
                                else
                                {
                                    int i = 0;
                                    for (; i < cld_g.Length - 1; i++)
                                    {
                                        var cld = cld_g[i];
                                        for (int x = 0; x < perCL; x++)
                                            cld.Add(queue.Dequeue());
                                        cld.Start(delta);
                                        activeDispatchs[dispatchs++] = cld;
                                    }
                                    var lcld = cld_g[i];
                                    while (queue.Count > 0)
                                        lcld.Add(queue.Dequeue());
                                    lcld.Start(delta);
                                    activeDispatchs[dispatchs++] = lcld;
                                }
#if FORCE_GM_NOPARALLEL
                                managercl.Begin();
                                PrepareForDraw(managercl, gd.SwapchainFramebuffer); // Set the base of the frame: clear the background, etc.
                                var tbuff = ArrayPool<ValueTask>.Shared.Rent(resBufferFill);
                                try
                                {
                                    for (int i = 0; i < resBufferFill; i++)
                                        tbuff[i] = resBuffer[i].InternalUpdate(managercl).Preserve();
                                    for (int i = 0; i < resBufferFill; i++) await tbuff[i];
                                }
                                finally
                                {
                                    ArrayPool<ValueTask>.Shared.Return(tbuff);
                                }
                                managercl.End();
                                gd.SubmitCommands(managercl);
                                for (int i = 0; i < dispatchs; i++)
                                    gd.SubmitCommands(activeDispatchs[i].WaitForEnd());
                                Array.Clear(activeDispatchs, 0, dispatchs);
#endif
                            }

#if !FORCE_GM_NOPARALLEL

                            managercl.Begin();
                            PrepareForDraw(managercl, gd.SwapchainFramebuffer); // Set the base of the frame: clear the background, etc.
                            var tbuff = ArrayPool<ValueTask>.Shared.Rent(resBufferFill);
                            try
                            {
                                for (int i = 0; i < resBufferFill; i++)
                                    tbuff[i] = resBuffer[i].InternalUpdate(managercl).Preserve();
                                for (int i = 0; i < resBufferFill; i++) await tbuff[i];
                            }
                            finally
                            {
                                ArrayPool<ValueTask>.Shared.Return(tbuff);
                            }
                            managercl.End();
                            gd.SubmitCommands(managercl);
                            for (int i = 0; i < dispatchs; i++)
                                gd.SubmitCommands(activeDispatchs[i].WaitForEnd());
                            Array.Clear(activeDispatchs, 0, dispatchs);
#endif
                        }
                    }
                    finally
                    {
                        drawlock.Release(); // End the general drawing stage
                    }
                
                    if (GUIElements.Count > 0) // There's no need to lock neither glock nor ImGUI lock if there are no elements to render. And if it does change between this check and the second one, then tough luck and it'll have to wait until the next frame
                    {
                        if (!await WaitOn(glock, condition: isRunning)) break; // Frame Render Stage 2: GUI Drawing
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

            {
                var tdopr = ValueTask.CompletedTask;
                if (DOPRegistrationBuffer.Count > 0)
                    tdopr = ProcessDrawOpRegistrationBuffer().Preserve();
                var tsdrr = ValueTask.CompletedTask;
                if (SDRRegistrationBuffer.Count > 0)
                    tsdrr = ProcessSharedResourceRegistrationBuffer().Preserve();

                DeferredCallScheduleUpdater();
                await tdopr;
                await tsdrr;
            }

            gd.WaitForIdle(); // Wait for operations to finish
            gd.SwapBuffers(); // Present

            // Code that does not require any resources and is not bothered if resources are suddenly released

            lock (SnapshotPool)
            {
                snapshotBuffer.CopyTo(CurrentSnapshot);
                snapshotBuffer.Clear();
            }

            frameCount++;
            delta = sw.Elapsed;
            fak.Push(1000 / (sw.ElapsedMilliseconds + 0.0000001f));
            sw.Restart();
        }
        InternalLog?.Information("Exiting main rendering loop and disposing");

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
        CreateWindow(out var window, out var gd);
        Window = window;
        Device = gd;
        var factory = gd.ResourceFactory;

        var (ww, wh) = window.Size;
        ImGuiController = new(gd, gd.SwapchainFramebuffer.OutputDescription, ww, wh);

        var bufferDesc = new BufferDescription(DataStructuring.FitToUniformBuffer<WindowTransformation, uint>(), BufferUsage.UniformBuffer);
        var dTransDesc = new ResourceLayoutDescription(new ResourceLayoutElementDescription("DrawParameters", ResourceKind.UniformBuffer, ShaderStages.Vertex));
        var dotransl = new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("Transform", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment)
        );

        WindowTransformBuffer = factory.CreateBuffer(ref bufferDesc);
        DrawTransformationLayout = factory.CreateResourceLayout(ref dTransDesc);
        DrawOpTransLayout = factory.CreateResourceLayout(ref dotransl);
        ManagerResourceBindings = new BindableResource[] { WindowTransformBuffer };
        ManagerResourceSet = factory.CreateResourceSet(new ResourceSetDescription(GetManagerResourceLayout(factory), WindowTransformBuffer));
        ManagerResourceSet.Name = $"{(Name is not null ? "->" : null)}GraphicsManagerResources";

        Window_SizeChanged(window, Game.TotalTime, window.Size);

        window.SizeChanged += Window_SizeChanged;
        window.Closed += Window_Closed;
        window.KeyPressed += Window_KeyPressed;
        window.KeyReleased += Window_KeyReleased;
        window.MouseButtonPressed += Window_MouseButtonPressed;
        window.MouseButtonReleased += Window_MouseButtonReleased;
        window.MouseMoved += Window_MouseMoved;
        window.MouseWheelScrolled += Window_MouseWheelScrolled;
        window.MouseExited += Window_MouseExited;
        window.Hidden += Window_Hidden;
        window.Shown += Window_Shown;
        window.FocusGained += Window_FocusGained;
        window.FocusLost += Window_FocusLost;
    }

    private void Window_FocusLost(Window sender, TimeSpan timestamp) => HasFocus = false;

    private void Window_FocusGained(Window sender, TimeSpan timestamp) => HasFocus = true;

    private void Window_MouseExited(Window sender, TimeSpan timestamp, Point delta, Point newPosition, uint mouseId, MouseButton pressed)
    {
        lock (SnapshotPool)
            snapshotBuffer.butt = 0;
    }

    /// <summary>
    /// <c>true</c> if <see cref="Window"/> is currently visible, or shown. <c>false</c> otherwise.
    /// </summary>
    /// <remarks>
    /// This <see cref="GraphicsManager"/> will *NOT* run or update while <see cref="Window"/> is inactive
    /// </remarks>
    public bool IsWindowAvailable { get; private set; }

    /// <summary>
    /// <c>true</c> if <see cref="Window"/> currently has input focus. <c>false</c> otherwise
    /// </summary>
    public bool HasFocus { get; private set; }

    /// <summary>
    /// Whether the current <see cref="GraphicsManager"/> should currently be updating or not
    /// </summary>
    public bool IsRendering => IsWindowAvailable && (!PauseOnInputLoss || HasFocus);

    private void Window_Shown(Window sender, TimeSpan timestamp) => UpdateWindowAvailability(true);

    private void Window_Hidden(Window sender, TimeSpan timestamp) => UpdateWindowAvailability(false);

    private void UpdateWindowAvailability(bool isAvailable)
    {
        IsWindowAvailable = isAvailable;
        lock (SnapshotPool)
            snapshotBuffer.butt = 0;
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
