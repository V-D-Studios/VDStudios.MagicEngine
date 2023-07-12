using Veldrid.ImageSharp;

namespace VDStudios.MagicEngine.Demo.ResourceExtensions;
public static class ImageTextures
{
    public static ImageSharpTexture RobinSpriteSheet => _robin.Value;

    public static ImageSharpTexture BoxyFont => _boxyFont.Value;

    //

    private static readonly Lazy<ImageSharpTexture> _robin
        = new(() => new ImageSharpTexture(Path.Combine("Resources", "Graphics", "Animations", "robin.png")));

    private static readonly Lazy<ImageSharpTexture> _boxyFont
        = new(() => new ImageSharpTexture(Path.Combine("Resources", "Graphics", "Fonts", "boxy_bold_font.png")));
}
