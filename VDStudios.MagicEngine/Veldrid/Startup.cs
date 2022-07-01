using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid.OpenGL;
using Veldrid;
using SDL2.NET;
using static SDL2.Bindings.SDL;
using SDL2.Bindings;
using PixelFormat = Veldrid.PixelFormat;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace VDStudios.MagicEngine.Veldrid;

/// <summary>
/// Contains utilities to be used to procure Veldrid's startup
/// </summary>
/// <remarks>
/// Almost identical to Veldrid.StartupUtilities, with a couple of minor optimizations and tweaks, as well as ported to work with SDL2.NET
/// </remarks>
public static class Startup
{
    private static readonly object s_glVersionLock = new();

    private static (int Major, int Minor)? s_maxSupportedGLVersion;

    private static (int Major, int Minor)? s_maxSupportedGLESVersion;

    private static void ThrowIfLessThan(int value, int comparison = 0)
    {
        if (value < comparison)
            throw new VeldridException(SDL_GetAndClearError());
    }

    #region Vulkan

    public static GraphicsDevice CreateVulkanGraphicsDevice(GraphicsDeviceOptions options, Window window)
    {
        return CreateVulkanGraphicsDevice(options, window, colorSrgb: false);
    }

    public static GraphicsDevice CreateVulkanGraphicsDevice(GraphicsDeviceOptions options, Window window, bool colorSrgb)
    {
        var (width, height) = window.Size;
        return GraphicsDevice.CreateVulkan(swapchainDescription: new SwapchainDescription(GetSwapchainSource(window), (uint)width, (uint)height, options.SwapchainDepthFormat, options.SyncToVerticalBlank, colorSrgb), options: options);
    }

    #endregion

    #region DirectX

    public static GraphicsDevice CreateDefaultD3D11GraphicsDevice(GraphicsDeviceOptions options, Window window)
    {
        var (width, height) = window.Size;
        return GraphicsDevice.CreateD3D11(swapchainDescription: new SwapchainDescription(GetSwapchainSource(window), (uint)width, (uint)height, options.SwapchainDepthFormat, options.SyncToVerticalBlank, options.SwapchainSrgbFormat), options: options);
    }

    #endregion

    #region OpenGL

    /// <summary>
    /// Creates an OpenGL-based <see cref="GraphicsDevice"/> that is tied to <paramref name="window"/>
    /// </summary>
    /// <param name="options">Settings regarding the <see cref="GraphicsDevice"/></param>
    /// <param name="window">The <see cref="Window"/> to tie the <see cref="GraphicsDevice"/> to</param>
    /// <param name="backend">The preferred <see cref="GraphicsBackend"/></param>
    /// <returns>The newly instanced <see cref="GraphicsDevice"/></returns>
    public static GraphicsDevice CreateDefaultOpenGLGraphicsDevice(GraphicsDeviceOptions options, Window window, GraphicsBackend backend)
    {
        SDL_ClearError();

        IntPtr sdlHandle = ((IHandle)window).Handle;

        var winfo = window.SystemInfo;

        SetSDLGLContextAttributes(options, backend);
        IntPtr openGLContextHandle = SDL_GL_CreateContext(sdlHandle);
        //if (INTERNAL_SDL_GetError() != IntPtr.Zero)
        //    throw new VeldridException("Unable to create OpenGL Context: \"" + SDL_GetAndClearError() + "\". This may indicate that the system does not support the requested OpenGL profile, version, or Swapchain format.");

        ThrowIfLessThan(SDL_GL_GetAttribute(SDL_GLattr.SDL_GL_DEPTH_SIZE, out int num));
        ThrowIfLessThan(SDL_GL_GetAttribute(SDL_GLattr.SDL_GL_STENCIL_SIZE, out int num2));
        ThrowIfLessThan(SDL_GL_SetSwapInterval(options.SyncToVerticalBlank ? 1 : 0));

        OpenGLPlatformInfo platformInfo = new(openGLContextHandle, SDL_GL_GetProcAddress,

        context => ThrowIfLessThan(SDL_GL_MakeCurrent(sdlHandle, context)),
        () => SDL_GL_GetCurrentContext(),

        () => ThrowIfLessThan(SDL_GL_MakeCurrent(IntPtr.Zero, IntPtr.Zero)),
        SDL_GL_DeleteContext,

        () => SDL_GL_SwapWindow(sdlHandle),

        sync => ThrowIfLessThan(SDL_GL_SetSwapInterval(sync ? 1 : 0)));
        return GraphicsDevice.CreateOpenGL(options, platformInfo, (uint)window.Size.Width, (uint)window.Size.Height);
    }

    /// <summary>
    /// Sets SDL attributes for OpenGL Contexts
    /// </summary>
    /// <param name="options">The GraphicsDevice options to use</param>
    /// <param name="backend">The specific Backend to use</param>
    public static void SetSDLGLContextAttributes(GraphicsDeviceOptions options, GraphicsBackend backend)
    {
        if (backend is not GraphicsBackend.OpenGL and not GraphicsBackend.OpenGLES)
            throw new VeldridException("backend must be OpenGL or OpenGLES.");

        SDL_GLcontext value = options.Debug ? ((SDL_GLcontext)3) : SDL_GLcontext.SDL_GL_CONTEXT_FORWARD_COMPATIBLE_FLAG;

        ThrowIfLessThan(SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_FLAGS, (int)value));
        var (value2, value3) = GetMaxGLVersion(backend == GraphicsBackend.OpenGLES);
        
        ThrowIfLessThan(SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, backend == GraphicsBackend.OpenGL ? 1 : 4));
        ThrowIfLessThan(SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, value2));
        ThrowIfLessThan(SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, value3));

        int value4 = 0;
        int value5 = 0;
        if (options.SwapchainDepthFormat.HasValue)
        {
            switch (options.SwapchainDepthFormat)
            {
                case PixelFormat.R16_UNorm:
                    value4 = 16;
                    break;
                case PixelFormat.D24_UNorm_S8_UInt:
                    value4 = 24;
                    value5 = 8;
                    break;
                case PixelFormat.R32_Float:
                    value4 = 32;
                    break;
                case PixelFormat.D32_Float_S8_UInt:
                    value4 = 32;
                    value5 = 8;
                    break;
                default:
                    throw new VeldridException("Invalid depth format: " + options.SwapchainDepthFormat.Value);
            }
        }

        ThrowIfLessThan(SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_DEPTH_SIZE, value4));
        ThrowIfLessThan(SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_STENCIL_SIZE, value5));
        ThrowIfLessThan(SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_FRAMEBUFFER_SRGB_CAPABLE, options.SwapchainSrgbFormat ? 1 : 0));
    }

    private static (int Major, int Minor) GetMaxGLVersion(bool gles)
    {
        lock (s_glVersionLock)
        {
            (int, int)? tuple = gles ? s_maxSupportedGLESVersion : s_maxSupportedGLVersion;
            if (!tuple.HasValue)
            {
                tuple = TestMaxVersion(gles);
                if (gles)
                {
                    s_maxSupportedGLESVersion = tuple;
                }
                else
                {
                    s_maxSupportedGLVersion = tuple;
                }
            }

            return tuple.Value;
        }
    }

    private static (int Major, int Minor) TestMaxVersion(bool gles)
    {
        Span<(int, int)> versions = !gles ? stackalloc (int, int)[5]
        {
            (4, 6),
            (4, 3),
            (4, 0),
            (3, 3),
            (3, 0)
        } : stackalloc (int, int)[2]
        {
                (3, 2),
                (3, 0)
        };

        for (int i = 0; i < versions.Length; i++)
        {
            var (num, num2) = versions[i];
            if (TestIndividualGLVersion(gles, num, num2))
            {
                return (num, num2);
            }
        }

        return (0, 0);
    }

    private static readonly WindowConfig TestWinConfig = new WindowConfig().OpenGL(true).Hide(true);
    private static bool TestIndividualGLVersion(bool gles, int major, int minor)
    {
        SDL_GLprofile value = !gles ? SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE : SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_ES;
        ThrowIfLessThan(SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, (int)value));
        ThrowIfLessThan(SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, major));
        ThrowIfLessThan(SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, minor));

        Window sdl2Window = new("", 1, 1, TestWinConfig);

        IntPtr context = SDL_GL_CreateContext(((IHandle)sdl2Window).Handle);

        SDL_GL_DeleteContext(context);

        sdl2Window.Dispose();

        return true;
    }

    #endregion

    #region Metal

    public static GraphicsDevice CreateMetalGraphicsDevice(GraphicsDeviceOptions options, Window window)
    {
        return CreateMetalGraphicsDevice(options, window, colorSrgb: false);
    }

    public static GraphicsDevice CreateMetalGraphicsDevice(GraphicsDeviceOptions options, Window window, bool colorSrgb)
    {
        var (width, height) = window.Size;
        return GraphicsDevice.CreateMetal(swapchainDescription: new SwapchainDescription(GetSwapchainSource(window), (uint)width, (uint)height, options.SwapchainDepthFormat, options.SyncToVerticalBlank, colorSrgb), options: options);
    }

    #endregion

    public static GraphicsDevice CreateGraphicsDevice(Window window)
    {
        return CreateGraphicsDevice(window, default, GetPlatformDefaultBackend());
    }

    public static GraphicsDevice CreateGraphicsDevice(Window window, GraphicsDeviceOptions options)
    {
        return CreateGraphicsDevice(window, options, GetPlatformDefaultBackend());
    }

    public static GraphicsDevice CreateGraphicsDevice(Window window, GraphicsBackend preferredBackend)
    {
        return CreateGraphicsDevice(window, default, preferredBackend);
    }

    public static GraphicsDevice CreateGraphicsDevice(Window window, GraphicsDeviceOptions options, GraphicsBackend preferredBackend)
    {
        return preferredBackend switch
        {
            GraphicsBackend.Direct3D11 => CreateDefaultD3D11GraphicsDevice(options, window),
            GraphicsBackend.Vulkan => CreateVulkanGraphicsDevice(options, window),
            GraphicsBackend.OpenGL => CreateDefaultOpenGLGraphicsDevice(options, window, preferredBackend),
            GraphicsBackend.Metal => CreateMetalGraphicsDevice(options, window),
            GraphicsBackend.OpenGLES => CreateDefaultOpenGLGraphicsDevice(options, window, preferredBackend),
            _ => throw new VeldridException("Invalid GraphicsBackend: " + preferredBackend),
        };
    }

    public static GraphicsBackend GetPlatformDefaultBackend() 
        => OperatingSystem.IsWindows()
           ? GraphicsBackend.Direct3D11
           : OperatingSystem.IsMacOS()
           ? !GraphicsDevice.IsBackendSupported(GraphicsBackend.Metal) ? GraphicsBackend.OpenGL : GraphicsBackend.Metal
           : !GraphicsDevice.IsBackendSupported(GraphicsBackend.Vulkan) ? GraphicsBackend.OpenGL : GraphicsBackend.Vulkan;

    public static SwapchainSource GetSwapchainSource(Window window)
    {
        var sysWMinfo = window.SystemInfo;

        switch (sysWMinfo.subsystem)
        {
            case SDL_SYSWM_TYPE.SDL_SYSWM_WINDOWS:
                {
                    var win32WindowInfo = sysWMinfo.info.win;
                    return SwapchainSource.CreateWin32(win32WindowInfo.window, win32WindowInfo.hinstance);
                }
            case SDL_SYSWM_TYPE.SDL_SYSWM_X11:
                {
                    var x11WindowInfo = sysWMinfo.info.x11;
                    return SwapchainSource.CreateXlib(x11WindowInfo.display, x11WindowInfo.window);
                }
            case SDL_SYSWM_TYPE.SDL_SYSWM_WAYLAND:
                {
                    var waylandWindowInfo = sysWMinfo.info.wl;
                    return SwapchainSource.CreateWayland(waylandWindowInfo.display, waylandWindowInfo.surface);
                }
            case SDL_SYSWM_TYPE.SDL_SYSWM_COCOA:
                return SwapchainSource.CreateNSWindow(sysWMinfo.info.cocoa.window);
            default:
                throw new PlatformNotSupportedException("Cannot create a SwapchainSource for " + sysWMinfo.subsystem.ToString() + ".");
        }
    }
}
