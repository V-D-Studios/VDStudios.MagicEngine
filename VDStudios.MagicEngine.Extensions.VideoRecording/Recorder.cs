using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDStudios.MagicEngine.Graphics;

namespace VDStudios.MagicEngine.Extensions.VideoRecording;

public abstract class Recorder<TFrameHook> : DisposableGameObject
    where TFrameHook : FrameHook
{
    public GraphicsManager Manager { get; }
    private readonly Stream Output;
    private readonly bool DisposeOutputStream;

    protected TFrameHook? Hook { get; private set; }

    public Recorder(GraphicsManager manager, Stream output, bool disposeOutputStream = false)
        : base(manager.Game, "Graphics & Input", "Video Recording")
    {
        Manager = manager ?? throw new ArgumentNullException(nameof(manager));
        Output = output ?? throw new ArgumentNullException(nameof(output));
        DisposeOutputStream = disposeOutputStream;
    }

    public abstract Task Update();

    public bool Start()
    {
        ThrowIfDisposed();
        lock (Sync)
        {
            if (Hook is null)
                return false;
            Hook = Manager.AttachFramehook() as TFrameHook ?? throw new InvalidOperationException("The manager this recorder is recording did not return a compatible FrameHook");
            return true;
        }
    }

    public bool Stop()
    {
        ThrowIfDisposed();
        lock (Sync)
        {
            if (Hook is null)
                return false;
            Hook.Dispose();
            Hook = null;
            return true;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (DisposeOutputStream)
            Output.Dispose();

        Stop();

        base.Dispose(disposing);
    }
}
