using Veldrid.ImageSharp;

namespace VDStudios.MagicEngine.Demo.ResourceExtensions;
public static class ImageTextures
{
    public static ImageSharpTexture RobinSpriteSheet => _robin.Value;

    //

    private static readonly Lazy<ImageSharpTexture> _robin
        = new(() => new ImageSharpTexture(Path.Combine("Resources", "Graphics", "Animations", "robin.png")));
}
