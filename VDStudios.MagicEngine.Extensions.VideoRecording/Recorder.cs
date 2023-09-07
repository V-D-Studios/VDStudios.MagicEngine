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

/// <summary>
/// A class that allows the recording of a <see cref="GraphicsManager"/>'s output into an AVI video using <see cref="SharpAvi"/>
/// </summary>
/// <typeparam name="TFrameHook">The type of <see cref="FrameHook"/> this Recorder will use</typeparam>
public abstract class Recorder<TFrameHook> : DisposableGameObject
    where TFrameHook : FrameHook
{
    /// <summary>
    /// The <see cref="GraphicsManager"/> whose output this <see cref="Recorder{TFrameHook}"/> will capture
    /// </summary>
    public GraphicsManager Manager { get; }

    private readonly Stream Output;
    private readonly bool DisposeOutputStream;

    private void AssertAllNullOrNoneNull()
        => Debug.Assert((Hook is not null && Writer is not null && VideoStream is not null) || (Hook is null && Writer is null && VideoStream is null), "Hook, Writer and VideoStream null desync");

    private TFrameHook? Hook;
    private AviWriter? Writer;
    private IAviVideoStream? VideoStream;

    /// <summary>
    /// Obtains the <see cref="AviWriter"/>, <typeparamref name="TFrameHook"/> and <see cref="IAviVideoStream"/> in this recorder if it's currently started via <see cref="Start"/>
    /// </summary>
    /// <returns><see langword="true"/> if this <see cref="Recorder{TFrameHook}"/> is started and all parameters have values. <see langword="false"/> otherwise</returns>
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

    /// <summary>
    /// Creates a new object of type <see cref="Recorder{TFrameHook}"/>
    /// </summary>
    /// <param name="manager">The manager whose output this <see cref="Recorder{TFrameHook}"/> will capture</param>
    /// <param name="output">The output <see cref="Stream"/> of this <see cref="Recorder{TFrameHook}"/></param>
    /// <param name="disposeOutputStream">If <see langword="true"/>, then <paramref name="output"/> will be disposed of when this <see cref="Recorder{TFrameHook}"/> is disposed</param>
    /// <exception cref="ArgumentNullException"></exception>
    public Recorder(GraphicsManager manager, Stream output, bool disposeOutputStream = false)
        : base(manager.Game, "Graphics & Input", "Video Recording")
    {
        Manager = manager ?? throw new ArgumentNullException(nameof(manager));
        Output = output ?? throw new ArgumentNullException(nameof(output));
        DisposeOutputStream = disposeOutputStream;
    }

    /// <summary>
    /// Updates the <see cref="Recorder{TFrameHook}"/>, letting it capture frames
    /// </summary>
    public abstract ValueTask Update();

    /// <summary>
    /// Creates and configures the <see cref="IAviVideoStream"/> for this <see cref="Recorder{TFrameHook}"/>
    /// </summary>
    /// <param name="hook">The hook of this <see cref="Recorder{TFrameHook}"/></param>
    /// <param name="writer">The writer created by <see cref="CreateAviWriter"/></param>
    /// <remarks>
    /// Called after <see cref="CreateAviWriter"/>
    /// </remarks>
    protected virtual IAviVideoStream CreateVideoStream(AviWriter writer, TFrameHook hook)
    {
        var vs = writer.AddVideoStream();
        var size = Manager.WindowSize;
        vs.Height = size.Y;
        vs.Width = size.X; 
        return vs;
    }

    /// <summary>
    /// Creates and configures the <see cref="AviWriter"/> for this <see cref="Recorder{TFrameHook}"/>
    /// </summary>
    /// <remarks>
    /// Called before <see cref="CreateVideoStream(AviWriter, TFrameHook)"/>
    /// </remarks>
    /// <returns></returns>
    protected virtual AviWriter CreateAviWriter()
    {
        decimal fps = Manager.TryGetTargetFrameRate(out var tfr) ? 1M / (decimal)tfr.TotalSeconds : 30;
        return new AviWriter(Output, true)
        {
            FramesPerSecond = fps,
            EmitIndex1 = true
        };
    }

    /// <summary>
    /// Starts recording
    /// </summary>
    /// <remarks>
    /// This method creates the recorder's resources: The <see cref="FrameHook"/>, the <see cref="AviWriter"/> and the <see cref="IAviVideoStream"/>
    /// </remarks>
    /// <returns>
    /// <see langword="true"/> If this <see cref="Recorder{TFrameHook}"/> was started. <see langword="false"/> if it was already started
    /// </returns>
    public bool Start()
    {
        ThrowIfDisposed();
        lock (Sync)
        {
            if (Hook is not null)
                return false;

            Hook = Manager.AttachFramehook() as TFrameHook ?? throw new InvalidOperationException("The manager this recorder is recording did not return a compatible FrameHook");
            Writer = CreateAviWriter() ?? throw new InvalidOperationException("CreateAviWriter method for this recorder returned null");
            VideoStream = CreateVideoStream(Writer, Hook) ?? throw new InvalidOperationException("CreateVideoStream method for this recorder returned null");

            return true;
        }
    }

    /// <summary>
    /// Stops recording
    /// </summary>
    /// <remarks>
    /// This method destroys the recorder's resources: The <see cref="FrameHook"/>, the <see cref="AviWriter"/> and the <see cref="IAviVideoStream"/>. But it does not dispose of <see cref="Output"/>
    /// </remarks>
    /// <returns>
    /// <see langword="true"/> If this <see cref="Recorder{TFrameHook}"/> was stopped. <see langword="false"/> if it was already stopped
    /// </returns>
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

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        Stop();

        if (DisposeOutputStream)
            Output.Dispose();

        base.Dispose(disposing);
    }
}
