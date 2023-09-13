using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace VDStudios.MagicEngine.Graphics.Veldrid;

/// <summary>
/// A Cache containing graphics resources for a <see cref="VeldridGraphicsContext"/> that are to be created on-demand
/// </summary>
/// <typeparam name="TResource">The resource this cache maintains</typeparam>
/// <typeparam name="TOwner">The owner of each resource</typeparam>
public partial class GraphicsContextOwnedResourceFactoryCache<TOwner, TResource>
    where TResource : class
    where TOwner : class
{
    internal GraphicsContextOwnedResourceFactoryCache(IVeldridGraphicsContextResources resourceOwner)
    {
        ResourceOwner = resourceOwner;
    }

    private readonly ConcurrentDictionary<string, ResourceOwnerCacheEntry> cache = new();
    private readonly IVeldridGraphicsContextResources ResourceOwner;

    /// <summary>
    /// Removes the resource under <paramref name="name"/> from the cache
    /// </summary>
    /// <param name="name">The name of the resource</param>
    /// <param name="entry">The dropped entry. It has not been cleared</param>
    /// <returns><see langword="true"/> if the resource was found and removed and is contained in <paramref name="entry"/></returns>
    public bool RemoveResource(string name, [NotNullWhen(true)] out ResourceOwnerCacheEntry? entry)
        => cache.Remove(name, out entry);

    /// <summary>
    /// Obtains the resource under <paramref name="name"/>
    /// </summary>
    /// <param name="name">The name of the resource</param>
    public ResourceOwnerCacheEntry GetResource(string name)
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
    public bool TryGetResource(string name, [NotNullWhen(true)] out ResourceOwnerCacheEntry? entry)
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
    public void RegisterResource(string name, Func<IVeldridGraphicsContextResources, TOwner> resourceFactory, out ResourceOwnerCacheEntry entry)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(resourceFactory);

        entry = cache.GetOrAdd(name, c => new ResourceOwnerCacheEntry(this, resourceFactory));
    }

    /// <summary>
    /// Attempts to obtain a <typeparamref name="TResource"/> under <paramref name="name"/>, or creates a new one using <paramref name="factory"/> if one is not found
    /// </summary>
    public ResourceOwnerCacheEntry GetOrAddResource(string name, Func<IVeldridGraphicsContextResources, TOwner> factory)
    {
        return cache.GetOrAdd(name, name => new ResourceOwnerCacheEntry(this, factory));
    }

    /// <summary>
    /// Checks if this cache contains a resource under <paramref name="name"/>
    /// </summary>
    public bool ContainsResource(string name)
        => cache.ContainsKey(name);
}
