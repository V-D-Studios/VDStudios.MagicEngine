using SDL2.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.Demo;
public static class StaticResources
{
    public static class Graphics
    {
        public static class Animations
        {
            public static Texture Robin => _robin.Value;

            //

            private static Lazy<Texture> _robin
                = new(() => SDL2.NET.SDLImage.Image.LoadTexture(Game.Instance.MainRenderer, Path.Combine("Resources", "Graphics", "Animations", "robin.png")));
        }
    }
}
