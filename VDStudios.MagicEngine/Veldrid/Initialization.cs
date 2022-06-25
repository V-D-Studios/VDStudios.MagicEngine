﻿using System;
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

namespace VDStudios.MagicEngine.Veldrid;
public static class Startup
{
    private static readonly object s_glVersionLock = new object();

    private static (int Major, int Minor)? s_maxSupportedGLVersion;

    private static (int Major, int Minor)? s_maxSupportedGLESVersion;

    private static void ThrowIfLessThan(int value, int comparison = 0)
    {
        if (value < comparison)
            throw new VeldridException(SDL_GetAndClearError());
    }

    public unsafe static GraphicsDevice CreateDefaultOpenGLGraphicsDevice(GraphicsDeviceOptions options, Window window, GraphicsBackend backend)
    {
        SDL_ClearError();

        IntPtr sdlHandle = ((IHandle)window).Handle;

        var winfo = window.SystemInfo;

        SetSDLGLContextAttributes(options, backend);
        IntPtr openGLContextHandle = SDL_GL_CreateContext(sdlHandle);
        if (openGLContextHandle == IntPtr.Zero)
            throw new VeldridException("Unable to create OpenGL Context: \"" + SDL_GetAndClearError() + "\". This may indicate that the system does not support the requested OpenGL profile, version, or Swapchain format.");

        ThrowIfLessThan(SDL_GL_GetAttribute(SDL_GLattr.SDL_GL_DEPTH_SIZE, out int num));
        ThrowIfLessThan(SDL_GL_GetAttribute(SDL_GLattr.SDL_GL_STENCIL_SIZE, out int num2));
        ThrowIfLessThan(SDL_GL_SetSwapInterval(options.SyncToVerticalBlank ? 1 : 0));

        OpenGLPlatformInfo platformInfo = new OpenGLPlatformInfo(openGLContextHandle, SDL_GL_GetProcAddress,

        context => ThrowIfLessThan(SDL_GL_MakeCurrent(sdlHandle, context)),
        () => SDL_GL_GetCurrentContext(),

        () => ThrowIfLessThan(SDL_GL_MakeCurrent(IntPtr.Zero, IntPtr.Zero)),
        SDL_GL_DeleteContext,

        () => SDL_GL_SwapWindow(sdlHandle),

        sync => ThrowIfLessThan(SDL_GL_SetSwapInterval(sync ? 1 : 0)));
        return GraphicsDevice.CreateOpenGL(options, platformInfo, (uint)window.Size.Width, (uint)window.Size.Height);
    }

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
}
