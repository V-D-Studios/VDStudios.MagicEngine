using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace VDStudios.MagicEngine.Graphics.Veldrid.Caching;

/// <summary>
/// A Cache containing graphics resources for a <see cref="VeldridGraphicsContext"/> that are to be created on-demand
/// </summary>
/// <typeparam name="TResource">The resource this cache maintains</typeparam>
public class GraphicsContextResourceFactoryCache<TResource>
    where TResource : class
{
    /// <summary>
    /// A resource entry for a <see cref="GraphicsContextResourceFactoryCache{TResource}"/>
    /// </summary>
    public sealed class ResourceCacheEntry
    {
        private readonly GraphicsContextResourceFactoryCache<TResource> OwnerCache;
        private readonly GraphicsResourceFactory<TResource> Factory;
        private TResource? resourceCache;

        /// <summary>
        /// The delegate used to obtain the resource
        /// </summary>
        public GraphicsResourceFactory<TResource> ResourceDelegate { get; }

        /// <summary>
        /// The resource maintained by this cache entry
        /// </summary>
        public TResource Resource => resourceCache ??= Factory(OwnerCache.ResourceOwner);

        /// <summary>
        /// Clears the cache for this <see cref="ResourceCacheEntry"/>, the next time <see cref="ResourceDelegate"/> is used it will be created again
        /// </summary>
        /// <remarks>
        /// If the resource implements <see cref="IDisposable"/>, it will be disposed
        /// </remarks>
        public void Clear()
        {
            if (resourceCache is IDisposable disposable)
                disposable.Dispose();
            resourceCache = null;
        }

        internal ResourceCacheEntry(GraphicsContextResourceFactoryCache<TResource> ownerCache, GraphicsResourceFactory<TResource> resourceFactory)
        {
            ArgumentNullException.ThrowIfNull(resourceFactory);
            ArgumentNullException.ThrowIfNull(ownerCache);
            OwnerCache = ownerCache;
            Factory = resourceFactory;
            ResourceDelegate = context => context == OwnerCache.ResourceOwner ? Resource : throw new ArgumentException("The passed Resource Owner does not own this resource cache", nameof(context));
        }
    }

    private readonly IVeldridGraphicsContextResources ResourceOwner;
    private readonly ConcurrentDictionary<string, ResourceCacheEntry> cache = new();

    internal GraphicsContextResourceFactoryCache(IVeldridGraphicsContextResources owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ResourceOwner = owner;
    }

    /// <summary>
    /// Removes the resource under <paramref name="name"/> from the cache
    /// </summary>
    /// <param name="name">The name of the resource</param>
    /// <param name="entry">The dropped entry. It has not been cleared</param>
    /// <returns><see langword="true"/> if the resource was found and removed and is contained in <paramref name="entry"/></returns>
    public bool RemoveResource(string name, [NotNullWhen(true)] out ResourceCacheEntry? entry)
        => cache.Remove(name, out entry);

    /// <summary>
    /// Obtains the resource under <paramref name="name"/>
    /// </summary>
    /// <param name="name">The name of the resource</param>
    public ResourceCacheEntry GetResource(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return cache.TryGetValue(name, out var entry) is false
            ? throw new ArgumentException($"Could not find a Resource under '{name}'", nameof(name))
            : entry;
    }

    /// <summary>
    /// Attempts to obtain an array of <typeparamref name="TResource"/> under <paramref name="name"/>
    /// </summary>
    /// <param name="entry">The resource entry, <see langword="null"/> if not found</param>
    /// <param name="name">The name of the resource</param>
    /// <returns><see langword="true"/> if the resource is found and <paramref name="entry"/> has it. <see langword="false"/> otherwise</returns>
    public bool TryGetResource(string name, [NotNullWhen(true)] out ResourceCacheEntry? entry)
    {
        ArgumentNullException.ThrowIfNull(name);
        return cache.TryGetValue(name, out entry);
    }

    /// <summary>
    /// Registers a new resource under <paramref name="name"/>
    /// </summary>
    /// <param name="name">The name of the resource</param>
    /// <param name="resourceFactory">The delegate used to create the resource when it's requested</param>
    /// <param name="entry">The newly created resource entry</param>
    public void RegisterResource(string name, GraphicsResourceFactory<TResource> resourceFactory, out ResourceCacheEntry entry)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(resourceFactory);

        entry = cache.GetOrAdd(name, c => new ResourceCacheEntry(this, resourceFactory));
    }

    /// <summary>
    /// Attempts to obtain a <typeparamref name="TResource"/> under <paramref name="name"/>, or creates a new one using <paramref name="factory"/> if one is not found
    /// </summary>
    public ResourceCacheEntry GetOrAddResource(string name, GraphicsResourceFactory<TResource> factory)
    {
        return cache.GetOrAdd(name, name => new ResourceCacheEntry(this, factory));
    }

    /// <summary>
    /// Checks if this cache contains a resource under <paramref name="name"/>
    /// </summary>
    public bool ContainsResource(string name)
        => cache.ContainsKey(name);
}
