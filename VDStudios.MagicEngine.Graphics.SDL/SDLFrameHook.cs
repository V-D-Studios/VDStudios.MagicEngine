using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using SDL2.NET;
using Serilog;

namespace VDStudios.MagicEngine.Graphics.SDL;

/// <summary>
/// A <see cref="FrameHook"/> that queues up <see cref="Surface"/>s with an <see cref="SDLGraphicsManager"/>'s frame data every frame
/// </summary>
public class SDLFrameHook : FrameHook
{
    private readonly ILogger ManagerLog;

    /// <summary>
    /// Creates a new <see cref="SDLFrameHook"/> hooked to <paramref name="owner"/>
    /// </summary>
    public SDLFrameHook(SDLGraphicsManager owner, ILogger log) : base(owner) 
    {
        FrameSkipTimer = new(owner, 0);
        ManagerLog = log;
    }

    /// <summary>
    /// The amount of frames to skip
    /// </summary>
    /// <remarks>
    /// If set to <c>2</c>, for example, two frames will be skipped and one will be queued, and so on.
    /// </remarks>
    public uint FrameSkip
    {
        get
        {
            ThrowIfDisposed();
            return FrameSkipTimer.Lapse;
        }

        set
        {
            ThrowIfDisposed();
            FrameSkipTimer = new(FrameSkipTimer, value);
        }
    }

    internal GraphicsManagerFrameTimer FrameSkipTimer;

    internal readonly ConcurrentQueue<Surface> frameQueue = new();

    /// <summary>
    /// Checks to see if there's a new <see cref="Surface"/> available, and obtains it if so
    /// </summary>
    public bool NextFrame([NotNullWhen(true)] out Surface? frame)
    {
        ThrowIfDisposed();
        return frameQueue.TryDequeue(out frame);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        var owner = ((SDLGraphicsManager)Owner);
        lock (owner)
            owner.framehooks.Remove(this);
        ManagerLog.Information("Removed Framehook {hook}", this);

        base.Dispose(disposing);
    }
}
