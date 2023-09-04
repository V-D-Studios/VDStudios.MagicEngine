namespace VDStudios.MagicEngine.Services;

/// <summary>
/// Information regarding a given service
/// </summary>
/// <param name="Type">The type the service represents</param>
/// <param name="Factory">A method for obtaining a service object</param>
/// <param name="Lifetime">The lifetime of the service</param>
public readonly record struct ServiceInfo(Type Type, Func<Type, ServiceCollection, object> Factory, ServiceLifetime Lifetime);