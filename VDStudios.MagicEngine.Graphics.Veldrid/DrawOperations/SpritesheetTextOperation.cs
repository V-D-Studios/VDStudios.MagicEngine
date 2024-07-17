//using System.Diagnostics;
//using SDL2.NET.SDLFont;

//namespace VDStudios.MagicEngine.Graphics.Veldrid.DrawOperations;

//public class SpritesheetTextOperation : TexturedShape2DRenderer, ITextOperation
//{
//    / <summary>
//    / Creates a new object of type<see cref="SpritesheetTextOperation"/>
//    / </summary>
//    / <param name = "game" > The < see cref="Game"/> this <see cref = "SpritesheetTextOperation" /> belongs to</param>
//    public SpritesheetTextOperation(Game game) : base(game)
//    {

//    }

//    private string? txt;

//    / <inheritdoc/>
//    public override string? Text
//    {
//        get => txt;
//        set
//        {
//            if (string.Equals(txt, value) is true)
//                return;

//            txt = value;
//            NotifyPendingGPUUpdate();
//        }
//    }

//    / <inheritdoc/>
//    protected override ValueTask CreateResourcesAsync() => ValueTask.CompletedTask;

//    / <inheritdoc/>
//    protected override void CreateGPUResources(VeldridGraphicsContext context)
//    {
//        NotifyPendingGPUUpdate();
//    }

//    / <inheritdoc/>
//    protected override void Draw(TimeSpan delta, VeldridGraphicsContext context, RenderTarget<VeldridGraphicsContext> target)
//    {
//        if (Text is null) return;
//        if (texture is null && txtbf is null)
//            ForceGPUUpdate(context);

//        Debug.Assert(texture is not null || txtbf is not null, "Texture was unexpectedly null at the time of drawing");
//        var t = texture ?? txtbf!; // The analyzer warns about this, but one of the references must not be null at this time, or the assertion would fail

//        var dest = this.CreateDestinationRectangle(t.Size.ToVector2(), target.Transformation).ToRectangle();
//        t.Render(null, dest);
//    }

//    / <inheritdoc/>
//    protected override void UpdateGPUState(VeldridGraphicsContext context)
//    {
//        if (texture is null)
//        {
//            var k = CurrentKey;

//            txtbf?.Dispose();
//            txtbf = null;

//            surface?.Dispose();
//            surface = null;

//            var (m, _, t, c1, c2) = k;
//            if (t is null or "") return;

//            Debug.Assert(m is >= 0 and <= 2, "Unknown mode");
//            surface = m switch
//            {
//                0 => __font.RenderTextBlended(t, c1, EncodingType.Unicode),
//                1 => __font.RenderTextShaded(t, c1, c2, EncodingType.Unicode),
//                2 => __font.RenderTextSolid(t, c1, EncodingType.Unicode),
//                _ => throw new InvalidOperationException()
//            };
//            texture = new Texture(context.Renderer, surface);
//            txtbf = texture;
//        }
//    }
//}
