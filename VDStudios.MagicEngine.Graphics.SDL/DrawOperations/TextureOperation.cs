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
    /// Configuration info for a <see cref="TextureOperation"/> outline
    /// </summary>
    /// <param name="Color">The color of the Outline</param>
    /// <param name="Width">The width of the outline</param>
    /// <param name="Fill">Whether the outline should be filled geometry or not</param>
    /// <param name="TransformWithRenderTarget">Whether or not to permit the outline to be transformed along with the Render Target</param>
    public readonly record struct OutlineConfig(RGBAColor Color, int Width, bool Fill, bool TransformWithRenderTarget = true)
    {
        /// <summary>
        /// The width of the outline
        /// </summary>
        public int Width { get; } = Width >= 0 ? Width : throw new ArgumentException("Width cannot be less than 0", nameof(Width));
    }

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
    public OutlineConfig? TextureEdgeOutline { get; set; }

    /// <summary>
    /// The color of the outline of the texture's center point. Set to <see langword="null"/> to not render the outline
    /// </summary>
    public OutlineConfig? TextureCenterOutline { get; set; }

    /// <summary>
    /// The color of the outline of the texture's top left corner. Set to <see langword="null"/> to not render the outline
    /// </summary>
    public OutlineConfig? TextureTopLeftOutline { get; set; }

    /// <summary>
    /// The color of the outline of the texture's top right corner. Set to <see langword="null"/> to not render the outline
    /// </summary>
    public OutlineConfig? TextureTopRightOutline { get; set; }

    /// <summary>
    /// The color of the outline of the texture's bottom right corner. Set to <see langword="null"/> to not render the outline
    /// </summary>
    public OutlineConfig? TextureBottomRightOutline { get; set; }

    /// <summary>
    /// The color of the outline of the texture's bottom left corner. Set to <see langword="null"/> to not render the outline
    /// </summary>
    public OutlineConfig? TextureBottomLeftOutline { get; set; }

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

        if (TextureEdgeOutline is OutlineConfig edgeOutline && edgeOutline.Width > 0)
        {
            if (edgeOutline.Width == 1)
                context.Renderer.DrawRectangle(dest, edgeOutline.Color);
            else
            {
                int xedge = edgeOutline.Width, yedge = edgeOutline.Width;
                if (edgeOutline.TransformWithRenderTarget)
                {
                    xedge = (int)(xedge * target.Transformation.ViewScale.X);
                    yedge = (int)(yedge * target.Transformation.ViewScale.Y);
                }

                Span<Rectangle> rects = stackalloc Rectangle[4];
                FillEdgeOutlineRects(rects, dest, xedge, yedge);
                if (edgeOutline.Fill)
                    context.Renderer.FillRectangles(rects, edgeOutline.Color);
                else
                    context.Renderer.DrawRectangles(rects, edgeOutline.Color);
            }
        }

        if (TextureCenterOutline is OutlineConfig CenterOutline && CenterOutline.Width > 0)
            RenderOutlinePoint(CenterOutline, dest.Center, target.Transformation.ViewScale, context);

        if (TextureTopLeftOutline is OutlineConfig TopLeftOutline && TopLeftOutline.Width > 0)
            RenderOutlinePoint(TopLeftOutline, dest.TopLeft, target.Transformation.ViewScale, context);

        if (TextureTopRightOutline is OutlineConfig TopRightOutline && TopRightOutline.Width > 0)
            RenderOutlinePoint(TopRightOutline, dest.TopRight, target.Transformation.ViewScale, context);

        if (TextureBottomLeftOutline is OutlineConfig BottomLeftOutline && BottomLeftOutline.Width > 0)
            RenderOutlinePoint(BottomLeftOutline, dest.BottomLeft, target.Transformation.ViewScale, context);

        if (TextureBottomRightOutline is OutlineConfig BottomRightOutline && BottomRightOutline.Width > 0)
            RenderOutlinePoint(BottomRightOutline, dest.BottomRight, target.Transformation.ViewScale, context);
    }

    private static void RenderOutlinePoint(OutlineConfig config, Point reference, Vector3 scale, SDLGraphicsContext context)
    {
        if (config.Width == 1)
            context.Renderer.DrawPoint(reference, config.Color);
        else
        {
            int xedge = config.Width, yedge = config.Width;
            if (config.TransformWithRenderTarget)
            {
                xedge = (int)(xedge * scale.X);
                yedge = (int)(yedge * scale.Y);
            }

            var rect = new Rectangle(xedge, yedge, reference.X - xedge / 2, reference.Y - yedge / 2);
            if (config.Fill)
                context.Renderer.FillRectangle(rect, config.Color);
            else
                context.Renderer.DrawRectangle(rect, config.Color);
        }
    }

    private static void FillEdgeOutlineRects(Span<Rectangle> rects, Rectangle dest, int xedge, int yedge)
    {
        Debug.Assert(rects.Length == 4);

        // top -
        rects[0] = new Rectangle(dest.Width + xedge, yedge, dest.X - xedge, dest.Y - yedge);
        // right |
        rects[1] = new Rectangle(xedge, dest.Height + yedge, dest.Width + dest.X, dest.Y - yedge);
        // bottom -
        rects[2] = new Rectangle(dest.Width + xedge, yedge, dest.X, dest.Height + dest.Y);
        // left |
        rects[3] = new Rectangle(xedge, dest.Height + yedge, dest.X - xedge, dest.Y);
    }

    /// <inheritdoc/>
    protected override void UpdateGPUState(SDLGraphicsContext context)
    {
    }
}