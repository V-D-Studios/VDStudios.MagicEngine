using SDL2.NET;
using SDL2.NET.Exceptions;
using VDStudios.MagicEngine.SDL.Base;
using static OpenGL.GL;
using static SDL2.Bindings.SDL;

namespace VDStudios.MagicEngine.Graphics.SDL;

#warning Consider binding SDL_gpu for this

public abstract class SDLOpenGLGraphicsManager : SDLGraphicsManagerBase<SDLGraphicsContext>
{
    public SDLOpenGLGraphicsManager(Game game, WindowConfig? windowConfig = null) : base(game, windowConfig)
    {
    }

    /// <summary>
    /// The pointer to the OpenGL context for SDL's Renderer
    /// </summary>
    public nint OpenGLContextPointer { get; private set; }

    /// <inheritdoc/>
    protected override void BeforeRun()
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

        var conf = WindowConfig ?? new WindowConfig().Clone();
        conf.Vulkan(false).Metal(false).OpenGL(true);

        if (SDL_CreateWindowAndRenderer(800, 600, (WindowConfig ?? WindowConfig.Default).GenerateFlags(), out nint w, out nint r) != 0)
            throw new SDLWindowCreationException(SDL_GetAndClearError());

        var glContext = SDL_GL_CreateContext(w);
        if (glContext == IntPtr.Zero)
            throw new SDLWindowCreationException("Could not create OpenGL Context");

        OpenGLContextPointer = glContext;

        SDL_GL_MakeCurrent(w, glContext);
        SDL_GL_SetSwapInterval(1);

        // initialize the screen to black as soon as possible
        glClearColor(0f, 0f, 0f, 1f);
        glClear(ClearBufferMask.ColorBufferBit);
        SDL_GL_SwapWindow(w);

        (WindowConfig ?? WindowConfig.Default).OpenGL(true);

        var win = new Window(w, Environment.CurrentManagedThreadId)
        {
            Title = Game.GameTitle
        };
        Window = win;

        ConfigureWindow();
    }
}
