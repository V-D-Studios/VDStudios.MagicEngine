﻿using SDL2.NET;
using System;
using System.Buffers;
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
        _thread = new(new ThreadStart(() => Run()));
        Game.graphicsManagersAwaitingSetup.Enqueue(this);
        IdleWaiter = new(FrameLock);
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
        using (await LockManagerAsync())
        {
            operation.Register(node, this);
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

    #region Waiting

    /// <summary>
    /// Wait until the Manager becomes Idle
    /// </summary>
    public void WaitForIdle()
    {
        FrameLock.Wait();
        FrameLock.Release();
    }

    /// <summary>
    /// Asynchronously waits until the Manager becomes Idle
    /// </summary>
    public async Task WaitForIdleAsync()
    {
        await FrameLock.WaitAsync();
        FrameLock.Release();
    }

    /// <summary>
    /// Waits until the Manager becomes idle and locks it
    /// </summary>
    /// <remarks>
    /// *ALWAYS* wrap the disposable object this method returns in an using statement. The GraphicsManager will stay locked forever if it's not disposed of
    /// </remarks>
    /// <returns>An <see cref="IDisposable"/> object that unlocks the manager when disposed of</returns>
    public IDisposable LockManager()
    {
        FrameLock.Wait();
        return IdleWaiter;
    }

    /// <summary>
    /// Asynchronously waits until the Manager becomes idle and locks it
    /// </summary>
    /// <remarks>
    /// *ALWAYS* wrap the disposable object this method returns in an using statement. The GraphicsManager will stay locked forever if it's not disposed of
    /// </remarks>
    /// <returns>An <see cref="IDisposable"/> object that unlocks the manager when disposed of</returns>
    public async Task<IDisposable> LockManagerAsync()
    {
        await FrameLock.WaitAsync();
        return IdleWaiter;
    }

    private readonly idleWaiter IdleWaiter;
    private sealed class idleWaiter : IDisposable
    {
        private SemaphoreSlim framelock;

        public idleWaiter(SemaphoreSlim fl) => framelock = fl;

        public void Dispose()
        {
            framelock.Release();
        }
    }

    #endregion

    #region Running

    #region Public Properties

    /// <summary>
    /// Whether this <see cref="GraphicsManager"/> is currently running
    /// </summary>
    public bool IsRunning { get; private set; } = true;

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
        CreateWindow(out var window, out var gd);
        Window = window;
        Device = gd;

        var sw = new Stopwatch();
        var drawqueue = new DrawQueue();
        var fl = FrameLock;

        while (IsRunning)
        {
            if (!fl.Wait(500))
                continue; // If 500ms pass and the FrameLock is still not released, check IsRunning again

            try
            {
                var (rw, rh) = window.Size;

                Running();

                var ops = RegisteredOperations;

                foreach (var kv in ops)
                    if (kv.Value.TryGetTarget(out var op) && !op.disposedValue) 
                        op.Owner.AddToDrawQueue(drawqueue, op);
                    else
                        ops.Remove(kv.Key);

                using (drawqueue._lock.Lock())
                {
                    var buffers = ArrayPool<ValueTask>.Shared;
                    var calls = buffers.Rent(ops.Count);
                    try
                    {
                        int i = 0;
                        for (; drawqueue.Count > 0; i++)
                            calls[i] = drawqueue.Dequeue().InternalDraw(new Vector2(rw / 2, rh / 2));
                        while (i > 0)
                            await calls[--i];
                    }
                    finally
                    {
                        buffers.Return(calls, true);
                    }
                }
                gd.WaitForIdle();
            }
            finally
            {
                fl.Release(1); // Code that does not require any resources and is not bothered if resources are suddenly released
            }

            _fps = 1000 / (sw.ElapsedMilliseconds + 0.0000001f);
            sw.Restart();
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