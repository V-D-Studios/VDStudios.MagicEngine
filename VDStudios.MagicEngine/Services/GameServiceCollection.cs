using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace VDStudios.MagicEngine.Services;

/// <summary>
/// Represents a collection of services for the Game
/// </summary>
public sealed class GameServiceCollection : ServiceCollection, IServiceRegistrar
{
    private readonly Dictionary<Type, object> InstantiatedSingletons = new();

    internal GameServiceCollection(Game game) : base(game) { }

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
            if (ServiceDictionary.TryGetValue(type, out var info))
                return info;

        throw ThrowForNotFound(type);
    }

    /// <inheritdoc/>
    internal override bool InternalTryGetService(Type type, [NotNullWhen(true)][MaybeNullWhen(false)] out ServiceInfo info)
    {
        Debug.Assert(type.IsClass, "Service type is not a class type");

        lock (ServiceDictionary)
            return ServiceDictionary.TryGetValue(type, out info);
    }

    internal override object FetchSingleton(ServiceInfo info)
    {
        lock (InstantiatedSingletons)
            if (InstantiatedSingletons.TryGetValue(info.Type, out var obj))
                return obj;
            else
            {
                var o = info.Factory(info.Type, this);
                InstantiatedSingletons.Add(info.Type, o);
                return o;
            }
    }

    #region Registration

    internal void DisableRegistration()
        => RegistrationDisabled = true;

    private bool RegistrationDisabled;

    private void ThrowIfRegistrationDisabled()
    {
        if (RegistrationDisabled)
            throw new InvalidOperationException("Services can no longer be registered on this GameServiceCollection");
    }

    #region Register

    /// <summary>
    /// Registers or overrides a service
    /// </summary>
    /// <param name="service">The service to register</param>
    void IServiceRegistrar.RegisterService(object service)
    {
        ((IServiceRegistrar)this).RegisterService(service.GetType(), (_, _) => service, ServiceLifetime.Singleton);
    }

    /// <summary>
    /// Registers or overrides a service of behind type <paramref name="interfaceType"/>
    /// </summary>
    /// <param name="service">The service to register</param>
    /// <param name="interfaceType">The type of service that abstract <paramref name="service"/> away</param>
    void IServiceRegistrar.RegisterService(Type interfaceType, object service)
        => ((IServiceRegistrar)this).RegisterService(interfaceType, (_, _) => service, ServiceLifetime.Singleton);

    /// <summary>
    /// Registers or overrides a service of type <typeparamref name="TService"/> as a singleton instance
    /// </summary>
    /// <typeparam name="TService">The type of service to register</typeparam>
    /// <param name="service">The service to register</param>
    void IServiceRegistrar.RegisterService<TService>(TService service)
        => ((IServiceRegistrar)this).RegisterService<TService>((_, _) => service, ServiceLifetime.Singleton);

    /// <summary>
    /// Registers or overrides a service of type <typeparamref name="TService"/> behind <typeparamref name="TInterface"/> as a singleton instance
    /// </summary>
    /// <typeparam name="TService">The type of service to register</typeparam>
    /// <typeparam name="TInterface">The type of service that abstract <paramref name="service"/> away</typeparam>
    /// <param name="service">The service to register</param>
    void IServiceRegistrar.RegisterService<TInterface, TService>(TService service)
        => ((IServiceRegistrar)this).RegisterService<TInterface, TService>((_, _) => service, ServiceLifetime.Singleton);

    #endregion

    #region Register Factory

    /// <summary>
    /// Registers or overrides a service that will be constructed through <paramref name="serviceFactory"/>
    /// </summary>
    /// <param name="type">The type of the service that will be created</param>
    /// <param name="serviceFactory">The factory of the objects that offer the service</param>
    /// <param name="lifetime">The lifetime of the service</param>
    void IServiceRegistrar.RegisterService(Type type, Func<Type, ServiceCollection, object> serviceFactory, ServiceLifetime lifetime)
    {
        ThrowIfRegistrationDisabled();
        if (type.IsValueType)
            throw new ArgumentException("type cannot be a ValueType", nameof(type));

        lock (ServiceDictionary)
            ServiceDictionary[type] = new(type, serviceFactory, lifetime);
    }

    /// <summary>
    /// Registers or overrides a service of type <typeparamref name="TService"/> that will be constructed through <paramref name="serviceFactory"/>
    /// </summary>
    /// <typeparam name="TService">The type of service to register</typeparam>
    /// <param name="serviceFactory">The factory of the objects that offer the service</param>
    /// <param name="lifetime">The lifetime of the service</param>
    void IServiceRegistrar.RegisterService<TService>(Func<Type, ServiceCollection, TService> serviceFactory, ServiceLifetime lifetime)
    {
        ThrowIfRegistrationDisabled();
        lock (ServiceDictionary)
            ServiceDictionary[typeof(TService)] = new(typeof(TService), serviceFactory, lifetime);
    }

    /// <summary>
    /// Registers or overrides a service of type <typeparamref name="TService"/> behind <typeparamref name="TInterface"/> that will be constructed through <paramref name="serviceFactory"/>
    /// </summary>
    /// <typeparam name="TService">The type of service to register</typeparam>
    /// <typeparam name="TInterface">The type of service that abstract <paramref name="serviceFactory"/> away</typeparam>
    /// <param name="serviceFactory">The factory of the objects that offer the service</param>
    /// <param name="lifetime">The lifetime of the service</param>
    void IServiceRegistrar.RegisterService<TInterface, TService>(Func<Type, ServiceCollection, TService> serviceFactory, ServiceLifetime lifetime)
    {
        ThrowIfRegistrationDisabled();
        lock (ServiceDictionary)
            ServiceDictionary[typeof(TInterface)] = new(typeof(TInterface), serviceFactory, lifetime);
    }

    #endregion

    #endregion
}
