using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.Graphics.Veldrid;
public partial class GraphicsContextOwnedResourceFactoryCache<TOwner, TResource>
    where TResource : class
    where TOwner : class
{
    /// <summary>
    /// A resource entry for a <see cref="GraphicsContextOwnedResourceFactoryCache{TOwner, TResource}"/>
    /// </summary>
    public sealed class ResourceOwnerCacheEntry
    {
        internal readonly GraphicsContextOwnedResourceFactoryCache<TOwner, TResource> OwnerCache;
        private readonly Dictionary<string, OwnedResourceCacheEntry> cache = new();
        private readonly Func<IVeldridGraphicsContextResources, TOwner> OwnerFactory;
        private TOwner? ownerCache;

        /// <summary>
        /// A delegate that can be used to indirectly access <see cref="OwnerResource"/>
        /// </summary>
        public GraphicsResourceFactory<TOwner> OwnerDelegate { get; }

        /// <summary>
        /// The resource that owns the entry and its subresources of type <typeparamref name="TResource"/>
        /// </summary>
        /// <remarks>
        /// Created lazyly the first time its requested, can be cleared using <see cref="Clear"/>
        /// </remarks>
        public TOwner OwnerResource
        {
            get
            {
                lock (cache)
                    return ownerCache ??= OwnerFactory(OwnerCache.ResourceOwner);
            }
        }

        /// <summary>
        /// Clears the cache for this <see cref="ResourceOwnerCacheEntry"/>, the next time <see cref="OwnerFactory"/> is used it will be created again. All of its owned resource will also be cleared
        /// </summary>
        /// <remarks>
        /// If the resource implements <see cref="IDisposable"/>, it will be disposed
        /// </remarks>
        public void Clear()
        {
            lock (cache)
            {
                foreach (var (k, v) in cache)
                    v.Clear();

                cache.Clear();

                if (ownerCache is IDisposable disposable)
                    disposable.Dispose();
                ownerCache = null;
            }
        }

        /// <summary>
        /// Removes the resource under <paramref name="name"/> from the cache
        /// </summary>
        /// <param name="name">The name of the resource</param>
        /// <param name="entry">The dropped entry. It has not been cleared</param>
        /// <returns><see langword="true"/> if the resource was found and removed and is contained in <paramref name="entry"/></returns>
        public bool RemoveResource(string name, [NotNullWhen(true)] out OwnedResourceCacheEntry? entry)
        {
            lock (cache)
                return cache.Remove(name, out entry);
        }

        /// <summary>
        /// Obtains the resource under <paramref name="name"/>
        /// </summary>
        /// <param name="name">The name of the resource</param>
        public OwnedResourceCacheEntry GetResource(string name)
        {
            ArgumentNullException.ThrowIfNull(name);
            lock (cache)
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
        public bool TryGetResource(string name, [NotNullWhen(true)] out OwnedResourceCacheEntry? entry)
        {
            ArgumentNullException.ThrowIfNull(name);
            lock (cache)
                return cache.TryGetValue(name, out entry);
        }

        /// <summary>
        /// Registers a new resource under <paramref name="name"/>
        /// </summary>
        /// <param name="name">The name of the resource</param>
        /// <param name="resourceFactory">The delegate used to create the resource when it's requested</param>
        public OwnedResourceCacheEntry RegisterResource(string name, Func<IVeldridGraphicsContextResources, TOwner, TResource> resourceFactory)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(resourceFactory);

            lock (cache)
                if (cache.TryGetValue(name, out var res))
                    throw new ArgumentException($"A resource under '{name}' has already been registered", nameof(name));
                else
                {
                    cache.Add(name, res = new OwnedResourceCacheEntry(this, resourceFactory));
                    return res;
                }
        }

        /// <summary>
        /// Attempts to obtain a <typeparamref name="TResource"/> under <paramref name="name"/>, or creates a new one using <paramref name="resourceFactory"/> if one is not found
        /// </summary>
        public OwnedResourceCacheEntry GetOrAddResource(string name, Func<IVeldridGraphicsContextResources, TOwner, TResource> resourceFactory)
        {
            OwnedResourceCacheEntry? value;
            lock (cache)
                if (cache.TryGetValue(name, out value) is false)
                    cache.Add(name, value = new OwnedResourceCacheEntry(this, resourceFactory));
            return value;
        }

        /// <summary>
        /// Checks if this cache contains a resource under <paramref name="name"/>
        /// </summary>
        public bool ContainsResource(string name)
            => cache.ContainsKey(name);

        internal ResourceOwnerCacheEntry(GraphicsContextOwnedResourceFactoryCache<TOwner, TResource> owner, Func<IVeldridGraphicsContextResources, TOwner> resourceFactory)
        {
            ArgumentNullException.ThrowIfNull(resourceFactory);
            ArgumentNullException.ThrowIfNull(owner);
            OwnerCache = owner;
            OwnerFactory = resourceFactory;
            OwnerDelegate = _ => OwnerResource;
        }
    } 
}