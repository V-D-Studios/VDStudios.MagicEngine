using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using SDL2.NET;
using Serilog;
using VDStudios.MagicEngine.Timing;

namespace VDStudios.MagicEngine.Graphics.SDL;

/// <summary>
/// A <see cref="FrameHook"/> that queues up <see cref="Surface"/>s with an <see cref="SDLGraphicsManager"/>'s frame data every frame
/// </summary>
/// <remarks>
/// Creates a new <see cref="SDLFrameHook"/> hooked to <paramref name="owner"/>
/// </remarks>
public class SDLFrameHook(SDLGraphicsManager owner, ILogger log) : FrameHook(owner)
{
    private readonly ILogger ManagerLog = log;

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

    internal GraphicsManagerFrameTimer FrameSkipTimer = new(owner, 0);

    internal readonly ConcurrentQueue<Surface> frameQueue = new();

    /// <summary>
    /// Checks to see if there's a new <see cref="Surface"/> available, and obtains it if so
    /// </summary>
    public bool NextFrame([NotNullWhen(true)] out Surface? frame)
    {
        if (IsDisposed)
        {
            frame = null;
            return false;
        }
        return frameQueue.TryDequeue(out frame);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        var owner = (SDLGraphicsManager)Owner;
        lock (owner)
            owner.framehooks.Remove(this);
        ManagerLog.Information("Removed Framehook {hook}", this);

        base.Dispose(disposing);
    }
}
