using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    private readonly TTFont FontData;
    private Texture? texture;
    private Texture? txtbf;

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
        FontData = font ?? throw new ArgumentNullException(nameof(font));
    }

    /// <summary>
    /// Schedules the operation to render <paramref name="text"/> blended
    /// </summary>
    public void SetTextBlended(string text, RGBAColor color, int? size = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        var k = new TextRenderCacheKey(0, size ?? FontData.Size, text, color, default);

        if (k != CurrentKey)
        {
            CurrentKey = k;
            if (size is int s)
                FontData.Size = s;
            texture = null;
        }
    }

    /// <summary>
    /// Schedules the operation to render <paramref name="text"/> shaded with <paramref name="background"/> and <paramref name="foreground"/>
    /// </summary>
    public void SetTextShaded(string text, RGBAColor foreground, RGBAColor background, int? size = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        var k = new TextRenderCacheKey(1, size ?? FontData.Size, text, foreground, background);

        if (k != CurrentKey)
        {
            CurrentKey = k;
            if (size is int s)
                FontData.Size = s;
            texture = null;
        }
    }

    /// <summary>
    /// Schedules the operation to render <paramref name="text"/> solid
    /// </summary>
    public void SetTextSolid(string text, RGBAColor color, int? size = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        var k = new TextRenderCacheKey(2, size ?? FontData.Size, text, color, default);

        if (k != CurrentKey)
        {
            CurrentKey = k;
            if (size is int s)
                FontData.Size = s;
            texture = null;
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
        Debug.Assert(texture is not null, "Texture was unexpectedly null at the time of drawing");

        Transform(scale: new Vector3(4, 4, 1));

        var dest = this.CreateDestinationRectangle(texture.Size.ToVector2(), target.Transformation).ToRectangle();
        texture.Render(null, dest);
    }

    /// <inheritdoc/>
    protected override void UpdateGPUState(SDLGraphicsContext context)
    {
        if (texture is null)
        {
            var k = CurrentKey;
            txtbf?.Dispose();
            var (m, _, t, c1, c2) = k;
            Debug.Assert(m is >= 0 and <= 2, "Unknown mode");
            using var srf = m switch
            {
                0 => FontData.RenderTextBlended(t, c1, EncodingType.UTF8),
                1 => FontData.RenderTextShaded(t, c1, c2, EncodingType.UTF8),
                2 => FontData.RenderTextSolid(t, c1, EncodingType.UTF8),
                _ => throw new InvalidOperationException()
            };
            texture = new Texture(context.Renderer, srf);
            txtbf = texture;
        }
    }
}
