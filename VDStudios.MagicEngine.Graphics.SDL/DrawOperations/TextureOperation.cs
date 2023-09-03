using System.Diagnostics;
using System.Numerics;
using SDL2.NET;
using SDL2.NET.SDLImage;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Graphics.SDL;
using VDStudios.MagicEngine.SDL.Demo;

namespace VDStudios.MagicEngine.Graphics.SDL.DrawOperations;

/// <summary>
/// A <see cref="DrawOperation{TGraphicsContext}"/> that renders a single texture
/// </summary>
public class TextureOperation : DrawOperation<SDLGraphicsContext>
{
    /// <summary>
    /// Instances a new object of class <see cref="TextureOperation"/>
    /// </summary>
    /// <param name="game">The <see cref="Game"/> this <see cref="DrawOperation{TGraphicsContext}"/> belongs to</param>
    /// <param name="textureFactory">A delegate that, when invoked, will produce the <see cref="Texture"/> to be rendered</param>
    /// <param name="view">The first value for <see cref="View"/>. If null, it will be set to the <see cref="Texture"/>'s size at position 0,0 upon loading the <see cref="Texture"/></param>
    public TextureOperation(Game game, Func<SDLGraphicsContext, Texture> textureFactory, Rectangle? view = null) : base(game)
    {
        TextureFactory = textureFactory ?? throw new ArgumentNullException(nameof(textureFactory));
        View = view ?? View;
    }

    /// <summary>
    /// The color of the outline of the target rectangle where the texture will be rendered. Set to <see langword="null"/> to not render the outline
    /// </summary>
    public RGBAColor? TextureOutlineColor { get; set; }

    private Texture? texture;
    private readonly Func<SDLGraphicsContext, Texture> TextureFactory;

    /// <summary>
    /// Represents the current view of the <see cref="Texture"/>, that is, the portion of the <see cref="Texture"/> that will be rendered
    /// </summary>
    public Rectangle View { get; set; }

    /// <inheritdoc/>
    protected override ValueTask CreateResourcesAsync() => ValueTask.CompletedTask;

    /// <inheritdoc/>
    protected override void CreateGPUResources(SDLGraphicsContext context)
    {
        texture = TextureFactory(context);
        if (View == default)
            View = new Rectangle(texture.Size, default);

        ColorTransformationChanged += PlayerRenderer_ColorTransformationChanged;
    }

    private void PlayerRenderer_ColorTransformationChanged(DrawOperation<SDLGraphicsContext> drawOperation, TimeSpan timestamp)
    {
        if (texture is not null)
            this.ApplyColor(texture);
    }

    /// <inheritdoc/>
    protected override void Draw(TimeSpan delta, SDLGraphicsContext context, RenderTarget<SDLGraphicsContext> target)
    {
        Debug.Assert(texture is not null);
        var dest = this.CreateDestinationRectangle(View.Size.ToVector2(), target.Transformation).ToRectangle();
        texture.Render(View, dest);
        if (TextureOutlineColor is RGBAColor color) 
            context.Renderer.DrawRectangle(dest, color);
    }

    /// <inheritdoc/>
    protected override void UpdateGPUState(SDLGraphicsContext context)
    {
    }
}