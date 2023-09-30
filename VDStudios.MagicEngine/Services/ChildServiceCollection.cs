using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace VDStudios.MagicEngine.Services;

/// <summary>
/// Represents a collection of services that, following the Game -> Scene structure, allows for its services to be overriden, or new services not available Game wide to be added
/// </summary>
public sealed class ChildServiceCollection<TGameObject> : ServiceCollection, IServiceRegistrar
    where TGameObject : GameObject
{
    /// <summary>
    /// The <typeparamref name="TGameObject"/> that owns this <see cref="ChildServiceCollection{TGameObject}"/>
    /// </summary>
    public TGameObject Owner { get; }

    /// <summary>
    /// The <see cref="ServiceCollection"/> that acts as a parent to this <see cref="ChildServiceCollection{TGameObject}"/>
    /// </summary>
    public ServiceCollection Parent { get; }

    internal ChildServiceCollection(ServiceCollection parent, TGameObject gameObject) : base(gameObject)
    {
        ArgumentNullException.ThrowIfNull(gameObject);
        ArgumentNullException.ThrowIfNull(parent);
        Parent = parent;
        Owner = gameObject;
    }

    /// <inheritdoc/>
    internal override object VerifyService(ServiceInfo info)
    {
        var s = info.Lifetime is not ServiceLifetime.Singleton
                ? throw new InvalidOperationException("Scoped and Transient services must be obtained from a ServiceScope")
                : FetchSingleton(info);

        return s.GetType().IsAssignableTo(info.Type) is false
            ? throw new InvalidOperationException($"Object registered under type {info.Type} is not compatible since it's of type {s.GetType()}")
            : s;
    }

    /// <inheritdoc/>
    internal override ServiceInfo InternalGetService(Type type)
    {
        Debug.Assert(type.IsClass, "Service type is not a class type");

        lock (ServiceDictionary)
            if (ServiceDictionary.TryGetValue(type, out var info) || Parent.InternalTryGetService(type, out info))
                return info;

        throw ThrowForNotFound(type);
    }

    /// <inheritdoc/>
    internal override bool InternalTryGetService(Type type, [NotNullWhen(true)][MaybeNullWhen(false)] out ServiceInfo info)
    {
        Debug.Assert(type.IsClass, "Service type is not a class type");

        lock (ServiceDictionary)
            if (ServiceDictionary.TryGetValue(type, out info))
                return true;

        return Parent.InternalTryGetService(type, out info);
    }

    #region Registration

    internal override bool HasService(Type type)
        => base.HasService(type) || Parent.HasService(type);

    internal void DisableRegistration()
        => RegistrationDisabled = true;

    internal bool IsRegistrationDisabled => RegistrationDisabled;

    private bool RegistrationDisabled;

    private void ThrowIfRegistrationDisabled()
    {
        if (RegistrationDisabled)
            throw new InvalidOperationException("Services can no longer be registered on this SceneServiceCollection");
    }

    #region Register

    void IServiceRegistrar.RegisterService(object service) 
        => ((IServiceRegistrar)this).RegisterService(service.GetType(), (_, _) => service, ServiceLifetime.Singleton);

    void IServiceRegistrar.RegisterService(Type interfaceType, object service) 
        => ((IServiceRegistrar)this).RegisterService(interfaceType, (_, _) => service, ServiceLifetime.Singleton);

    void IServiceRegistrar.RegisterService<TService>(TService service) 
        => ((IServiceRegistrar)this).RegisterService((_, _) => service, ServiceLifetime.Singleton);

    void IServiceRegistrar.RegisterService<TInterface, TService>(TService service) 
        => ((IServiceRegistrar)this).RegisterService<TInterface, TService>((_, _) => service, ServiceLifetime.Singleton);

    #endregion

    #region Register Factory

    void IServiceRegistrar.RegisterService(Type type, Func<Type, ServiceCollection, object> serviceFactory, ServiceLifetime lifetime)
    {
        ThrowIfRegistrationDisabled();
        if (type.IsValueType)
            throw new ArgumentException("type cannot be a ValueType", nameof(type));

        if (lifetime == ServiceLifetime.Singleton && Parent.HasService(type))
            throw new ArgumentException($"Cannot override a singleton Service. Service type: {type}", nameof(lifetime));

        lock (ServiceDictionary)
            ServiceDictionary[type] = new(type, serviceFactory, lifetime, this);
    }

    void IServiceRegistrar.RegisterService<TService>(Func<Type, ServiceCollection, TService> serviceFactory, ServiceLifetime lifetime)
    {
        ThrowIfRegistrationDisabled();

        if (lifetime == ServiceLifetime.Singleton && Parent.HasService(typeof(TService)))
            throw new ArgumentException($"Cannot override a singleton Service. Service type: {typeof(TService)}", nameof(lifetime));

        lock (ServiceDictionary)
            ServiceDictionary[typeof(TService)] = new(typeof(TService), serviceFactory, lifetime, this);
    }

    void IServiceRegistrar.RegisterService<TInterface, TService>(Func<Type, ServiceCollection, TService> serviceFactory, ServiceLifetime lifetime)
    {
        ThrowIfRegistrationDisabled();

        if (lifetime == ServiceLifetime.Singleton && Parent.HasService(typeof(TInterface)))
            throw new ArgumentException($"Cannot override a singleton Service. Service type: {typeof(TInterface)}", nameof(lifetime));

        lock (ServiceDictionary)
            ServiceDictionary[typeof(TInterface)] = new(typeof(TInterface), serviceFactory, lifetime, this);
    }

    #endregion

    #endregion
}
