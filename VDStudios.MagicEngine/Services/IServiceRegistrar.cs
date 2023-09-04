namespace VDStudios.MagicEngine.Services;

public interface IServiceRegistrar
{
    void RegisterService(object service);
    void RegisterService(Type type, Func<ServiceCollection, object> serviceFactory, ServiceLifetime lifetime = ServiceLifetime.Scoped);
    void RegisterService(Type interfaceType, object service);
    void RegisterService<TInterface, TService>(Func<ServiceCollection, TService> serviceFactory, ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TInterface : class
        where TService : class, TInterface;
    void RegisterService<TInterface, TService>(TService service)
        where TInterface : class
        where TService : class, TInterface;
    void RegisterService<TService>(Func<ServiceCollection, TService> serviceFactory, ServiceLifetime lifetime = ServiceLifetime.Scoped) where TService : class;
    void RegisterService<TService>(TService service) where TService : class;
}