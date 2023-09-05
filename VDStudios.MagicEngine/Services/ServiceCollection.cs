using System.Diagnostics.CodeAnalysis;
using VDStudios.MagicEngine.Internal;

namespace VDStudios.MagicEngine.Services;

/// <summary>
/// Represents a collection of services that, following the Game -> Scene -> Node structure, allows for its services to be overriden, or to provide services not available upwards of the tree
/// </summary>
public abstract class ServiceCollection : GameObject
{
    internal ServiceCollection(IGameObject ownerObject) : base(ownerObject.Game, "Updating", "Services")
    {
        scopePool = new(pool => new ServiceScope(this), static s => { }, 2);
    }
    
    /// <summary>
    /// Creates a new <see cref="ServiceScope"/> for this <see cref="ServiceCollection"/>, through which services with a lifetime of <see cref="ServiceLifetime.Scoped"/> or <see cref="ServiceLifetime.Transient"/> can be created
    /// </summary>
    /// <returns></returns>
    public ServiceScope CreateScope()
    {
        scopePool.Rent().GetItem(out var scope);
        scope.Enable();
        return scope;
    }
    internal readonly ObjectPool<ServiceScope> scopePool;

    internal readonly Dictionary<Type, ServiceInfo> ServiceDictionary = new();

    internal static Exception ThrowForNotFound(Type type)
        => new KeyNotFoundException($"Could not find a service for type {type} at any point of the node tree");

    internal abstract object VerifyService(ServiceInfo info);

    internal TService VerifyService<TService>(ServiceInfo info) where TService : class
        => (TService)VerifyService(info);

    internal abstract ServiceInfo InternalGetService(Type type);

    internal abstract bool InternalTryGetService(Type type, [NotNullWhen(true)][MaybeNullWhen(false)] out ServiceInfo info);

    internal abstract object FetchSingleton(ServiceInfo info);

    /// <summary>
    /// Tries to fetch the requested service from this collection or from upwards of the tree
    /// </summary>
    /// <typeparam name="TService">The type of the service that is being requested</typeparam>
    /// <param name="service">An object representing the requested service</param>
    /// <returns><see langword="true"/> if the requested service was found somewhere and <paramref name="service"/> is <see langword="not"/> <see langword="null"/>. <see langword="false"/> otherwise.</returns>
    public bool TryGetService<TService>([NotNullWhen(true)] out TService? service) where TService : class
    {
        if (InternalTryGetService(typeof(TService), out var info))
        {
            service = VerifyService<TService>(info);
            return true;
        }
        service = null;
        return false;
    }

    /// <summary>
    /// Fetches the requested service from this collection or from upwards of the tree
    /// </summary>
    /// <typeparam name="TService">The type of the service that is being requested</typeparam>
    /// <returns>An object representing the requested service</returns>
    public TService GetService<TService>() where TService : class
        => VerifyService<TService>(InternalGetService(typeof(TService)));

    /// <summary>
    /// Tries to fetch the requested service from this collection or from upwards of the tree
    /// </summary>
    /// <param name="type">The type of the service that is being requested</param>
    /// <param name="service">An object representing the requested service</param>
    /// <returns><see langword="true"/> if the requested service was found somewhere and <paramref name="service"/> is <see langword="not"/> <see langword="null"/>. <see langword="false"/> otherwise.</returns>
    public bool TryGetService(Type type, [NotNullWhen(true)] out object? service)
    {
        if (InternalTryGetService(type, out var info))
        {
            service = VerifyService(info);
            return true;
        }
        service = null;
        return false;
    }

    /// <summary>
    /// Fetches the requested service from this collection or from upwards of the tree
    /// </summary>
    /// <param name="type">The type of the service that is being requested</param>
    /// <returns>An object representing the requested service</returns>
    public object GetService(Type type)
        => VerifyService(InternalGetService(type));

    internal virtual bool HasService(Type type)
        => ServiceDictionary.ContainsKey(type);
}
