using System.Runtime.InteropServices;
using SharpAvi;
using SharpAvi.Output;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Graphics.SDL;

namespace VDStudios.MagicEngine.Extensions.VideoRecording.SDL;

public class SDLRecorder : Recorder<SDLFrameHook>
{
    public SDLRecorder(SDLGraphicsManager manager, Stream output, bool disposeOutputStream = false) : base(manager, output, disposeOutputStream)
    {
    }

    protected override IAviVideoStream CreateVideoStream(AviWriter writer, SDLFrameHook hook)
    {
        var vs = base.CreateVideoStream(writer, hook);
        vs.BitsPerPixel = (BitsPerPixel)hook.BitsPerPixel;
        return vs;
    }

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
