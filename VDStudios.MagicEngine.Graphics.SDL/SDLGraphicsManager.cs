using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Security.Principal;
using ImGuiNET;
using SDL2.NET;
using SDL2.NET.SDLImage;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Input;
using VDStudios.MagicEngine.SDL.Base;

namespace VDStudios.MagicEngine.Graphics.SDL;

/// <summary>
/// A <see cref="GraphicsManager{TGraphicsContext}"/> for SDL
/// </summary>
/// <inheritdoc/>
public class SDLGraphicsManager(Game game, WindowConfig? windowConfig = null) : SDLGraphicsManagerBase<SDLGraphicsContext>(game, windowConfig)
{
    private SDLGraphicsContext? context;

    /// <summary>
    /// The SDL Renderer managed by this <see cref="SDLGraphicsManager"/>
    /// </summary>
    public Renderer Renderer { get => renderer ?? throw new InvalidOperationException("Cannot get the Renderer of an SDLGraphicsManager that has not been launched"); private set => renderer = value; }
    private Renderer? renderer;

    /// <inheritdoc/>
    protected override SDLGraphicsContext FetchGraphicsContext()
    {
        Debug.Assert(renderer is not null, "Renderer is unexpectedly null");
        lock (Sync)
            return context ??= new(this);
    }

    /// <inheritdoc/>
    protected override void DisposeSDLResources()
    {
        if (renderer is not null)
            Renderer.Dispose();

        ReleaseWindow();

        renderer = null;
        IsRunning = false;
    }

    /// <inheritdoc/>
    protected override void BeforeRun()
    {
        Window.CreateWindowAndRenderer(Game.GameTitle, 800, 600, out var win, out var ren, configuration: WindowConfig);
        Window = win;
        Renderer = ren;
        ConfigureWindow();
    }

    internal record class ScreenshotRequest(Stream Output, ScreenshotImageFormat Format, int JpegQuality)
    {
        public readonly SemaphoreSlim Semaphore = new(0, 1);
        public Surface? Surface;
        public Task? UploadTask;

        public void FireUploadScreenshotTask()
        {
            UploadTask = Task.Run(() =>
            {
                try
                {
                    Debug.Assert(Surface is not null, "Surface was unexpectedly null at the time of uploading screenshot");
                    using var rwops = RWops.CreateFromStream(Output);
                    if (Format is ScreenshotImageFormat.BMP)
                        Surface.SaveBMP(rwops);
                    else if (Format is ScreenshotImageFormat.PNG)
                        Surface.SavePNG(rwops);
                    else if (Format is ScreenshotImageFormat.JPG)
                        Surface.SaveJPG(rwops, JpegQuality);
                }
                finally
                {
                    Surface = null;
                    Semaphore.Release();
                }
            });
        }
    }

    private Surface CreateScreenshotSurface()
    {
        var winsize = WindowSize;
        var srf = new Surface(winsize.X, winsize.Y, 32, 0x00ff0000, 0x0000ff00, 0x000000ff, 0xff000000);
        Renderer.ReadPixels(SDL2.NET.PixelFormat.Unknown, srf.GetPixels(out _), srf.Pitch);
        return srf;
    }

    internal readonly HashSet<SDLFrameHook> framehooks = new();
    internal readonly Queue<ScreenshotRequest> screenshotRequests = new();

    /// <inheritdoc/>
    public override SDLFrameHook AttachFramehook()
    {
        Log.Verbose("Attaching new Framehook");
        SDLFrameHook fh;

        using var pxf = new PixelFormatData(Window.PixelFormat);
        fh = new(this, Log)
        {
            BytesPerPixel = (uint)pxf.BytesPerPixel,
            BitsPerPixel = (uint)pxf.BitsPerPixel
        };

        lock (framehooks)
            framehooks.Add(fh);
        Log.Information("Hooked new framehook {hook}", fh);
        return fh;
    }

    /// <inheritdoc/>
    public override async ValueTask TakeScreenshot(Stream output, ScreenshotImageFormat format, int jpegQuality = 100)
    {
        var req = new ScreenshotRequest(output, format, jpegQuality);
        lock (screenshotRequests)
            screenshotRequests.Enqueue(req);

        await req.Semaphore.WaitAsync();

        Debug.Assert(req.UploadTask is not null, "UploadTask was unexpectedly null after the semaphore release");
        await req.UploadTask;
    }

    /// <inheritdoc/>
    protected override void BeforeSubmitFrame()
    {
        Surface? srf = null;

        if (screenshotRequests.Count > 0)
            lock (screenshotRequests)
                if (screenshotRequests.Count > 0)
                {
                    srf ??= CreateScreenshotSurface(); // We let the GC finalize this surface

                    while (screenshotRequests.TryDequeue(out var req)) 
                    {
                        Debug.Assert(srf is not null, "Screenshot Surface was not properly created");
                        req.Surface = srf;
                        req.FireUploadScreenshotTask();
                    }
                }

        if (framehooks.Count > 0)
            lock (framehooks)
                if (framehooks.Count > 0)
                {
                    foreach (var fh in framehooks)
                        if (fh.FrameSkipTimer.HasClocked)
                        {
                            fh.FrameSkipTimer.Restart();
                            fh.frameQueue.Enqueue(srf ??= CreateScreenshotSurface());
                        }
                }
    }

    /// <inheritdoc/>
    protected override void SetupGraphicsManager() { }
}
