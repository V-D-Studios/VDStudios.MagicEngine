using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpAvi.Output;
using VDStudios.MagicEngine.Graphics;

namespace VDStudios.MagicEngine.Extensions.VideoRecording;

public abstract class Recorder<TFrameHook> : DisposableGameObject
    where TFrameHook : FrameHook
{
    public GraphicsManager Manager { get; }
    private readonly Stream Output;
    private readonly bool DisposeOutputStream;

    private void AssertAllNullOrNoneNull()
        => Debug.Assert((Hook is not null && Writer is not null && VideoStream is not null) || (Hook is null && Writer is null && VideoStream is null), "Hook, Writer and VideoStream null desync");

    private TFrameHook? Hook;
    private AviWriter? Writer;
    private IAviVideoStream? VideoStream;

    protected bool GetWriter([NotNullWhen(true)] out AviWriter? writer, [NotNullWhen(true)] out TFrameHook? hook, [NotNullWhen(true)] out IAviVideoStream? videoStream)
    {
        lock (Sync)
        {
            AssertAllNullOrNoneNull();

            if (Hook is null)
            {
                writer = null;
                hook = null;
                videoStream = null;
                return false;
            }

            hook = Hook;
            writer = Writer!;
            videoStream = VideoStream!;
            return true;
        }
    }

    public Recorder(GraphicsManager manager, Stream output, bool disposeOutputStream = false)
        : base(manager.Game, "Graphics & Input", "Video Recording")
    {
        Manager = manager ?? throw new ArgumentNullException(nameof(manager));
        Output = output ?? throw new ArgumentNullException(nameof(output));
        DisposeOutputStream = disposeOutputStream;
    }

    public abstract ValueTask Update();

    protected virtual IAviVideoStream CreateVideoStream(AviWriter writer, TFrameHook hook)
    {
        var vs = writer.AddVideoStream();
        var size = Manager.WindowSize;
        vs.Height = size.Y;
        vs.Width = size.X; 
        return vs;
    }

    protected virtual AviWriter CreateAviWriter()
    {
        decimal fps = Manager.TryGetTargetFrameRate(out var tfr) ? (decimal)tfr.TotalSeconds : 1M / 30;
        return new AviWriter(Output, true)
        {
            FramesPerSecond = fps
        };
    }

    public bool Start()
    {
        ThrowIfDisposed();
        lock (Sync)
        {
            if (Hook is null)
                return false;

            Hook = Manager.AttachFramehook() as TFrameHook ?? throw new InvalidOperationException("The manager this recorder is recording did not return a compatible FrameHook");
            Writer = CreateAviWriter() ?? throw new InvalidOperationException("CreateAviWriter method for this recorder returned null");
            VideoStream = CreateVideoStream(Writer, Hook) ?? throw new InvalidOperationException("CreateVideoStream method for this recorder returned null");

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

            AssertAllNullOrNoneNull();

            Hook.Dispose();
            Hook = null;
            
            Writer!.Close();
            Writer = null;

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
