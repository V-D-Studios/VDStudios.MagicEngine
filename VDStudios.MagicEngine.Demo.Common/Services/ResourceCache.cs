using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDL2.NET;

namespace VDStudios.MagicEngine.Demo.Common.Services;

public class ResourceCache<TOwner, TResource>
    where TOwner : notnull
{
    public sealed class ResourceCacheEntry
    {
        private readonly ConcurrentDictionary<TOwner, TResource> cache = new();

        public Func<TOwner, TResource> Factory { get; }

        public void Clear()
        {
            cache.Clear();
        }

        public void Clear(TOwner c)
        {
            ArgumentNullException.ThrowIfNull(c);
            cache.Remove(c, out _);
        }

        public ResourceCacheEntry(Func<TOwner, TResource> resourceFactory)
        {
            ArgumentNullException.ThrowIfNull(resourceFactory);
            Factory = c => cache.GetOrAdd(c, resourceFactory);
        }
    }

    private readonly ConcurrentDictionary<string, ResourceCacheEntry> cache = new();

    public bool RemoveResource(string name, [NotNullWhen(true)] out ResourceCacheEntry? entry)
        => cache.Remove(name, out entry);

    public ResourceCacheEntry GetResource(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return cache[name];
    }

    public void RegisterResource(string name, Func<TOwner, TResource> resourceFactory, out ResourceCacheEntry entry)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(resourceFactory);

        entry = cache.GetOrAdd(name, c => new ResourceCacheEntry(resourceFactory));
    }
}
