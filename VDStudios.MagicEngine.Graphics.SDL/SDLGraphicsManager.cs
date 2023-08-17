﻿using System.Collections.Concurrent;
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
    public override RgbaVector BackgroundColor { get; set; }

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
    protected override ValueTask<SDLGraphicsContext> FetchGraphicsContext()
        => ValueTask.FromResult(context ??= new(this));

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

    #region Window Thread

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
            while (IsRunning)
            {
                while (WindowActionQueue.TryDequeue(out var action))
                    action(Window);

                Events.Update();
                //Thread.Sleep(1000);
            }
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
