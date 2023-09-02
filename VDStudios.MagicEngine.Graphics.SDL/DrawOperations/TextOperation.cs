using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SDL2.NET;
using SDL2.NET.SDLFont;
using VDStudios.MagicEngine.SDL.Demo;

namespace VDStudios.MagicEngine.Graphics.SDL.DrawOperations;

/// <summary>
/// An operation that renders text 
/// </summary>
public class TextOperation : DrawOperation<SDLGraphicsContext>
{
    private readonly record struct TextRenderCacheKey(byte Mode, int Size, string Text, RGBAColor Color, RGBAColor Foreground);
    private Texture? texture;
    private Surface? surface;
    private Texture? txtbf;

    private TTFont __font;

    /// <summary>
    /// The current <see cref="TTFont"/> for this <see cref="TextOperation"/>
    /// </summary>
    /// <remarks>
    /// Changing this will result in the current text being re-rendered
    /// </remarks>
    public TTFont Font
    {
        get => __font;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (((IHandle)value).Handle == 0)
                throw new ArgumentException("The value object points to a null native pointer", nameof(value));
            if (((IHandle)value).Handle != ((IHandle)__font).Handle)
            {
                __font = value;
                texture = null;
                NotifyPendingGPUUpdate();
            }
        }
    }

    /// <summary>
    /// The Text that is currently set to be rendered by this <see cref="TextOperation"/>
    /// </summary>
    public string Text => CurrentKey.Text;

    private TextRenderCacheKey CurrentKey;

    /// <summary>
    /// Creates a new object of type <see cref="TextOperation"/>
    /// </summary>
    /// <param name="font">The <see cref="TTFont"/> object from which to render the text</param>
    /// <param name="game">The <see cref="Game"/> this <see cref="TextOperation"/> belongs to</param>
    public TextOperation(TTFont font, Game game) : base(game)
    {
        __font = font ?? throw new ArgumentNullException(nameof(font));
    }

    /// <summary>
    /// Schedules the operation to render <paramref name="text"/> blended
    /// </summary>
    public void SetTextBlended(string text, RGBAColor color, int? size = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        var k = new TextRenderCacheKey(0, size ?? __font.Size, text, color, default);

        if (k != CurrentKey)
        {
            if (size is int s)
                __font.Size = s;
            CurrentKey = k;
            texture = null;
            NotifyPendingGPUUpdate();
        }
    }

    /// <summary>
    /// Schedules the operation to render <paramref name="text"/> shaded with <paramref name="background"/> and <paramref name="foreground"/>
    /// </summary>
    public void SetTextShaded(string text, RGBAColor foreground, RGBAColor background, int? size = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        var k = new TextRenderCacheKey(1, size ?? __font.Size, text, foreground, background);

        if (k != CurrentKey)
        {
            if (size is int s)
                __font.Size = s;
            CurrentKey = k;
            texture = null;
            NotifyPendingGPUUpdate();
        }
    }

    /// <summary>
    /// Schedules the operation to render <paramref name="text"/> solid
    /// </summary>
    public void SetTextSolid(string text, RGBAColor color, int? size = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        var k = new TextRenderCacheKey(2, size ?? __font.Size, text, color, default);

        if (k != CurrentKey)
        {
            if (size is int s)
                __font.Size = s;
            CurrentKey = k;
            texture = null;
            NotifyPendingGPUUpdate();
        }
    }

    /// <inheritdoc/>
    protected override ValueTask CreateResourcesAsync() => ValueTask.CompletedTask;

    /// <inheritdoc/>
    protected override void CreateGPUResources(SDLGraphicsContext context)
    {
        NotifyPendingGPUUpdate();
    }

    /// <inheritdoc/>
    protected override void Draw(TimeSpan delta, SDLGraphicsContext context, RenderTarget<SDLGraphicsContext> target)
    {
        if (Text is null) return;
        Debug.Assert(texture is not null || txtbf is not null, "Texture was unexpectedly null at the time of drawing");
        var t = texture ?? txtbf!; // The analyzer warns about this, but one of the references must not be null at this time, or the assertion would fail

        var dest = this.CreateDestinationRectangle(t.Size.ToVector2(), target.Transformation).ToRectangle();
        t.Render(null, dest);
    }

    /// <inheritdoc/>
    protected override void UpdateGPUState(SDLGraphicsContext context)
    {
        if (texture is null)
        {
            var k = CurrentKey;
            
            txtbf?.Dispose();
            txtbf = null;

            surface?.Dispose();
            surface = null;

            var (m, _, t, c1, c2) = k;
            if (t is null) return;
            Debug.Assert(m is >= 0 and <= 2, "Unknown mode");
            surface = m switch
            {
                0 => __font.RenderTextBlended(t, c1, EncodingType.Unicode),
                1 => __font.RenderTextShaded(t, c1, c2, EncodingType.Unicode),
                2 => __font.RenderTextSolid(t, c1, EncodingType.Unicode),
                _ => throw new InvalidOperationException()
            };
            texture = new Texture(context.Renderer, surface);
            txtbf = texture;
        }
    }
}
