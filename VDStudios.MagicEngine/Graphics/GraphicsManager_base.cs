using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using VDStudios.MagicEngine.Input;
using VDStudios.MagicEngine.Timing;

namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// Represents an abstract GraphicsManager that is not tied to a specific GraphicsContext.
/// </summary>
/// <remarks>
/// This class cannot be instanced, and cannot be inherited outside this library
/// </remarks>
public abstract class GraphicsManager : DisposableGameObject
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

    /// <summary>
    /// Obtains an <see cref="InputSnapshot"/> of the input that was processed this frame
    /// </summary>
    /// <remarks>
    /// The produced <see cref="InputSnapshot"/> is cached, and must be cleared before every frame through <see cref="ClearSnapshot"/>
    /// </remarks>
    protected InputSnapshot FetchSnapshot()
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

    /// <summary>
    /// Clears the cached <see cref="InputSnapshot"/>, so that <see cref="FetchSnapshot"/> may produce a new one
    /// </summary>
    protected void ClearSnapshot()
    {
        if (inputSnapshot is not null)
            lock (snapshotBuffer)
                if (inputSnapshot is not null)
                    inputSnapshot = null;
    }

    private readonly SemaphoreSlim InputSemaphore = new(1, 1);

    internal readonly SemaphoreSlim initLock = new(1, 1);

    private Task? InputPropagationTask;

    internal ulong FrameCount { get; private set; }

    /// <summary>
    /// The object that maintains the information that is fed to <see cref="FramesPerSecond"/>
    /// </summary>
    protected readonly FloatAverageKeeper DeltaAverageKeeper = new(10);
    
    /// <summary>
    /// Whether the <see cref="GraphicsManager{TGraphicsContext}"/> should stop rendering when it loses input focus
    /// </summary>
    public bool PauseOnInputLoss { get; set; }

    /// <summary>
    /// Represents the current Frames-per-second value calculated while this <see cref="GraphicsManager{TGraphicsContext}"/> is running
    /// </summary>
    public float FramesPerSecond => DeltaAverageKeeper.Average;

    /// <summary>
    /// The current Window's Size
    /// </summary>
    public abstract IntVector2 WindowSize { get; protected set; }

    /// <summary>
    /// The color to draw when the frame is beginning to be drawn
    /// </summary>
    public virtual RgbaVector BackgroundColor { get; set; } = RgbaVector.CornflowerBlue;

    /// <summary>
    /// Propagates input across all of <see cref="InputReady"/>'s subscribers
    /// </summary>
    protected virtual void SubmitInput(InputSnapshot snapshot, CancellationToken ct = default)
    {
        InputSemaphore.Wait(ct);

        try
        {
            InputPropagationTask?.ConfigureAwait(false).GetAwaiter().GetResult();

            InputPropagationTask = Task.Run(() =>
            {
                try
                {
                    FireInputReady(snapshot);
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
    /// Propagates input across all of <see cref="InputReady"/>'s subscribers
    /// </summary>
    protected virtual async ValueTask SubmitInputAsync(InputSnapshot snapshot, CancellationToken ct = default)
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
                    FireInputReady(snapshot);
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
    protected void FireInputReady(InputSnapshot snapshot)
        => InputReady?.Invoke(this, snapshot, Game.TotalTime);

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

    /// <summary>
    /// Reports a text input event to the current staging snapshot
    /// </summary>
    /// <param name="text">The text inputted</param>
    protected void ReportTextInput(string text)
    {
        lock (snapshotBuffer)
            snapshotBuffer.ReportTextInput(text);
    }

    #endregion

    /// <summary>
    /// Creates a new <see cref="GraphicsManagerFrameTimer"/> with <paramref name="frameInterval"/> as its lapse
    /// </summary>
    public GraphicsManagerFrameTimer GetFrameTimer(uint frameInterval)
        => new(this, frameInterval);

    /// <summary>
    /// Waits for a <see cref="SemaphoreSlim"/> lock to be unlocked and adquires the lock if possible
    /// </summary>
    /// <param name="semaphore">The <see cref="SemaphoreSlim"/> to wait on</param>
    /// <param name="condition">The condition to continue waiting for the semaphore. If <see langword="true"/> the method will continue waiting until the other parameters are up, otherwise, it will break immediately.</param>
    /// <param name="syncWait">The amount of milliseconds to wait synchronously before to checking on the condition</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to cancel the operation</param>
    /// <param name="waitlock">The object that can be used to release the semaphore through an using statement</param>
    /// <returns><see langword="true"/> if <paramref name="semaphore"/>'s lock was adquired, <see langword="false"/> otherwise</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static bool WaitOn(SemaphoreSlim semaphore, Func<bool> condition, [NotNullWhen(true)][MaybeNullWhen(false)] out WaitLockDisposable waitlock, int syncWait = 15, CancellationToken ct = default)
    {
        if (!semaphore.Wait(syncWait, ct))
        {
            if (!condition())
            {
                waitlock = default;
                return false;
            }
        }

        //succesfully adquired the lock
        waitlock = new(semaphore);
        return true;
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
    protected static async ValueTask<SuccessResult<WaitLockDisposable>> WaitOnAsync(SemaphoreSlim semaphore, Func<bool> condition, int syncWait = 15, int asyncWait = 50, int syncRepeats = 10, CancellationToken ct = default)
    {
        while (syncRepeats-- > 0)
            if (!semaphore.Wait(syncWait, ct))
            {
                if (!condition())
                    return SuccessResult<WaitLockDisposable>.Failure;
            }
            else //succesfully adquired the lock
                return new SuccessResult<WaitLockDisposable>(new WaitLockDisposable(semaphore), true);
        // ran out of repeats without adquiring the lock

        while (!await semaphore.WaitAsync(asyncWait, ct))
            if (!condition())
                return SuccessResult<WaitLockDisposable>.Failure;
        return new SuccessResult<WaitLockDisposable>(new WaitLockDisposable(semaphore), true);
    }

    /// <summary>
    /// A struct that, when disposed, releases the lock it references. 
    /// </summary>
    /// <remarks>
    /// For internal purposes only and should only be used as such: <c><see langword="using"/> (<see langword="var"/> waited = <see cref="WaitOn(SemaphoreSlim, Func{bool}, out WaitLockDisposable, int, CancellationToken)"/>) { /* ... */ }</c>
    /// </remarks>
    protected readonly struct WaitLockDisposable : IDisposable
    {
        private readonly SemaphoreSlim sem;

        /// <summary>
        /// Creates a new object of type <see cref="WaitLockDisposable"/>
        /// </summary>
        public WaitLockDisposable(SemaphoreSlim semaphore)
        {
            Debug.Assert(semaphore is not null, "This WaitLockDisposable's Semaphore is unexpectedly null");
            sem = semaphore;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Debug.Assert(sem is not null, "This WaitLockDisposable's Semaphore is unexpectedly null");
            sem.Release();
        }
    }

    internal async ValueTask<bool> WaitForInitAsync(int millisecondsTimeout = -1, CancellationToken ct = default)
    {
        if (millisecondsTimeout is <= 50 and >= 0)
        {
            if (initLock.Wait(millisecondsTimeout, ct))
            {
                initLock.Release();
                return true;
            }
            return false;
        }

        int wait = millisecondsTimeout - 50;
        if (initLock.Wait(50, ct) || await initLock.WaitAsync(wait, ct))
        {
            initLock.Release();
            return true;
        }

        return false;
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
    /// Takes a screenshot of the screen at the end of the current frame and uploads it to <see cref="Stream"/>
    /// </summary>
    /// <param name="output">The output <see cref="Stream"/> into which to write the image</param>
    /// <param name="format">The file format of the resulting image</param>
    /// <param name="jpegQuality">The JPEG quality, where [0; 33] is Lowest quality, [34; 66] is Middle quality, [67; 100] is Highest quality. If <paramref name="format"/> is not <see cref="ScreenshotImageFormat.JPG"/>, this parameter is ignored</param>
    public abstract ValueTask TakeScreenshot(Stream output, ScreenshotImageFormat format, int jpegQuality = 100);

    /// <summary>
    /// Creates a new <see cref="FrameHook"/>, attaches it to this <see cref="GraphicsManager{TGraphicsContext}"/> and returns it for consumption
    /// </summary>
    /// <remarks>
    /// Dispose of the <see cref="FrameHook"/> to disconnect it from this <see cref="GraphicsManager{TGraphicsContext}"/>
    /// </remarks>
    public abstract FrameHook AttachFramehook();

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
    internal void InternalStart()
    {
        InternalLog?.Information("Starting GraphicsManager");
        Starting();

        InternalLog?.Information("Setting up Window");
        SetupGraphicsManager();
        InternalLog?.Information("Running GraphicsManager");

        Launch();
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
    /// Checks if the running worker of this <see cref="GraphicsManager"/> is faulted, and if so, awaits it to throw an exception
    /// </summary>
    protected internal abstract ValueTask AwaitIfFaulted();

    /// <summary>
    /// This method is called automatically when this <see cref="GraphicsManager{TGraphicsContext}"/> is about to start
    /// </summary>
    /// <remarks>
    /// A <see cref="GraphicsManager{TGraphicsContext}"/> starts only at the beginning of the first frame it can, and it's done from the default thread
    /// </remarks>
    protected virtual void Starting() { }

    /// <summary>
    /// The main running method of this <see cref="GraphicsManager{TGraphicsContext}"/>. This method is expected to be called once and return, having initialized a Thread or scheduled a long running Task for <see cref="GraphicsManager{TGraphicsContext}.Run"/>
    /// </summary>
    protected abstract void Launch();

    /// <summary>
    /// Notifies this <see cref="GraphicsManager{TGraphicsContext}"/> that a new Frame has finished
    /// </summary>
    protected void CountFrame() => FrameCount++;

    /// <summary>
    /// Attempts to get the target framerate of this <see cref="GraphicsManager{TGraphicsContext}"/> 
    /// </summary>
    /// <param name="targetFrameRate">The target framerate if this method returns <see langword="true"/>. <see langword="default"/> otherwise</param>
    /// <returns><see langword="true"/> if this method was able to adquire a target framerate value. <see langword="false"/> otherwise.</returns>
    public abstract bool TryGetTargetFrameRate([MaybeNullWhen(false)] [NotNullWhen(true)] out TimeSpan targetFrameRate);
}
