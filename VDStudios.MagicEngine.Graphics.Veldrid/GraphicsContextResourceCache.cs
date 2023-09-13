using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Veldrid;

namespace VDStudios.MagicEngine.Graphics.Veldrid;

/// <summary>
/// A Cache containing graphics resources for a <see cref="VeldridGraphicsContext"/>
/// </summary>
/// <typeparam name="TResource">The resource this cache maintains</typeparam>
public sealed class GraphicsContextResourceCache<TResource>
    where TResource : notnull
{
    internal GraphicsContextResourceCache(VeldridGraphicsContext context)
        => this.context = context ?? throw new ArgumentNullException(nameof(context));

    private readonly record struct ResourceKey(Type Type, string? Name);

    private readonly VeldridGraphicsContext context;
    private readonly ConcurrentDictionary<ResourceKey, TResource> dict = new();

    /// <summary>
    /// Attempts to remove a <typeparamref name="TResource"/> from <typeparamref name="T"/> under <paramref name="name"/>
    /// </summary>
    /// <typeparam name="T">The type that the resource is for</typeparam>
    /// <param name="resource">The resource, not <see langword="null"/> if found and removed</param>
    /// <param name="name">The name of the resource in the <typeparamref name="T"/> resource set</param>
    /// <returns><see langword="true"/> if the resource is found and has been removed. <see langword="false"/> otherwise</returns>
    public bool RemoveResource<T>([NotNullWhen(true)] out TResource? resource, string? name = null)
        => dict.TryRemove(new ResourceKey(typeof(T), name), out resource);

    /// <summary>
    /// Attempts to obtain a <typeparamref name="TResource"/> for <typeparamref name="T"/> under <paramref name="name"/>
    /// </summary>
    /// <typeparam name="T">The type that the resource is for</typeparam>
    /// <param name="resource">The resource, <see langword="null"/> if not found</param>
    /// <param name="name">The name of the resource in the <typeparamref name="T"/> resource set</param>
    /// <returns><see langword="true"/> if the resource is found and <paramref name="resource"/> has it. <see langword="false"/> otherwise</returns>
    public bool TryGetResource<T>([NotNullWhen(true)] out TResource? resource, string? name = null)
        => TryGetResource(typeof(T), out resource, name);

    /// <summary>
    /// Attempts to obtain a <typeparamref name="TResource"/> for <typeparamref name="T"/> under <paramref name="name"/>, or creates a new one using <paramref name="factory"/> if one is not found
    /// </summary>
    public TResource GetOrAddResource<T>(Func<IVeldridGraphicsContextResources, TResource> factory, string? name = null)
        => GetOrAddResource(typeof(T), factory, name);

    /// <summary>
    /// Checks if this resource set contains a TResource for <typeparamref name="T"/> under <paramref name="name"/>
    /// </summary>
    public bool ContainsResource<T>(string? name = null)
        => ContainsResource(typeof(T), name);

    /// <summary>
    /// Attempts to remove a <typeparamref name="TResource"/> from <paramref name="type"/> under <paramref name="name"/>
    /// </summary>
    /// <param name="type">The type that the resource is for</param>
    /// <param name="resource">The resource, not <see langword="null"/> if found and removed</param>
    /// <param name="name">The name of the resource in the <paramref name="type"/> resource set</param>
    /// <returns><see langword="true"/> if the resource is found and has been removed. <see langword="false"/> otherwise</returns>
    public bool RemoveResource(Type type, [NotNullWhen(true)] out TResource? resource, string? name = null)
        => dict.TryRemove(new ResourceKey(type, name), out resource);

    /// <summary>
    /// Attempts to obtain a <typeparamref name="TResource"/> for <paramref name="type"/> under <paramref name="name"/>
    /// </summary>
    /// <param name="type">The type that the resource is for</param>
    /// <param name="resource">The resource, <see langword="null"/> if not found</param>
    /// <param name="name">The name of the resource in the <paramref name="type"/> resource set</param>
    /// <returns><see langword="true"/> if the resource is found and <paramref name="resource"/> has it. <see langword="false"/> otherwise</returns>
    public bool TryGetResource(Type type, [NotNullWhen(true)] out TResource? resource, string? name = null)
        => dict.TryGetValue(new ResourceKey(type, name), out resource);

    /// <summary>
    /// Attempts to obtain a <typeparamref name="TResource"/> for <paramref name="type"/> under <paramref name="name"/>, or creates a new one using <paramref name="factory"/> if one is not found
    /// </summary>
    public TResource GetOrAddResource(Type type, Func<IVeldridGraphicsContextResources, TResource> factory, string? name = null)
        => dict.GetOrAdd(new ResourceKey(type, name), (sk, fa) => fa(context), factory);

    /// <summary>
    /// Checks if this resource set contains a TResource for <paramref name="type"/> under <paramref name="name"/>
    /// </summary>
    public bool ContainsResource(Type type, string? name = null)
        => dict.ContainsKey(new ResourceKey(type, name));
}
