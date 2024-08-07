﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDL2.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using static SDL2.Bindings.SDL;
using static OpenGL.GL;
using SDL2.NET;
using glPixelFormat = OpenGL.GL.PixelFormat;

namespace VDStudios.MagicEngine.Graphics.SDL.ImGUI;

public static class ImGuiGL
{
    public static void SetGLAttributes()
    {
        SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_FLAGS, (int)SDL_GLcontext.SDL_GL_CONTEXT_FORWARD_COMPATIBLE_FLAG);
        SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE);
        SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, 3);
        SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, 2);

        SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE);
        SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_DOUBLEBUFFER, 1);
        SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_DEPTH_SIZE, 24);
        SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_ALPHA_SIZE, 8);
        SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_STENCIL_SIZE, 8);
    }

    public static (IntPtr, IntPtr) CreateWindowAndGLContext(string title, int width, int height, bool fullscreen = false, bool highDpi = false)
    {
        // initialize SDL and set a few defaults for the OpenGL context
        SDL_Init(SDL_INIT_VIDEO);
        SetGLAttributes();

        // create the window which should be able to have a valid OpenGL context and is resizable
        var flags = SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL_WindowFlags.SDL_WINDOW_RESIZABLE;
        if (fullscreen) flags |= SDL_WindowFlags.SDL_WINDOW_FULLSCREEN;
        if (highDpi) flags |= SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI;

        var window = SDL_CreateWindow(title, SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED, width, height, flags);
        var glContext = CreateGLContext(window);
        return (window, glContext);
    }

    static IntPtr CreateGLContext(IntPtr window)
    {
        var glContext = SDL_GL_CreateContext(window);
        if (glContext == IntPtr.Zero)
            throw new Exception("CouldNotCreateContext");

        SDL_GL_MakeCurrent(window, glContext);
        SDL_GL_SetSwapInterval(1);

        // initialize the screen to black as soon as possible
        glClearColor(0f, 0f, 0f, 1f);
        glClear(ClearBufferMask.ColorBufferBit);
        SDL_GL_SwapWindow(window);

        Console.WriteLine($"GL Version: {glGetString(StringName.Version)}");
        return glContext;
    }

    public static uint LoadTexture(IntPtr pixelData, int width, int height, glPixelFormat format = glPixelFormat.Rgba, PixelInternalFormat internalFormat = PixelInternalFormat.Rgba)
    {
        var textureId = GenTexture();
        glPixelStorei(PixelStoreParameter.UnpackAlignment, 1);
        glBindTexture(TextureTarget.Texture2D, textureId);
        glTexImage2D(TextureTarget.Texture2D, 0, internalFormat, width, height, 0, format, PixelType.UnsignedByte, pixelData);
        glTexParameteri(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, TextureParameter.Linear);
        glTexParameteri(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, TextureParameter.Linear);
        glBindTexture(TextureTarget.Texture2D, 0);
        return textureId;
    }

    public static void AddImGuiFeature(this SDLGraphicsManager manager)
    {
        manager.AddFeature(new ImGuiOpenGLFeature());
    }
}