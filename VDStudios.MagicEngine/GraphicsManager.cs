using SDL2.NET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using VDStudios.MagicEngine.Exceptions;
using VDStudios.MagicEngine.Internal;
using Veldrid;
using static System.Formats.Asn1.AsnWriter;
using VeldridPixelFormat = Veldrid.PixelFormat;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a Thread dedicated solely to handling a specific pair of <see cref="SDL2.NET.Window"/> and <see cref="GraphicsDevice"/>, and managing their respective resources in a thread-safe manner
/// </summary>
public class GraphicsManager : GameObject, IDisposable
{
    #region Construction

    /// <summary>
    /// Instances and constructs a new <see cref="GraphicsManager"/> objects
    /// </summary>
    public GraphicsManager(Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        currentScene = scene;
        CreateWindow(out var win, out var gd);
        Window = win;
        Device = gd;
        _thread = new(new ThreadStart(() => Run().Wait()));
        Game.graphicsManagersAwaitingSetup.Enqueue(this);
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Represents the current Updates-per-second value calculated while the game is running
    /// </summary>
    /// <remarks>
    /// This value does not represent the <see cref="Game"/>'s FPS, as that is the amount of frames the game outputs per second. This value is only updated while the game is running, also not during <see cref="Load(Progress{float}, IServiceProvider)"/> or any of the other methods
    /// </remarks>
    public float FPS => _fps;
    private float _fps;

    #endregion

    #region Thread

    private Thread _thread;

    #endregion

    #region Scene

    /// <summary>
    /// The current Scene in this <see cref="GraphicsManager"/>
    /// </summary>
    /// <remarks>
    /// Can never be null
    /// </remarks>
    public Scene CurrentScene
    {
        get => currentScene;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(value, currentScene))
                return;

            if (!SceneChanging(value, out var reason))
                throw new SceneChangeRejectedException(reason);

            var prev = currentScene;
            currentScene = value;
            SceneChanged?.Invoke(this, Game.TotalTime, prev, value);
        }
    }
    private Scene currentScene;

    #region Reaction Methods

    /// <summary>
    /// This method is called automatically when <see cref="CurrentScene"/> is changing
    /// </summary>
    /// <param name="newScene">The <see cref="Scene"/> being set</param>
    /// <param name="rejectionReason">An optional out parameter that should be set to the reason the change was rejected if the method returns <c>false</c></param>
    /// <returns><c>true</c> if the change is acceptable, <c>false</c> if the change should be rejected</returns>
    protected virtual bool SceneChanging(Scene newScene, [NotNullWhen(false)] out string? rejectionReason)
    {
        rejectionReason = null;
        return true;
    }

    #endregion

    #region Events

    /// <summary>
    /// Fired when <see cref="CurrentScene"/> changes
    /// </summary>
    public GraphicsManagerSceneChangedEvent? SceneChanged;

    #endregion

    #endregion

    #region Running

    #region Public Properties

    /// <summary>
    /// Whether this <see cref="GraphicsManager"/> is currently running
    /// </summary>
    public bool IsRunning { get; private set; }

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

    #endregion

    #region Internal

    private readonly SemaphoreSlim FrameLock = new(1, 1);

    internal void InternalStart()
    {
        Starting();
        _thread.Start();
    }

    private async Task Run()
    {
        var sw = new Stopwatch();
        var drawqueue = new DrawQueue();

        Scene scene;

        var gd = Device;
        var window = Window;
        var factory = gd.ResourceFactory;
        var commands = factory.CreateCommandList();

        while (IsRunning)
        {
            if (!FrameLock.Wait(500))
                continue; // If 500ms pass and the FrameLock is still not released, check IsRunning again

            var (rw, rh) = window.Size;

            scene = CurrentScene;

            await scene.Draw(drawqueue).ConfigureAwait(true);

            using (drawqueue._lock.Lock())
                while (drawqueue.Count > 0)
                    drawqueue.Dequeue().InternalDraw(new Vector2(rw / 2, rh / 2), gd);

            FrameLock.Release(1); // Code that does not require any resources and is not bothered if resources are suddenly released

            sw.Restart();
            _fps = 1000 / (sw.ElapsedMilliseconds + 0.0000001f);
        }
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

    /// <summary>
    /// This method is automatically called during this object's construction, creates a <see cref="SDL2.NET.Window"/> and <see cref="GraphicsDevice"/> to be used as <see cref="Window"/> and <see cref="Device"/> respectively
    /// </summary>
    /// <remarks>
    /// It's better if you don't call base <see cref="CreateWindow"/>. This is called, specifically, by <see cref="GraphicsManager"/>'s constructor. If this is a derived type, know that this method will be called by the first constructor in the hierarchy, and NOT after your type's constructor
    /// </remarks>
    protected virtual void CreateWindow(out Window mainWindow, out GraphicsDevice graphicsDevice)
    {
        WindowConfig.Default.OpenGL(true);
        mainWindow = new Window(Game.GameTitle, 600, 800, WindowConfig.Default);
        graphicsDevice = Veldrid.Startup.CreateDefaultOpenGLGraphicsDevice(
#if !DEBUG
            new(true, VeldridPixelFormat.R8_G8_B8_A8_UInt, true, ResourceBindingModel.Improved, false, false),
#else
            new(false, VeldridPixelFormat.R8_G8_B8_A8_UInt, true, ResourceBindingModel.Improved, false, false),
#endif
            mainWindow,
            GraphicsBackend.OpenGL
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

    #region Protected Methods

    /// <summary>
    /// Replaces <see cref="Window"/> with <paramref name="window"/> and <see cref="Device"/> with <paramref name="device"/>
    /// </summary>
    /// <param name="window">The <see cref="SDL2.NET.Window"/> to replace <see cref="Window"/> with</param>
    /// <param name="device">The <see cref="GraphicsDevice"/> to replace <see cref="Device"/> with</param>
    /// <param name="disposePrevious">If <c>true</c>, then the previous <see cref="Window"/> and <see cref="Device"/> will be disposed of before exiting the method</param>
    /// <remarks>
    /// This method is very dangerous to use, and should only be used if you're absolutely sure of what you're doing. You're not allowed to use any loaded textures or shaders that were created using a different GraphicsDevice.
    /// </remarks>
    protected void SetMainWindow(Window window, GraphicsDevice device, bool disposePrevious = true)
    {
        if (!ReferenceEquals(window, Window))
        {
            if (!WindowChanging(window, out var error))
                throw new WindowChangeRejectedException(error);
            Window = window;
            try
            {
                WindowChanged?.Invoke(this, Game.TotalTime, window);
            }
            catch { }
        }

        if (!ReferenceEquals(device, Device))
        {
            if (!GraphicsDeviceChanging(device, out var error))
                throw new GraphicsDeviceChangeRejectedException(error);
            Device = device;
            try
            {
                DeviceChanged?.Invoke(this, Game.TotalTime, device);
            }
            catch { }
        }
    }

    #endregion

    #region Reaction Methods

    /// <summary>
    /// This method is automatically called when <see cref="Window"/> is changing
    /// </summary>
    /// <remarks>
    /// This method is called before <see cref="WindowChanged"/> is fired
    /// </remarks>
    /// <param name="newWindow">The new <see cref="Window"/> being set</param>
    /// <param name="error">An optional error that explains the reason the <see cref="Window"/> could not be changed</param>
    /// <returns><c>true</c> if the change is accepted, <c>false</c> otherwise. If this method returns <c>false</c>, a <paramref name="error"/> should contain an explanation as for why</returns>
    protected virtual bool WindowChanging(Window newWindow, [NotNullWhen(false)] out string? error)
    {
        error = null;
        return true;
    }

    /// <summary>
    /// This method is automatically called when <see cref="Device"/> is changing
    /// </summary>
    /// <remarks>
    /// This method is called before <see cref="DeviceChanged"/> is fired
    /// </remarks>
    /// <param name="newDevice">The new <see cref="GraphicsDevice"/> being set</param>
    /// <param name="error">An optional error that explains the reason the <see cref="Window"/> could not be changed</param>
    /// <returns><c>true</c> if the change is accepted, <c>false</c> otherwise. If this method returns <c>false</c>, a <paramref name="error"/> should contain an explanation as for why</returns>
    protected virtual bool GraphicsDeviceChanging(GraphicsDevice newDevice, [NotNullWhen(false)] out string? error)
    {
        error = null;
        return true;
    }

    #endregion

    #region Events

    /// <summary>
    /// Fired when <see cref="Window"/> changes
    /// </summary>
    public event GraphicsManagerWindowChangedEvent? WindowChanged;

    /// <summary>
    /// Fired when <see cref="Device"/> changes
    /// </summary>
    public event GraphicsManagerRendererChangedEvent? DeviceChanged;

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

        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        InternalDispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}

/// <summary>
/// Represents a <see cref="GraphicsManager"/> that is controlled entirely by <see cref="Game"/>
/// </summary>
public class GameGraphicsManager : GraphicsManager
{
    /// <inheritdoc/>
    public GameGraphicsManager(Scene scene) : base(scene) { }

    protected override bool GraphicsDeviceChanging(GraphicsDevice newDevice, [NotNullWhen(false)] out string? error)
    {
        error = "Only the game can change this GraphicsDevice";
        return false;
    }
}