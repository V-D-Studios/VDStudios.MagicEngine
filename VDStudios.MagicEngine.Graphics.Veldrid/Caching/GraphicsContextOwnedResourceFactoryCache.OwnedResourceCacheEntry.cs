using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.Graphics.Veldrid;

public partial class GraphicsContextOwnedResourceFactoryCache<TOwner, TResource>
    where TResource : class
    where TOwner : class
{
    /// <summary>
    /// An owned resource entry for a <see cref="GraphicsContextOwnedResourceFactoryCache{TOwner, TResource}"/>
    /// </summary>
    public sealed class OwnedResourceCacheEntry
    {
        internal readonly ResourceOwnerCacheEntry OwnerCache;
        private readonly Func<IVeldridGraphicsContextResources, TOwner, TResource> ResourceFactory;

        private TResource? resourceCache;

        /// <summary>
        /// A delegate that can be used to indirectly access <see cref="Resource"/>
        /// </summary>
        public GraphicsResourceFactory<TResource> ResourceDelegate { get; }

        /// <summary>
        /// The resource that is maintained by this cache entry
        /// </summary>
        /// <remarks>
        /// Created lazyly the first time its requested, can be cleared using <see cref="Clear"/>
        /// </remarks>
        public TResource Resource
            => resourceCache ??= ResourceFactory(OwnerCache.OwnerCache.ResourceOwner, OwnerCache.OwnerResource);

        /// <summary>
        /// Clears the cache for this <see cref="OwnedResourceCacheEntry"/>, the next time <see cref="Resource"/> is used it will be created again
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

        internal OwnedResourceCacheEntry(ResourceOwnerCacheEntry owner, Func<IVeldridGraphicsContextResources, TOwner, TResource> resourceFactory)
        {
            ArgumentNullException.ThrowIfNull(resourceFactory);
            ArgumentNullException.ThrowIfNull(owner);
            ResourceFactory = resourceFactory;
            OwnerCache = owner;
            ResourceDelegate =
            ResourceDelegate = context => context != OwnerCache.OwnerCache.ResourceOwner ?
                throw new ArgumentException("The passed context does not own this ResourceCache", nameof(context)) : 
                Resource;
        }
    }
}

