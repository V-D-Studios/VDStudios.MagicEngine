using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SDL2.NET;
using SDL2.NET.Exceptions;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Input;
using static OpenGL.GL;
using static SDL2.Bindings.SDL;

namespace VDStudios.MagicEngine.SDL.Base;

/// <summary>
/// A GraphicsManager base for SDL
/// </summary>
public abstract class SDLGraphicsManagerBase<TGraphicsContext> : GraphicsManager<TGraphicsContext>
    where TGraphicsContext : GraphicsContext<TGraphicsContext>
{
    /// <inheritdoc/>
    protected SDLGraphicsManagerBase(Game game, WindowConfig? windowConfig = null) : base(game)
    {
        WindowConfig = windowConfig ?? WindowConfig.Default;
        WindowView = Matrix4x4.Identity;
    }

    /// <inheritdoc/>
    public override IntVector2 WindowSize { get; protected set; }

    /// <summary>
    /// <see cref="Window"/>'s configuration
    /// </summary>
    public WindowConfig WindowConfig { get; }

    /// <summary>
    /// The SDL Window managed by this <see cref="SDLGraphicsManagerBase{TGraphicsContext}"/>
    /// </summary>
    public Window Window 
    { 
        get => window ?? throw new InvalidOperationException("Cannot get the Window of an SDLGraphicsManager that has not been launched"); 
        protected set => window = value;
    }
    private Window? window;

    /// <summary>
    /// <see langword="true"/> if <see cref="Window"/> is <see langword="null"/>, <see langword="false"/> otherwise.
    /// </summary>
    protected bool IsWindowNull => window is null;

    internal readonly SemaphoreSlim WindowThreadLock = new(1, 1);

    /// <summary>
    /// The read-only queue containing actions queued through <see cref="PerformOnWindow(Action{Window})"/>
    /// </summary>
    protected readonly ConcurrentQueue<Action<Window>> WindowActionQueue = new();

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

    /// <inheritdoc/>
    public override bool TryGetTargetFrameRate([MaybeNullWhen(false), NotNullWhen(true)] out TimeSpan targetFrameRate)
    {
        int rate;
        if (IsWindowNull || (rate = Window.DisplayMode.Value.RefreshRate) <= 0)
        {
            targetFrameRate = default;
            return false;
        }

        targetFrameRate = TimeSpan.FromSeconds(1d / rate);
        return true;
    }

    /// <inheritdoc/>
    protected override InputSnapshotBuffer CreateNewEmptyInputSnapshotBuffer()
        => new SDLInputSnapshotBuffer<TGraphicsContext>(this);

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

    /// <summary>
    /// Disposes of SDL's resources by unsubscribing from events and disposing of <see cref="Renderer"/> and <see cref="Window"/>
    /// </summary>
    protected abstract void DisposeSDLResources();

    /// <inheritdoc/>
    protected override void DisposeRunningResources()
    {
        DisposeSDLResources();
    }

    /// <inheritdoc/>
    protected override void FramelockedDispose(bool disposing)
    {
        DisposeSDLResources();
    }

    /// <inheritdoc/>
    protected override void RunningWindowLocked(TimeSpan delta)
    {
        while (WindowActionQueue.TryDequeue(out var action))
            action(Window);
    }

    /// <inheritdoc/>
    protected override void UpdateWhileWaitingOnWindow()
    {
        Events.Update();
        Thread.Sleep(100);
    }

    /// <inheritdoc/>
    protected override void BeforeWindowRelease()
    {
        Events.Update();
    }

    /// <inheritdoc/>
    protected override void Launch()
    {
        FrameLock.Wait();
        WindowThread = Task.Run(Run);
        FrameLock.Wait();
        FrameLock.Release();
    }

    #endregion

    /// <summary>
    /// Detaches event listeners from <see cref="Window"/> and disposes of it, setting it to <see langword="null"/> and setting <see cref="GraphicsManager{TGraphicsContext}.IsWindowAvailable"/> to <see langword="false"/>
    /// </summary>
    protected void ReleaseWindow()
    {
        if (window is Window win) 
        {
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
            win.TextInput -= Win_TextInput;
            win.Dispose();
        }

        Window = null!;
        IsWindowAvailable = false;
    }

    /// <summary>
    /// Attaches event listeners to <see cref="Window"/> and queries initial data from it
    /// </summary>
    protected void ConfigureWindow()
    {
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
        Window.TextInput += Win_TextInput;

        Log?.Debug("Querying WindowFlags");
        var flags = Window.Flags;
        IsWindowAvailable = flags.HasFlag(WindowFlags.Shown);
        HasFocus = flags.HasFlag(WindowFlags.InputFocus);

        Log?.Debug("Reading WindowSize");
        var (ww, wh) = Window.Size;
        WindowSize = new IntVector2(ww, wh);
        WindowView = Matrix4x4.CreateScale(wh / (float)ww, 1, 1);
    }

    #region Window Events

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    protected void Window_MouseWheelScrolled(Window sender, TimeSpan timestamp, uint mouseId, float verticalScroll, float horizontalScroll)
    {
        ReportMouseWheelEvent(mouseId, new Vector2(horizontalScroll, verticalScroll));
    }

    protected void Window_MouseMoved(Window sender, TimeSpan timestamp, Point delta, Point newPosition, uint mouseId, SDL2.NET.MouseButton pressed)
    {
        ReportMouseMoved(mouseId, delta.ToVector2(), newPosition.ToVector2(), (Input.MouseButton)pressed);
    }

    protected void Window_MouseButtonReleased(Window sender, TimeSpan timestamp, uint mouseId, int clicks, SDL2.NET.MouseButton state)
    {
        ReportMouseButtonReleased(mouseId, clicks, (Input.MouseButton)state);
    }

    protected void Window_MouseButtonPressed(Window sender, TimeSpan timestamp, uint mouseId, int clicks, SDL2.NET.MouseButton state)
    {
        ReportMouseButtonPressed(mouseId, clicks, (Input.MouseButton)state);
    }

    protected void Window_KeyReleased(Window sender, TimeSpan timestamp, SDL2.NET.Scancode scancode, SDL2.NET.Keycode key, SDL2.NET.KeyModifier modifiers, bool isPressed, bool repeat, uint unicode)
    {
        ReportKeyReleased(0, (Input.Scancode)scancode, (Input.Keycode)key, (Input.KeyModifier)modifiers, isPressed, repeat, unicode);
    }

    protected void Window_KeyPressed(Window sender, TimeSpan timestamp, SDL2.NET.Scancode scancode, SDL2.NET.Keycode key, SDL2.NET.KeyModifier modifiers, bool isPressed, bool repeat, uint unicode)
    {
        ReportKeyPressed(0, (Input.Scancode)scancode, (Input.Keycode)key, (Input.KeyModifier)modifiers, isPressed, repeat, unicode);
    }

    protected void Window_Hidden(Window sender, TimeSpan timestamp)
    {
        IsWindowAvailable = false;
    }

    protected void Window_FocusLost(Window sender, TimeSpan timestamp)
    {
        HasFocus = false;
    }

    protected void Win_TextInput(Window sender, TimeSpan timestamp, ReadOnlySpan<char> text)
    {
        ReportTextInput(new string(text));
    }

    protected void Window_FocusGained(Window sender, TimeSpan timestamp)
    {
        HasFocus = true;
    }

    protected void Window_Shown(Window sender, TimeSpan timestamp)
    {
        IsWindowAvailable = true;
    }

    protected void Window_Closed(Window sender, TimeSpan timestamp)
    {
        IsWindowAvailable = false;
    }

    protected void Window_SizeChanged(Window window, TimeSpan timestamp, Size newSize)
    {
        IntVector2 oSize;
        IntVector2 nSize = new(newSize.Width, newSize.Height);
        FrameLock.Wait();
        try
        {
            var (ww, wh) = newSize;
            Log?.Information("Window size changed to {{w:{width}, h:{height}}}", newSize.Width, newSize.Height);

            oSize = WindowSize;
            WindowSize = nSize;

            //ImGuiController.WindowResized(ww, wh);
            WindowSizeChangedFrameLocked(oSize, nSize);
        }
        finally
        {
            FrameLock.Release();
        }

        WindowSizeChanged(oSize, nSize);
    }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    #endregion

}
