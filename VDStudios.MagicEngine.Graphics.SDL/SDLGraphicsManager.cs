using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Security.Principal;
using SDL2.NET;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Input;

namespace VDStudios.MagicEngine.Graphics.SDL;

/// <summary>
/// A <see cref="GraphicsManager{TGraphicsContext}"/> for SDL
/// </summary>
public class SDLGraphicsManager : GraphicsManager<SDLGraphicsContext>
{
    /// <inheritdoc/>
    public override IntVector2 WindowSize { get; protected set; }

    private SDLGraphicsContext? context;

    /// <summary>
    /// The SDL Window managed by this <see cref="SDLGraphicsManager"/>
    /// </summary>
    public Window Window { get => window ?? throw new InvalidOperationException("Cannot get the Window of an SDLGraphicsManager that has not been launched"); private set => window = value; }
    private Window? window;

    /// <summary>
    /// The SDL Renderer managed by this <see cref="SDLGraphicsManager"/>
    /// </summary>
    public Renderer Renderer { get => renderer ?? throw new InvalidOperationException("Cannot get the Renderer of an SDLGraphicsManager that has not been launched"); private set => renderer = value; }
    private Renderer? renderer;

    /// <summary>
    /// <see cref="Window"/>'s configuration
    /// </summary>
    public WindowConfig WindowConfig { get; }

    /// <inheritdoc/>
    public SDLGraphicsManager(Game game, WindowConfig? windowConfig = null) : base(game) 
    {
        WindowConfig = windowConfig ?? WindowConfig.Default;
        WindowActionQueue = new ConcurrentQueue<Action<Window>>();
    }

    /// <inheritdoc/>
    protected override SDLGraphicsContext FetchGraphicsContext()
    {
        Debug.Assert(window is not null, "Window is unexpectedly null");
        Debug.Assert(renderer is not null, "Renderer is unexpectedly null");
        lock (Sync)
            return context ??= new(this);
    }

    /// <inheritdoc/>
    protected override void FramelockedDispose(bool disposing)
    {
        DisposeSDLResources();
    }

    /// <summary>
    /// Disposes of SDL's resources by unsubscribing from events and disposing of <see cref="Renderer"/> and <see cref="Window"/>
    /// </summary>
    protected void DisposeSDLResources()
    {
        if (Window is Window win)
        {
            Renderer ren = Renderer;
            Debug.Assert(ren is not null, "Despite Window not being null, Renderer is unexpectedly null");

            win.Closed -= Window_Closed;
            win.SizeChanged -= Window_SizeChanged;
            win.Shown -= Window_Shown;
            win.FocusGained -= Window_FocusGained;
            win.FocusTaken -= Window_FocusGained;
            win.FocusLost -= Window_FocusLost;
            win.Hidden -= Window_Hidden;
            win.KeyPressed -= Window_KeyPressed;
            win.KeyReleased -= Window_KeyReleased;
            win.MouseButtonPressed -= Window_MouseButtonPressed;
            win.MouseButtonReleased -= Window_MouseButtonReleased;
            win.MouseMoved -= Window_MouseMoved;

            Renderer.Dispose();
            win.Dispose();
        }

        window = null;
        renderer = null;

        IsWindowAvailable = false;
        IsRunning = false;
    }

    /// <inheritdoc/>
    protected override InputSnapshotBuffer CreateNewEmptyInputSnapshotBuffer()
        => new SDLInputSnapshotBuffer(this);

    /// <inheritdoc/>
    protected override void SetupGraphicsManager() { }

    /// <inheritdoc/>
    protected override async ValueTask AwaitIfFaulted()
    {
        if (WindowThread is null)
            throw new InvalidOperationException();

        if (WindowThread.IsCompleted)
        {
            try
            {
                await WindowThread;
            }
            finally
            {
                IsRunning = false;
            }
        }
    }

    /// <summary>
    /// Locked while the GUI is being drawn
    /// </summary>
    /// <remarks>
    /// Locked while <see cref="GraphicsManager{TGraphicsContext}.FrameLock"/> is locked. After <see cref="GraphicsManager{TGraphicsContext}.DrawLock"/>
    /// </remarks>
    protected readonly SemaphoreSlim GUILock = new(1, 1);

    #region Window Thread

    internal readonly SemaphoreSlim WindowThreadLock = new(1, 1);
    private Task? WindowThread;
    private readonly ConcurrentQueue<Action<Window>> WindowActionQueue;

    /// <summary>
    /// Performs the desired action on <see cref="Window"/>, and returns immediately
    /// </summary>
    /// <remarks>
    /// Failure to perform on <see cref="Window"/> from this method will result in an exception, as <see cref="SDL2.NET.Window"/> is NOT thread-safe and, in fact, thread-protected
    /// </remarks>
    /// <param name="action">The action to perform on <see cref="Window"/></param>
    public void PerformOnWindow(Action<Window> action)
    {
        WindowActionQueue.Enqueue(action);
    }

    /// <summary>
    /// This method is called automatically when <see cref="Window"/> changes its size
    /// </summary>
    /// <remarks>
    /// This method is called after the frame is unlocked
    /// </remarks>
    protected virtual void WindowSizeChanged(TimeSpan timestamp, Size newSize) { }

#warning Speaking of DrawQueues, do something about the DrawQueue not taking a position

    private void Run()
    {
        try
        {
            Window = new Window(Game.GameTitle, 800, 600, WindowConfig);
            Renderer = new WindowRenderer(Window);

            Window.Closed += Window_Closed;
            Window.SizeChanged += Window_SizeChanged;
            Window.Shown += Window_Shown;
            Window.FocusGained += Window_FocusGained;
            Window.FocusTaken += Window_FocusGained;
            Window.FocusLost += Window_FocusLost;
            Window.Hidden += Window_Hidden;
            Window.KeyPressed += Window_KeyPressed;
            Window.KeyReleased += Window_KeyReleased;
            Window.MouseButtonPressed += Window_MouseButtonPressed;
            Window.MouseButtonReleased += Window_MouseButtonReleased;
            Window.MouseMoved += Window_MouseMoved;
            Window.MouseWheelScrolled += Window_MouseWheelScrolled;

            IsRunning = true;
        }
        finally
        {
            FrameLock.Release();
        }

        try
        {
            var framelock = FrameLock;
            var drawlock = DrawLock;
            var winlock = WindowShownLock;
            var guilock = GUILock;

            var isRunning = IsRunningCheck;
            var isNotRendering = IsNotRenderingCheck;

            var sw = new Stopwatch();
            var drawqueue = new DrawQueue<SDLGraphicsContext>();
            var removalQueue = new Queue<Guid>(10);
            var drawOpBuffer = new List<DrawOperation<SDLGraphicsContext>>();
            TimeSpan delta = default;

            ulong frameCount = 0;

            PerformOnWindow(w =>
            {
                Log?.Debug("Querying WindowFlags");
                var flags = w.Flags;
                IsWindowAvailable = flags.HasFlag(WindowFlags.Shown);
                HasFocus = flags.HasFlag(WindowFlags.InputFocus);
            });

            var (ww, wh) = WindowSize;

            Log?.Information("Entering main rendering loop");
            while (IsRunning) // Running Loop
            {
                WaitLockDisposable winlockwaiter;

                while (WaitOn(winlock, condition: isNotRendering, out winlockwaiter, syncWait: 500) is false)
                {
                    while (!IsRendering)
                    {
                        Events.Update();
                        Thread.Sleep(100);
                        continue;
                    }
                }

                using (winlockwaiter)
                {
                    while (WindowActionQueue.TryDequeue(out var action))
                        action(Window);


                    if (!WaitOn(framelock, condition: isRunning, out var framelockwaiter)) break; // Frame Render
                    using (framelockwaiter)
                    {
                        Running();

                        var context = GetGraphicsContext();

                        context.Update(delta);
                        context.BeginFrame();

                        if (!WaitOn(drawlock, condition: isRunning, out var drawlockwaiter)) break; // Frame Render Stage 1: General Drawing
                        using (drawlockwaiter)
                        {
                            if (RenderTargets.Count <= 0)
                                Log?.Debug("This GraphicsManager has no render targets");
                            else
                            {
                                drawOpBuffer.Clear();
                                foreach (var drawable in Game.CurrentScene.GetDrawableNodes<SDLGraphicsContext>())
                                    foreach (var dop in drawable.DrawOperationManager.GetDrawOperations(this))
                                        drawOpBuffer.Add(dop);

                                if (drawOpBuffer.Count <= 0)
                                    Log?.Verbose("No draw operations were registered");
                                else
                                {
                                    lock (RenderTargets)
                                    {
                                        foreach (var target in RenderTargets)
                                        {
                                            target.BeginFrame(delta, context);
                                            foreach (var dop in drawOpBuffer)
                                                target.RenderDrawOperation(delta, context, dop);
                                            target.EndFrame(context);
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

                        context.EndAndSubmitFrame();
                    }

                    Events.Update();
                }
                
                SubmitInput();
                // Code that does not require any resources and is not bothered if resources are suddenly released

                frameCount++;
                delta = sw.Elapsed;
                DeltaAverageKeeper.Push(1000 / (sw.ElapsedMilliseconds + 0.0000001f));

                sw.Restart();
            }

            Debug.Assert(IsRunning is false, "The main rendering loop broke even though the GraphicsManager is still supposed to be running");
            Log?.Information("Exiting main rendering loop and disposing");
        }
        finally
        {
            DisposeSDLResources();
        }
    }

    /// <inheritdoc/>
    protected override void Launch()
    {
        FrameLock.Wait();
        WindowThread = Task.Run(Run);
        FrameLock.Wait();
        FrameLock.Release();
    }

    private void Window_MouseWheelScrolled(Window sender, TimeSpan timestamp, uint mouseId, float verticalScroll, float horizontalScroll)
    {
        ReportMouseWheelEvent(mouseId, new Vector2(horizontalScroll, verticalScroll));
    }

    #region Window Events

    private void Window_MouseMoved(Window sender, TimeSpan timestamp, Point delta, Point newPosition, uint mouseId, SDL2.NET.MouseButton pressed)
    {
        ReportMouseMoved(mouseId, delta.ToVector2(), newPosition.ToVector2(), (Input.MouseButton)pressed);
    }

    private void Window_MouseButtonReleased(Window sender, TimeSpan timestamp, uint mouseId, int clicks, SDL2.NET.MouseButton state)
    {
        ReportMouseButtonReleased(mouseId, clicks, (Input.MouseButton)state);
    }

    private void Window_MouseButtonPressed(Window sender, TimeSpan timestamp, uint mouseId, int clicks, SDL2.NET.MouseButton state)
    {
        ReportMouseButtonPressed(mouseId, clicks, (Input.MouseButton)state);
    }

    private void Window_KeyReleased(Window sender, TimeSpan timestamp, SDL2.NET.Scancode scancode, SDL2.NET.Keycode key, SDL2.NET.KeyModifier modifiers, bool isPressed, bool repeat, uint unicode)
    {
        ReportKeyReleased(0, (Input.Scancode)scancode, (Input.Keycode)key, (Input.KeyModifier)modifiers, isPressed, repeat, unicode);
    }

    private void Window_KeyPressed(Window sender, TimeSpan timestamp, SDL2.NET.Scancode scancode, SDL2.NET.Keycode key, SDL2.NET.KeyModifier modifiers, bool isPressed, bool repeat, uint unicode)
    {
        ReportKeyPressed(0, (Input.Scancode)scancode, (Input.Keycode)key, (Input.KeyModifier)modifiers, isPressed, repeat, unicode);
    }

    private void Window_Hidden(Window sender, TimeSpan timestamp)
    {
        IsWindowAvailable = false;
    }

    private void Window_FocusLost(Window sender, TimeSpan timestamp)
    {
        HasFocus = false;
    }

    private void Window_FocusGained(Window sender, TimeSpan timestamp)
    {
        HasFocus = true;
    }

    private void Window_Shown(Window sender, TimeSpan timestamp)
    {
        IsWindowAvailable = true;
    }

    private void Window_Closed(Window sender, TimeSpan timestamp)
    {
        IsWindowAvailable = false;
    }

    private void Window_SizeChanged(Window window, TimeSpan timestamp, Size newSize)
    {
        FrameLock.Wait();
        try
        {
            var (ww, wh) = newSize;
            Log?.Information("Window size changed to {{w:{width}, h:{height}}}", newSize.Width, newSize.Height);

            //ImGuiController.WindowResized(ww, wh);

            WindowSize = new IntVector2(newSize.Width, newSize.Height);
            WindowView = Matrix4x4.CreateScale(wh / (float)ww, 1, 1);
        }
        finally
        {
            FrameLock.Release();
        }

        WindowSizeChanged(timestamp, newSize);
    }

    #endregion

    #endregion
}
