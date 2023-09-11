using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using SDL2.NET;
using SDL2.NET.SDLImage;
using VDStudios.MagicEngine.Input;
using VDStudios.MagicEngine.SDL.Base;
using Veldrid;
using Vulkan;

namespace VDStudios.MagicEngine.Graphics.Veldrid;

/// <summary>
/// A <see cref="GraphicsManager{TGraphicsContext}"/> for Veldrid
/// </summary>
public class VeldridGraphicsManager : SDLGraphicsManagerBase<VeldridGraphicsContext>
{
    ///// <summary>
    ///// The list of ImGUI Elements
    ///// </summary>
    //public ImGUIElementList ImGUIElements { get; } = new();
    
    //private readonly List<ImGUIElement> imGUIElementBuffer = new();
    
    //private readonly ImGuiController imGuiController;

    private readonly static object ImGuiSync = new();

    //private void UpdateImGuiInput(InputSnapshot snapshot)
    //{
    //    imGuiController.UpdateImGuiInput(snapshot);
    //}

    //private void RenderImGuiElements(TimeSpan delta)
    //{
    //    lock (ImGUIElements.sync)
    //    {
    //        imGUIElementBuffer.Clear();
    //        foreach (var el in ImGUIElements)
    //            if (el.IsActive)
    //                imGUIElementBuffer.Add(el);
    //    }

    //    lock (ImGuiSync)
    //    {
    //        imGuiController.InitializeSDLRenderer(Renderer);
    //        ImGui.NewFrame();
    //        imGuiController.NewFrame();

    //        for (int i = 0; i < imGUIElementBuffer.Count; i++)
    //            imGUIElementBuffer[i].SubmitUI(delta);

    //        ImGui.EndFrame();
    //        imGuiController.RenderDrawData();
    //    }
    //}

    /// <inheritdoc/>
    public override IntVector2 WindowSize { get; protected set; }

    private VeldridGraphicsContext? context;

    /// <summary>
    /// The <see cref="GraphicsDevice"/> for <see cref="Window"/>
    /// </summary>
    public GraphicsDevice GraphicsDevice { get => device ?? throw new InvalidOperationException("Cannot get the GraphicsDevices of an VeldridGraphicsManager that has not been launched"); private set => device = value; }
    private GraphicsDevice? device;
    
    /// <summary>
    /// <see cref="Window"/>'s configuration
    /// </summary>
    public WindowConfig WindowConfig { get; }

    /// <inheritdoc/>
    public VeldridGraphicsManager(Game game, WindowConfig? windowConfig = null) : base(game)
    {
        WindowConfig = windowConfig ?? WindowConfig.Default;
        //imGuiController = new(this);
        WindowView = Matrix4x4.Identity;
    }

    /// <inheritdoc/>
    protected override VeldridGraphicsContext FetchGraphicsContext()
    {
        Debug.Assert(IsWindowNull is false, "Window is unexpectedly null");
        lock (Sync)
            return context ??= new(this, GraphicsDevice);
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
        if (IsWindowNull is false)
        {
            var gd = device;
            Debug.Assert(gd is not null, "Despite Window not being null, GraphicsDevice is unexpectedly null");
            gd.Dispose();
        }

        ReleaseWindow();

        device = null;
        IsRunning = false;
    }

    /// <inheritdoc/>
    protected override InputSnapshotBuffer CreateNewEmptyInputSnapshotBuffer()
        => new VeldridInputSnapshotBuffer(this);

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

    internal readonly SemaphoreSlim WindowThreadLock = new(1, 1);
    private Task? WindowThread;

    /// <summary>
    /// This method is called automatically when <see cref="Window"/> changes its size
    /// </summary>
    /// <remarks>
    /// This method is called after the frame is unlocked
    /// </remarks>
    protected virtual void WindowSizeChanged(TimeSpan timestamp, Size newSize) { }

    /// <inheritdoc/>
    protected override void BeforeRun()
    {
        Window = new Window(Game.GameTitle, 800, 600, WindowConfig);
        ConfigureWindow();
        GraphicsDevice = Startup.CreateGraphicsDevice(Window);

        var (ww, wh) = Window.Size;
        GraphicsDevice.MainSwapchain.Resize((uint)ww, (uint)wh);
    }

    /// <inheritdoc/>
    protected override void DisposeRunningResources()
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
    protected override void BeforeFrameRelease()
    {
        //RenderImGuiElements(delta);
    }

    /// <inheritdoc/>
    protected override void BeforeWindowRelease()
    {
        Events.Update();
    }

    /// <inheritdoc/>
    protected override void AtFrameEnd()
    {
        //UpdateImGuiInput(FetchSnapshot());
    }

    /// <inheritdoc/>
    protected override void Launch()
    {
        FrameLock.Wait();
        WindowThread = Task.Run(VeldridRun);
        FrameLock.Wait();
        FrameLock.Release();
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

    #endregion

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        //lock (ImGuiSync)
        //    imGuiController.Shutdown();
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
        //.ReadPixels(SDL2.NET.PixelFormat.Unknown, srf.GetPixels(out _), srf.Pitch);
        return srf;
    }

    internal readonly HashSet<VeldridFrameHook> framehooks = new();
    internal readonly Queue<ScreenshotRequest> screenshotRequests = new();

    /// <inheritdoc/>
    public override VeldridFrameHook AttachFramehook()
    {
        Log.Verbose("Attaching new Framehook");
        VeldridFrameHook fh;

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
    protected override void ValidateRenderTarget(RenderTarget<VeldridGraphicsContext> target)
    {
        if (target is not VeldridRenderTarget)
            throw new ArgumentException("Render Targets registered in a VeldridGraphicsManager must inherit from VeldridRenderTarget", nameof(target));
    }

    /// <inheritdoc/>
    protected override void WindowSizeChangedFrameLocked(IntVector2 oldSize, IntVector2 newSize)
    {
        var (ww, wh) = newSize;
        WindowView = Matrix4x4.CreateScale(wh / (float)ww, 1, 1);
        GraphicsDevice.MainSwapchain.Resize((uint)ww, (uint)wh);
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

    /// <summary>s
    /// Replaces <see cref="GraphicsManager{TSelf}.Run()"/>
    /// </summary>
    protected void VeldridRun()
    {
        try
        {
            BeforeRun();
            IsRunning = true;
        }
        finally
        {
            FrameLock.Release();
        }

        try
        {
            bool nodopwarn = false;
            var framelock = FrameLock;
            var drawlock = DrawLock;
            var winlock = WindowShownLock;
            var guilock = GUILock;

            var isRunning = IsRunningCheck;
            var isNotRendering = IsNotRenderingCheck;

            var sw = new Stopwatch();
            var drawqueue = CreateDrawQueue();
            var rtTaskBuffer = new List<Task>();
            var renderTargetBuffer = new List<(uint Level, RenderTargetList<VeldridGraphicsContext> Targets)>();
            TimeSpan delta = default;

            var (ww, wh) = WindowSize;

            Log?.Information("Entering main rendering loop");
            while (IsRunning) // Running Loop
            {
                WaitLockDisposable winlockwaiter;

                while (WaitOn(winlock, condition: isNotRendering, out winlockwaiter, syncWait: 500) is false)
                {
                    while (!IsRendering)
                    {
                        UpdateWhileWaitingOnWindow();
                        continue;
                    }
                }

                using (winlockwaiter)
                {
                    ClearSnapshot();
                    RunningWindowLocked(delta);

                    if (!WaitOn(framelock, condition: isRunning, out var framelockwaiter)) break; // Frame Render
                    using (framelockwaiter)
                    {
                        RunningFramelocked(delta);

                        var context = GetGraphicsContext();

                        context.Update(delta);
                        context.BeginFrame();

                        if (!WaitOn(drawlock, condition: isRunning, out var drawlockwaiter)) break; // Frame Render Stage 1: General Drawing
                        using (drawlockwaiter)
                        {
                            BufferRenderTargets(renderTargetBuffer);
                            if (renderTargetBuffer.Count <= 0)
                                Log?.Debug("This GraphicsManager has no render levels nor render targets");
                            else
                            {
                                for (int i = 0; i < renderTargetBuffer.Count; i++)
                                {
                                    drawqueue.Clear();
                                    var (renderLevel, renderTargets) = renderTargetBuffer[i];
                                    if (renderTargets.Count <= 0)
                                        Log?.Debug("The RenderTargetList for RenderLevel {level} has no render targets", renderLevel);

                                    if (Game.CurrentScene.GetDrawOperationManager<VeldridGraphicsContext>(out var dopm) is false)
                                    {
                                        if (nodopwarn is false)
                                        {
                                            Log?.Warning($"The current Scene does not have a DrawOperationManager for context of type {(typeof(VeldridGraphicsContext))}");
                                            nodopwarn = true;
                                        }
                                    }
                                    else
                                    {
                                        nodopwarn = false;
                                        foreach (var dop in dopm.GetDrawOperations(this, renderLevel))
                                            dopm.AddToDrawQueue(drawqueue, dop);

                                        if (drawqueue.Count <= 0)
                                            Log?.Verbose("No draw operations were registered for RenderLevel {level}", renderLevel);
                                        else
                                        {
                                            lock (renderTargets)
                                            {
                                                var enume = renderTargets.GetEnumerator();
                                                while (enume.MoveNext())
                                                {
                                                    context.AssignCommandList((VeldridRenderTarget)enume.Current);
                                                    enume.Current.BeginFrame(delta, context);
                                                }

                                                while (drawqueue.TryDequeue(out var dop))
                                                {
                                                    enume.Reset();
                                                    while (enume.MoveNext())
                                                        enume.Current.RenderDrawOperation(delta, context, dop);
                                                }

                                                enume.Reset();
                                                while (enume.MoveNext())
                                                {
                                                    context.RemoveCommandList((VeldridRenderTarget)enume.Current);
                                                    enume.Current.EndFrame(context);
                                                }
                                            }
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

                        BeforeSubmitFrame();

                        context.EndAndSubmitFrame();

                        BeforeFrameRelease();
                    }

                    BeforeWindowRelease();
                }

                SubmitInput(FetchSnapshot());

                AtFrameEnd();

                // Code that does not require any resources and is not bothered if resources are suddenly released

                CountFrame();
                delta = sw.Elapsed;
                DeltaAverageKeeper.Push(1000 / (sw.ElapsedMilliseconds + 0.0000001f));

                sw.Restart();
            }

            Debug.Assert(IsRunning is false, "The main rendering loop broke even though the GraphicsManager is still supposed to be running");
            Log?.Information("Exiting main rendering loop and disposing");
        }
        finally
        {
            DisposeRunningResources();
        }
    }
}
