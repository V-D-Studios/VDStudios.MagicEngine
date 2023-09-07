using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDL2.NET;
using VDStudios.MagicEngine.Graphics.SDL;

namespace VDStudios.MagicEngine.SDL.Demo.Services;
public class TextureCache
{
    public sealed class TextureCacheEntry
    {
        private readonly ConcurrentDictionary<SDLGraphicsContext, Texture> cache = new();

        public Func<SDLGraphicsContext, Texture> Factory { get; }

        public void Clear()
        {
            cache.Clear();
        }

        public void Clear(SDLGraphicsContext c)
        {
            ArgumentNullException.ThrowIfNull(c);
            cache.Remove(c, out _);
        }

        public TextureCacheEntry(Func<SDLGraphicsContext, Texture> textureFactory)
        {
            ArgumentNullException.ThrowIfNull(textureFactory);
            Factory = c => cache.GetOrAdd(c, textureFactory);
        }
    }

    private readonly ConcurrentDictionary<string, TextureCacheEntry> cache = new();

    public TextureCacheEntry GetTexture(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return cache[name];
    }

    public void RegisterTexture(string name, Func<SDLGraphicsContext, Texture> textureFactory, out TextureCacheEntry entry)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(textureFactory);

        entry = cache.GetOrAdd(name, c => new TextureCacheEntry(textureFactory));
    }
}
