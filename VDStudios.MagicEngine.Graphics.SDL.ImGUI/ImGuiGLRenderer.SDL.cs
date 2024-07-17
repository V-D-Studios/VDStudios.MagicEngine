using static SDL2.Bindings.SDL;
using System.Numerics;
using ImGuiNET;
using VDStudios.MagicEngine.Input;

namespace VDStudios.MagicEngine.Graphics.SDL.ImGUI;

public partial class ImGuiGLRenderer
{
    float _time;
    readonly bool[] _mousePressed = [false, false, false];

    public void NewFrame()
    {
        ImGui.NewFrame();
        var io = ImGui.GetIO();

        // Setup display size (every frame to accommodate for window resizing)
        SDL_GetWindowSize(_window, out var w, out var h);
        SDL_GL_GetDrawableSize(_window, out var displayW, out var displayH);
        io.DisplaySize = new Vector2(w, h);
        if (w > 0 && h > 0)
            io.DisplayFramebufferScale = new Vector2((float)displayW / w, (float)displayH / h);

        // Setup time step (we don't use SDL_GetTicks() because it is using millisecond resolution)
        var frequency = SDL_GetPerformanceFrequency();
        var currentTime = SDL_GetPerformanceCounter();
        io.DeltaTime = _time > 0 ? (float)((double)(currentTime - _time) / frequency) : 1.0f / 60.0f;
        if (io.DeltaTime <= 0)
            io.DeltaTime = 0.016f;
        _time = currentTime;

        UpdateMousePosAndButtons();
    }

    public void ProcessEvent(KeyEventRecord ev)
    {
        var io = ImGui.GetIO();

        // Modifiers
        if (ev.Modifiers.HasFlag(KeyModifier.LeftShift))
            io.AddKeyEvent(ImGuiKey.LeftShift, ev.IsPressed);
        if (ev.Modifiers.HasFlag(KeyModifier.RightShift))
            io.AddKeyEvent(ImGuiKey.RightShift, ev.IsPressed);
        if (ev.Modifiers.HasFlag(KeyModifier.LeftAlt))
            io.AddKeyEvent(ImGuiKey.LeftAlt, ev.IsPressed);
        if (ev.Modifiers.HasFlag(KeyModifier.RightAlt))
            io.AddKeyEvent(ImGuiKey.RightAlt, ev.IsPressed);
        if (ev.Modifiers.HasFlag(KeyModifier.LeftCtrl))
            io.AddKeyEvent(ImGuiKey.LeftCtrl, ev.IsPressed);
        if (ev.Modifiers.HasFlag(KeyModifier.RightCtrl))
            io.AddKeyEvent(ImGuiKey.RightCtrl, ev.IsPressed);

        // Keys
        switch (ev.Scancode)
        {
            case Scancode.Escape:
                io.AddKeyEvent(ImGuiKey.Escape, ev.IsPressed);
                break;

            case Scancode.Tab:
                io.AddKeyEvent(ImGuiKey.Tab, ev.IsPressed);
                break;

            case Scancode.Left:
                io.AddKeyEvent(ImGuiKey.LeftArrow, ev.IsPressed);
                break;

            case Scancode.Right:
                io.AddKeyEvent(ImGuiKey.RightArrow, ev.IsPressed);
                break;

            case Scancode.Up:
                io.AddKeyEvent(ImGuiKey.UpArrow, ev.IsPressed);
                break;

            case Scancode.Down:
                io.AddKeyEvent(ImGuiKey.DownArrow, ev.IsPressed);
                break;

            case Scancode.PageUp:
                io.AddKeyEvent(ImGuiKey.PageUp, ev.IsPressed);
                break;

            case Scancode.PageDown:
                io.AddKeyEvent(ImGuiKey.PageDown, ev.IsPressed);
                break;

            case Scancode.Home:
                io.AddKeyEvent(ImGuiKey.Home, ev.IsPressed);
                break;

            case Scancode.End:
                io.AddKeyEvent(ImGuiKey.End, ev.IsPressed);
                break;

            case Scancode.Insert:
                io.AddKeyEvent(ImGuiKey.Insert, ev.IsPressed);
                break;

            case Scancode.Delete:
                io.AddKeyEvent(ImGuiKey.Delete, ev.IsPressed);
                break;

            case Scancode.Backspace:
                io.AddKeyEvent(ImGuiKey.Backspace, ev.IsPressed);
                break;

            case Scancode.Space:
                io.AddKeyEvent(ImGuiKey.Space, ev.IsPressed);
                break;

            case Scancode.Return:
                io.AddKeyEvent(ImGuiKey.Enter, ev.IsPressed);
                break;

            case Scancode.KeyPadEnter:
                io.AddKeyEvent(ImGuiKey.KeypadEnter, ev.IsPressed);
                break;

            case Scancode.A:
                io.AddKeyEvent(ImGuiKey.A, ev.IsPressed);
                break;

            case Scancode.C:
                io.AddKeyEvent(ImGuiKey.C, ev.IsPressed);
                break;

            case Scancode.V:
                io.AddKeyEvent(ImGuiKey.V, ev.IsPressed);
                break;

            case Scancode.X:
                io.AddKeyEvent(ImGuiKey.X, ev.IsPressed);
                break;

            case Scancode.Y:
                io.AddKeyEvent(ImGuiKey.Y, ev.IsPressed);
                break;

            case Scancode.Z:
                io.AddKeyEvent(ImGuiKey.Z, ev.IsPressed);
                break;
        }
    }

    public void ProcessEvent(MouseWheelEventRecord ev)
    {
        var io = ImGui.GetIO();

        if (ev.WheelDelta.X > 0) io.MouseWheelH += 1;
        else if (ev.WheelDelta.X < 0) io.MouseWheelH -= 1;

        if (ev.WheelDelta.Y > 0) io.MouseWheel += 1;
        else if (ev.WheelDelta.Y < 0) io.MouseWheel -= 1;
    }

    public void ProcessEvent(MouseEventRecord ev)
    {
        _mousePressed[0] = ev.Pressed.HasFlag(MouseButton.Left);
        _mousePressed[1] = ev.Pressed.HasFlag(MouseButton.Right);
        _mousePressed[2] = ev.Pressed.HasFlag(MouseButton.Middle);
    }

    public void ProcessEvent(TextInputEventRecord ev)
    {
        var io = ImGui.GetIO();

        io.AddInputCharactersUTF8(ev.Text);
    }

    void UpdateMousePosAndButtons()
    {
        var io = ImGui.GetIO();

        // Set OS mouse position if requested (rarely used, only when ImGuiConfigFlags_NavEnableSetMousePos is enabled by user)
        if (io.WantSetMousePos)
            SDL_WarpMouseInWindow(_window, (int)io.MousePos.X, (int)io.MousePos.Y);
        else
            io.MousePos = new Vector2(float.MinValue, float.MinValue);

        var mouseButtons = SDL_GetMouseState(out var mx, out var my);
        io.MouseDown[0] =
            _mousePressed[0] ||
            (mouseButtons & SDL_BUTTON(SDL_BUTTON_LEFT)) !=
            0; // If a mouse press event came, always pass it as "mouse held this frame", so we don't miss click-release events that are shorter than 1 frame.
        io.MouseDown[1] = _mousePressed[1] || (mouseButtons & SDL_BUTTON(SDL_BUTTON_RIGHT)) != 0;
        io.MouseDown[2] = _mousePressed[2] || (mouseButtons & SDL_BUTTON(SDL_BUTTON_MIDDLE)) != 0;
        _mousePressed[0] = _mousePressed[1] = _mousePressed[2] = false;

        var focusedWindow = SDL_GetKeyboardFocus();
        if (_window == focusedWindow)
        {
            // SDL_GetMouseState() gives mouse position seemingly based on the last window entered/focused(?)
            // The creation of a new windows at runtime and SDL_CaptureMouse both seems to severely mess up with that, so we retrieve that position globally.
            SDL_GetWindowPosition(focusedWindow, out var wx, out var wy);
            SDL_GetGlobalMouseState(out mx, out my);
            mx -= wx;
            my -= wy;
            io.MousePos = new Vector2(mx, my);
        }

        // SDL_CaptureMouse() let the OS know e.g. that our imgui drag outside the SDL window boundaries shouldn't e.g. trigger the OS window resize cursor.
        var any_mouse_button_down = ImGui.IsAnyMouseDown();
        SDL_CaptureMouse(any_mouse_button_down ? SDL_bool.SDL_TRUE : SDL_bool.SDL_FALSE);
    }

    void PrepareGLContext() => SDL_GL_MakeCurrent(_window, _glContext);
}
