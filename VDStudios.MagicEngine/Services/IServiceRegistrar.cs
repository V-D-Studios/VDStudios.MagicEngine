namespace VDStudios.MagicEngine.Services;

/// <summary>
/// An interface that exposes methods to register new services onto a <see cref="ServiceCollection"/>
/// </summary>
public interface IServiceRegistrar
{
    /// <summary>
    /// Registers or overrides a service
    /// </summary>
    /// <param name="service">The service to register</param>
    public void RegisterService(object service);

    /// <summary>
    /// Registers or overrides a service of behind type <paramref name="interfaceType"/>
    /// </summary>
    /// <param name="service">The service to register</param>
    /// <param name="interfaceType">The type of service that abstract <paramref name="service"/> away</param>
    public void RegisterService(Type interfaceType, object service);

    /// <summary>
    /// Registers or overrides a service of type <typeparamref name="TService"/> as a singleton instance
    /// </summary>
    /// <typeparam name="TService">The type of service to register</typeparam>
    /// <param name="service">The service to register</param>
    public void RegisterService<TService>(TService service) where TService : class;

    /// <summary>
    /// Registers or overrides a service of type <typeparamref name="TService"/> behind <typeparamref name="TInterface"/> as a singleton instance
    /// </summary>
    /// <typeparam name="TService">The type of service to register</typeparam>
    /// <typeparam name="TInterface">The type of service that abstract <paramref name="service"/> away</typeparam>
    /// <param name="service">The service to register</param>
    public void RegisterService<TInterface, TService>(TService service)
        where TInterface : class
        where TService : class, TInterface;

    /// <summary>
    /// Registers or overrides a service that will be constructed through <paramref name="serviceFactory"/>
    /// </summary>
    /// <param name="type">The type of the service that will be created</param>
    /// <param name="serviceFactory">The factory of the objects that offer the service</param>
    /// <param name="lifetime">The lifetime of the service</param>
    public void RegisterService(Type type, Func<Type, ServiceCollection, object> serviceFactory, ServiceLifetime lifetime = ServiceLifetime.Scoped);

    /// <summary>
    /// Registers or overrides a service of type <typeparamref name="TService"/> behind <typeparamref name="TInterface"/> that will be constructed through <paramref name="serviceFactory"/>
    /// </summary>
    /// <typeparam name="TService">The type of service to register</typeparam>
    /// <typeparam name="TInterface">The type of service that abstract <paramref name="serviceFactory"/> away</typeparam>
    /// <param name="serviceFactory">The factory of the objects that offer the service</param>
    /// <param name="lifetime">The lifetime of the service</param>
    public void RegisterService<TInterface, TService>(Func<Type, ServiceCollection, TService> serviceFactory, ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TInterface : class
        where TService : class, TInterface;

    /// <summary>
    /// Registers or overrides a service of type <typeparamref name="TService"/> that will be constructed through <paramref name="serviceFactory"/>
    /// </summary>
    /// <typeparam name="TService">The type of service to register</typeparam>
    /// <param name="serviceFactory">The factory of the objects that offer the service</param>
    /// <param name="lifetime">The lifetime of the service</param>
    public void RegisterService<TService>(Func<Type, ServiceCollection, TService> serviceFactory, ServiceLifetime lifetime = ServiceLifetime.Scoped) 
        where TService : class;
}