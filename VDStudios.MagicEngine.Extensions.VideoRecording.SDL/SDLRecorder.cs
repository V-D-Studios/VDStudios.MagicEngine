using System.Runtime.InteropServices;
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
        vs.BitsPerPixel = (BitsPerPixel)hook.BitsPerPixel;
        return vs;
    }

    /// <inheritdoc/>
    public override ValueTask Update()
        => new(Task.Run(async () =>
        {
            if (GetWriter(out var writer, out var hook, out var stream))
            {
                bool att = false;
                while (true)
                {
                    if (hook.NextFrame(out var surface) is false)
                    {
                        if (att)
                            break;
                        att = true;
                        await Task.Delay(15);
                    }
                    else
                    {
                        att = false;
                        stream.WriteFrame(false, surface.GetPixels(out var bypp));
                    }
                }
            }
        }));
}
