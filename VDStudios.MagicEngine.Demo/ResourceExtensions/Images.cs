using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid.ImageSharp;

namespace VDStudios.MagicEngine.Demo.ResourceExtensions;
public static class ImageTextures
{
    public static ImageSharpTexture RobinSpriteSheet => _robin.Value;

    //

    private static Lazy<ImageSharpTexture> _robin
        = new(() => new ImageSharpTexture(Path.Combine("Resources", "Graphics", "Animations", "robin.png")));
}
