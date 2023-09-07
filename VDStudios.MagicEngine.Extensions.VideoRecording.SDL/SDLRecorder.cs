using System.Buffers;
using System.Runtime.InteropServices;
using SDL2.NET;
using SDL2.NET.Utilities;
using SharpAvi;
using SharpAvi.Output;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Graphics.SDL;

namespace VDStudios.MagicEngine.Extensions.VideoRecording.SDL;

/// <inheritdoc/>
public class SDLRecorder : Recorder<SDLFrameHook>
{
    /// <summary>
    /// Creates a new object of type <see cref="Recorder{TFrameHook}"/>
    /// </summary>
    /// <param name="manager">The manager whose output this <see cref="Recorder{TFrameHook}"/> will capture</param>
    /// <param name="output">The output <see cref="Stream"/> of this <see cref="Recorder{TFrameHook}"/></param>
    /// <param name="disposeOutputStream">If <see langword="true"/>, then <paramref name="output"/> will be disposed of when this <see cref="Recorder{TFrameHook}"/> is disposed</param>
    /// <exception cref="ArgumentNullException"></exception>
    public SDLRecorder(SDLGraphicsManager manager, Stream output, bool disposeOutputStream = false) : base(manager, output, disposeOutputStream)
    {
    }

    /// <inheritdoc/>
    protected override IAviVideoStream CreateVideoStream(AviWriter writer, SDLFrameHook hook)
    {
        var vs = base.CreateVideoStream(writer, hook);
        vs.Codec = CodecIds.X264;
        return vs;
    }

    /// <inheritdoc/>
    public override ValueTask Update()
        => new(Task.Run(() =>
        {
            using var buffer = new MemoryStream();
            if (GetWriter(out var writer, out var hook, out var stream))
            {
                while (hook.NextFrame(out var surface))
                {
                    using var rwop = RWops.CreateFromStream(buffer);
                    surface.SaveBMP(rwop);

                    if (buffer.TryGetBuffer(out var b))
                        stream.WriteFrame(false, b);

                    buffer.Position = 0;
                }
            }
        }));
}
