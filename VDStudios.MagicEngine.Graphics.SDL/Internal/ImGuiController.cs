using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using SDL2.NET;
using SDL2.NET.Platform.iOSSpecific;
using VDStudios.MagicEngine.Input;
using VDStudios.MagicEngine.Internal;
using static SDL2.Bindings.SDL;
using MouseButton = VDStudios.MagicEngine.Input.MouseButton;
using Scancode = VDStudios.MagicEngine.Input.Scancode;

namespace VDStudios.MagicEngine.Graphics.SDL.Internal;

internal unsafe class ImGuiController
{
    private static readonly byte* SDLRendererNameBytes;
    private readonly object sync = new();
    private readonly nint Context;
    private readonly SDLGraphicsManager Manager;
    private nint SDLRendererData;

    public Vector2 ScaleFactor { get; set; } = Vector2.One;

    static ImGuiController()
    {
        const string SDLRendererName = "imgui_impl_sdlrenderer2\0";
        int len = Encoding.ASCII.GetByteCount(SDLRendererName);
        var ptr = (byte*)Marshal.AllocHGlobal(len);
        Span<byte> dat = new(ptr, len);
        Encoding.ASCII.GetBytes(SDLRendererName, dat);
        SDLRendererNameBytes = ptr;
    }

    #region Helper classes

    // SDL_Renderer data
    private struct ImGui_ImplSDLRenderer2_Data
    {
        public IntPtr SDLRenderer;
        public IntPtr FontTexture;
    }

    // Backup SDL_Renderer state that will be modified to restore it afterwards
    private struct BackupSDLRendererState
    {
        public SDL_Rect Viewport;
        public bool ClipEnabled;
        public SDL_Rect ClipRect;
    };

    #endregion

    public ImGuiController(SDLGraphicsManager manager)
    {
        Context = ImGui.CreateContext();
        //var fonts = ImGui.GetIO().Fonts;
        ImGui.GetIO().Fonts.AddFontDefault();
        SetKeyMappings();
        Manager = manager;

        SetPerFrameImGuiData(1f / 60f);
    }
    
    private void SetPerFrameImGuiData(float deltaSeconds)
    {
        ImGui.SetCurrentContext(Context);
        ImGuiIOPtr io = ImGui.GetIO();
        io.DisplaySize = new Vector2(
            Manager.WindowSize.X / ScaleFactor.X,
            Manager.WindowSize.Y / ScaleFactor.Y);
        io.DisplayFramebufferScale = ScaleFactor;
        io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
    }

    public void UpdateImGuiInput(InputSnapshot snapshot)
    {
        ImGui.SetCurrentContext(Context);
        ImGuiIOPtr io = ImGui.GetIO();

        Vector2 mousePosition = snapshot.MousePosition;

        // Determine if any of the mouse buttons were pressed during this snapshot period, even if they are no longer held.
        bool leftPressed = false;
        bool middlePressed = false;
        bool rightPressed = false;
        foreach (var me in snapshot.MouseEvents)
        {
            if (me.Pressed != 0)
            {
                switch (me.Pressed)
                {
                    case MouseButton.Left:
                        leftPressed = true;
                        break;
                    case MouseButton.Middle:
                        middlePressed = true;
                        break;
                    case MouseButton.Right:
                        rightPressed = true;
                        break;
                }
            }
        }

#if DEBUG
        var rdown = rightPressed || snapshot.IsMouseDown(MouseButton.Right);
        var ldown = leftPressed || snapshot.IsMouseDown(MouseButton.Left);
        var mdown = middlePressed || snapshot.IsMouseDown(MouseButton.Middle);
        io.MouseDown[0] = ldown;
        io.MouseDown[1] = rdown;
        io.MouseDown[2] = mdown;
#else
        io.MouseDown[0] = leftPressed || snapshot.IsMouseDown(MouseButton.Left);
        io.MouseDown[1] = rightPressed || snapshot.IsMouseDown(MouseButton.Right);
        io.MouseDown[2] = middlePressed || snapshot.IsMouseDown(MouseButton.Middle);
#endif
        io.MousePos = mousePosition;
        io.MouseWheel = snapshot.WheelDelta.Y;
        io.MouseWheelH = snapshot.WheelDelta.X;

        IReadOnlyList<uint> keyCharPresses = snapshot.KeyCharPresses;
        for (int i = 0; i < keyCharPresses.Count; i++)
        {
            uint c = keyCharPresses[i];
            io.AddInputCharacter(c);
        }

        foreach (var ke in snapshot.KeyEvents)
        {
            io.KeysDown[(int)ke.Scancode] = ke.IsPressed;
            if (ke.Scancode == Scancode.LeftCtrl)
                io.KeyCtrl = ke.IsPressed;
            if (ke.Scancode == Scancode.LeftShift)
                io.KeyAlt = ke.IsPressed;
            if (ke.Scancode == Scancode.LeftAlt)
                io.KeyShift = ke.IsPressed;
            if (ke.Scancode == Scancode.LeftGUI)
                io.KeySuper = ke.IsPressed;
        }
    }

    private void SetKeyMappings()
    {
        ImGui.SetCurrentContext(Context);
        ImGuiIOPtr io = ImGui.GetIO();
        io.KeyMap[(int)ImGuiKey.Tab] = (int)Scancode.Tab;
        io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Scancode.Left;
        io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Scancode.Right;
        io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Scancode.Up;
        io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Scancode.Down;
        io.KeyMap[(int)ImGuiKey.PageUp] = (int)Scancode.PageUp;
        io.KeyMap[(int)ImGuiKey.PageDown] = (int)Scancode.PageDown;
        io.KeyMap[(int)ImGuiKey.Home] = (int)Scancode.Home;
        io.KeyMap[(int)ImGuiKey.End] = (int)Scancode.End;
        io.KeyMap[(int)ImGuiKey.Delete] = (int)Scancode.Delete;
        io.KeyMap[(int)ImGuiKey.Backspace] = (int)Scancode.Backspace;
        io.KeyMap[(int)ImGuiKey.Enter] = (int)Scancode.Return;
        io.KeyMap[(int)ImGuiKey.Escape] = (int)Scancode.Escape;
        io.KeyMap[(int)ImGuiKey.Space] = (int)Scancode.Space;
        io.KeyMap[(int)ImGuiKey.A] = (int)Scancode.A;
        io.KeyMap[(int)ImGuiKey.C] = (int)Scancode.C;
        io.KeyMap[(int)ImGuiKey.V] = (int)Scancode.V;
        io.KeyMap[(int)ImGuiKey.X] = (int)Scancode.X;
        io.KeyMap[(int)ImGuiKey.Y] = (int)Scancode.Y;
        io.KeyMap[(int)ImGuiKey.Z] = (int)Scancode.Z;
    }

    private static ImGui_ImplSDLRenderer2_Data* GetBackendData()
        => (ImGui_ImplSDLRenderer2_Data*)(ImGui.GetCurrentContext() > 0 ? ImGui.GetIO().BackendRendererUserData : nint.Zero);

    public bool InitializeSDLRenderer(Renderer renderer)
    {
        lock (sync)
        {
            ImGui.SetCurrentContext(Context);
            var io = ImGui.GetIO();
            if (io.BackendRendererUserData != 0)
                return false;

            ArgumentNullException.ThrowIfNull(renderer);

            io.NativePtr->BackendRendererName = SDLRendererNameBytes;

            // Setup backend capabilities flags
            if (SDLRendererData != nint.Zero)
                Marshal.FreeHGlobal(SDLRendererData);
            SDLRendererData = Marshal.AllocHGlobal(sizeof(ImGui_ImplSDLRenderer2_Data) * 4);
            Unsafe.InitBlock(SDLRendererData.ToPointer(), 0, (uint)sizeof(ImGui_ImplSDLRenderer2_Data) * 4);

            io.BackendRendererUserData = SDLRendererData;
            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset; 
            // We can honor the ImDrawCmd::VtxOffset field, allowing for large meshes.

            var bd = (ImGui_ImplSDLRenderer2_Data*)SDLRendererData;
            bd->SDLRenderer = ((IHandle)renderer).Handle;

            // This Marshal.WriteIntPtr is bd->SDLRenderer = renderer;

            return true;
        }
    }

    public void Shutdown()
    {
        lock (sync)
        {
            ImGui.SetCurrentContext(Context);
            ImGui_ImplSDLRenderer2_Data* bd = GetBackendData();
            if (bd == nint.Zero.ToPointer())
                throw new InvalidOperationException("No renderer backend to shutdown, or already shutdown?");

            ImGuiIOPtr io = ImGui.GetIO();

            DestroyDeviceObjects();

            io.NativePtr->BackendRendererName = (byte*)nint.Zero.ToPointer();
            io.BackendRendererUserData = nint.Zero;
            io.BackendFlags &= ~ImGuiBackendFlags.RendererHasVtxOffset;

            if (SDLRendererData != nint.Zero)
                Marshal.FreeHGlobal(SDLRendererData);
        }
    }

    public void SetupRenderState()
    {
        ImGui.SetCurrentContext(Context);
        ImGui_ImplSDLRenderer2_Data* bd = GetBackendData();

        // Clear out any viewports and cliprect set by the user
        // FIXME: Technically speaking there are lots of other things we could backup/setup/restore during our render process.
        SDL_RenderSetViewport(bd->SDLRenderer, nint.Zero);
        SDL_RenderSetClipRect(bd->SDLRenderer, nint.Zero);
    }

    public void NewFrame()
    {
        ImGui.SetCurrentContext(Context);
        ImGui_ImplSDLRenderer2_Data* bd = GetBackendData();
        if (bd == nint.Zero.ToPointer())
            throw new InvalidOperationException("Did you call Init()?");

        if (bd->FontTexture == nint.Zero)
            CreateDeviceObjects();
    }

    public void RenderDrawData()
    {
        ImGui.SetCurrentContext(Context);
        var data = ImGui.GetDrawData();
        ImGui_ImplSDLRenderer2_Data* bd = GetBackendData();
        var draw_data = data.NativePtr;

        // If there's a scale factor set by the user, use that instead
        // If the user has specified a scale factor to SDL_Renderer already via SDL_RenderSetScale(), SDL will scale whatever we pass
        // to SDL_RenderGeometryRaw() by that scale factor. In that case we don't want to be also scaling it ourselves here.
        float rsx = 1.0f;
        float rsy = 1.0f;
        SDL_RenderGetScale(bd->SDLRenderer, out rsx, out rsy);
        Vector2 render_scale = default;
        render_scale.X = (rsx == 1.0f) ? data.FramebufferScale.X : 1.0f;
        render_scale.Y = (rsy == 1.0f) ? data.FramebufferScale.Y : 1.0f;

        // Avoid rendering when minimized, scale coordinates for retina displays (screen coordinates != framebuffer coordinates)
        int fb_width = (int)(data.DisplaySize.X * render_scale.X);
        int fb_height = (int)(data.DisplaySize.Y * render_scale.Y);
        if (fb_width == 0 || fb_height == 0)
            return;

        BackupSDLRendererState old = default;
        old.ClipEnabled = SDL_RenderIsClipEnabled(bd->SDLRenderer) == SDL_bool.SDL_TRUE;
        SDL_RenderGetViewport(bd->SDLRenderer, out old.Viewport);
        SDL_RenderGetClipRect(bd->SDLRenderer, out old.ClipRect);

        // Will project scissor/clipping rectangles into framebuffer space
        Vector2 clip_off = data.DisplayPos;         // (0,0) unless using multi-viewports
        Vector2 clip_scale = render_scale;

        // Render command lists
        SetupRenderState();
        for (int n = 0; n < data.CmdListsCount; n++)
        {
            ImDrawList* cmd_list = draw_data->CmdLists[n];
            ImDrawVert* vtx_buffer = (ImDrawVert*)cmd_list->VtxBuffer.Data;
            ushort* idx_buffer = (ushort*)cmd_list->IdxBuffer.Data;

            for (int cmd_i = 0; cmd_i < cmd_list->CmdBuffer.Size; cmd_i++)
            {
                ImDrawCmd* pcmd = (ImDrawCmd*)cmd_list->CmdBuffer.Address<ImDrawCmd>(cmd_i);

                // Project scissor/clipping rectangles into framebuffer space
                Vector2 clip_min = new((pcmd->ClipRect.X - clip_off.X) * clip_scale.X, (pcmd->ClipRect.Y - clip_off.Y) * clip_scale.Y);
                Vector2 clip_max = new((pcmd->ClipRect.Z - clip_off.X) * clip_scale.X, (pcmd->ClipRect.W - clip_off.Y) * clip_scale.Y);
                if (clip_min.X < 0.0f) { clip_min.X = 0.0f; }
                if (clip_min.Y < 0.0f) { clip_min.Y = 0.0f; }
                if (clip_max.X > fb_width) { clip_max.X = fb_width; }
                if (clip_max.Y > fb_height) { clip_max.Y = fb_height; }
                if (clip_max.X <= clip_min.X || clip_max.Y <= clip_min.Y)
                    continue;

                SDL_Rect r = new()
                {
                    x = (int)(clip_min.X),
                    y = (int)(clip_min.Y),
                    w = (int)(clip_max.X - clip_min.X),
                    h = (int)(clip_max.Y - clip_min.Y)
                };

                SDL_RenderSetClipRect(bd->SDLRenderer, ref r);

                float* xy = (float*)(void*)((char*)(vtx_buffer + pcmd->VtxOffset) + Marshal.OffsetOf<ImDrawVert>(nameof(ImDrawVert.pos)));
                float* uv = (float*)(void*)((char*)(vtx_buffer + pcmd->VtxOffset) + Marshal.OffsetOf<ImDrawVert>(nameof(ImDrawVert.uv)));

                SDL_Color* color = (SDL_Color*)(void*)((char*)(vtx_buffer + pcmd->VtxOffset) + Marshal.OffsetOf<ImDrawVert>(nameof(ImDrawVert.col))); // SDL 2.0.19+
                //int* color = (int*)(void*)((char*)(vtx_buffer + pcmd->VtxOffset) + IM_OFFSETOF(ImDrawVert, col)); // SDL 2.0.17 and 2.0.18

                // Bind texture, Draw
                nint tex = pcmd->TextureId;
                SDL_RenderGeometryRaw(bd->SDLRenderer, tex,
                    xy, (int)sizeof(ImDrawVert),
                    color, (int)sizeof(ImDrawVert),
                    uv, (int)sizeof(ImDrawVert),
                    cmd_list->VtxBuffer.Size - (int)pcmd->VtxOffset,
                    (nint)(idx_buffer + pcmd->IdxOffset), (int)pcmd->ElemCount, sizeof(ushort));
            }
        }

        // Restore modified SDL_Renderer state
        SDL_RenderSetViewport(bd->SDLRenderer, ref old.Viewport);
        SDL_RenderSetClipRect(bd->SDLRenderer, old.ClipEnabled ? (nint)Unsafe.AsPointer(ref old.ClipRect) : nint.Zero);
    }

    public bool CreateFontsTexture()
    {
        ImGui.SetCurrentContext(Context);
        ImGuiIOPtr io = ImGui.GetIO();
        ImGui_ImplSDLRenderer2_Data* bd = GetBackendData();

        // Build texture atlas
        io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height);   // Load as RGBA 32-bit (75% of the memory is wasted, but default font is so small) because it is more likely to be compatible with user's existing shaders. If your ImTextureId represent a higher-level concept than just a GL texture id, consider calling GetTexDataAsAlpha8() instead to save on GPU memory.

        // Upload texture to graphics system
        // (Bilinear sampling is required by default. Set 'io.Fonts->Flags |= ImFontAtlasFlags_NoBakedLines' or 'style.AntiAliasedLinesUseTex = false' to allow point/nearest sampling)
        bd->FontTexture = SDL_CreateTexture(bd->SDLRenderer, SDL_PIXELFORMAT_ABGR8888, (int)SDL_TextureAccess.SDL_TEXTUREACCESS_STATIC, width, height);
        if (bd->FontTexture == nint.Zero)
        {
            SDL_Log("error creating texture");
            return false;
        }
        SDL_UpdateTexture(bd->FontTexture, nint.Zero, (nint)pixels, 4 * width);
        SDL_SetTextureBlendMode(bd->FontTexture, SDL_BlendMode.SDL_BLENDMODE_BLEND);
        SDL_SetTextureScaleMode(bd->FontTexture, SDL_ScaleMode.SDL_ScaleModeLinear);

        // Store our identifier
        io.Fonts.SetTexID(bd->FontTexture);

        return true;
    }

    public void DestroyFontsTexture()
    {
        ImGui.SetCurrentContext(Context);
        ImGuiIOPtr io = ImGui.GetIO();
        ImGui_ImplSDLRenderer2_Data* bd = GetBackendData();
        if (bd->FontTexture > nint.Zero)
        {
            io.Fonts.SetTexID(0);
            SDL_DestroyTexture(bd->FontTexture);
            bd->FontTexture = nint.Zero;
        }
    }

    public bool CreateDeviceObjects()
    {
        return CreateFontsTexture();
    }

    public void DestroyDeviceObjects()
    {
        DestroyFontsTexture();
    }
}
