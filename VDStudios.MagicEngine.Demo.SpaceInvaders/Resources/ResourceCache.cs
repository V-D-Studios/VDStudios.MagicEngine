using System;
using System.Collections.Generic;
using System.Linq;
using Veldrid;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Veldrid.ImageSharp;

namespace VDStudios.MagicEngine.Demo.SpaceInvaders.Resources;

/// <summary>
/// Represents static references to loaded resources
/// </summary>
public static class ResourceCache
{
    public sealed class CachedResource<TResource>
    {
        private readonly ConcurrentDictionary<GraphicsManager, TResource> ResourceDict = new();
        private readonly Func<GraphicsManager, TResource> Factory;

        internal CachedResource(Func<GraphicsManager, TResource> factory)
        {
            Factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public TResource GetResource(GraphicsManager manager)
            => ResourceDict.GetOrAdd(manager, Factory);
    }

    public static CachedResource<Texture> EntitySpritesheet { get; } = new(gm =>
    {
        var gd = gm.Device;
        var rf = gd.ResourceFactory;
        return new ImageSharpTexture("Resources/sprites.png").CreateDeviceTexture(gd, rf);
    });
}
