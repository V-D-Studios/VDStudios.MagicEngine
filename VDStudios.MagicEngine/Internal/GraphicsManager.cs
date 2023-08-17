using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Input;

namespace VDStudios.MagicEngine.Internal;

/// <summary>
/// Represents an abstract GraphicsManager that is not tied to a specific GraphicsContext.
/// </summary>
/// <remarks>
/// This class cannot be instanced outside this library, as it is not meant to be used outside of this library.
/// </remarks>
public abstract class GraphicsManager : GameObject
{
    internal GraphicsManager(Game game) : base(game, "Graphics & Input", "Rendering")
    {
        snapshotBuffer = CreateNewEmptyInputSnapshotBuffer();
        initLock.Wait();
        Game.graphicsManagersAwaitingSetup.Enqueue(this);
    }

    #region Input Management

    private readonly InputSnapshotBuffer snapshotBuffer;
    private InputSnapshot? inputSnapshot;

    /// <summary>
    /// Creates a new Empty <see cref="InputSnapshotBuffer"/> to be added to the SnapshotPool
    /// </summary>
    /// <returns></returns>
    protected abstract InputSnapshotBuffer CreateNewEmptyInputSnapshotBuffer();

    internal InputSnapshot FetchSnapshot()
    {
        if (inputSnapshot is null)
            lock (snapshotBuffer)
                if (inputSnapshot is null)
                {
                    snapshotBuffer.FetchLastMomentData();
                    inputSnapshot = snapshotBuffer.CreateSnapshot();
                    snapshotBuffer.Clear();
                }

        return inputSnapshot;
    }

    private readonly SemaphoreSlim InputSemaphore = new(1, 1);

    internal readonly SemaphoreSlim initLock = new(1, 1);
    private Task? InputPropagationTask;

    
    
    private Task graphics_thread;

    /// <summary>
    /// Propagates input across all of <see cref="InputReady"/>'s subscribers
    /// </summary>
    protected virtual async ValueTask SubmitInput(CancellationToken ct = default)
    {
        if (InputSemaphore.Wait(50, ct) is false)
            await InputSemaphore.WaitAsync(ct);

        try
        {
            if (InputPropagationTask is not null)
                await InputPropagationTask;

            InputPropagationTask = Task.Run(() =>
            {
                try
                {
                    FireInputReady();
                }
                finally
                {
                    InputSemaphore.Release();
                }
            }, ct);
        }
        catch // If an exception is thrown before the task can be scheduled, the InputSemaphore should be released
        {
            InputSemaphore.Release();
            throw;
        }
    }

    /// <summary>
    /// Fires <see cref="InputReady"/>
    /// </summary>
    protected void FireInputReady()
        => InputReady?.Invoke(this, FetchSnapshot(), DateTime.Now);

    /// <summary>
    /// Fired when this <see cref="GraphicsManager"/> has finished preparing user input
    /// </summary>
    public event GraphicsManagerInputEventHandler? InputReady;

    /// <summary>
    /// Reports a mouse wheel event to the current staging snapshot
    /// </summary>
    /// <param name="mouseId">The id of the mouse if applicable</param>
    /// <param name="delta">The mouse's wheel delta</param>
    protected void ReportMouseWheelEvent(uint mouseId, Vector2 delta)
    {
        lock (snapshotBuffer)
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
        lock (snapshotBuffer)
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
        lock (snapshotBuffer)
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
        lock (snapshotBuffer)
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
        lock (snapshotBuffer)
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
        lock (snapshotBuffer)
            snapshotBuffer.ReportKeyPressed(keyboardId, scancode, key, modifiers, false, repeat, unicode);
    }

    #endregion

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

    internal void WaitForInit()
    {
        initLock.Wait();
        initLock.Release();
    }

        
    internal async ValueTask AwaitIfFaulted()
    {
        if (graphics_thread.IsFaulted)
            await graphics_thread;
    }

    
    
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

    
    
    
    /// <summary>
    /// Performs task such as Creating and Setting up the Window
    /// </summary>
    protected abstract void SetupGraphicsManager();

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
    /// The main running method of this <see cref="GraphicsManager{TGraphicsContext}"/>
    /// </summary>
    /// <remarks>
    /// This is a very advanced method, the entire state of the GraphicsManager is dependent on this method and thus must be understood perfectly. This method will be called strictly once, as it is expected the rendering loop to run within it
    /// </remarks>
    protected abstract Task Run();
}
