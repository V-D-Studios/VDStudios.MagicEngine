using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.Internal;

/// <summary>
/// Represents a collection of services that, following the Game -> Scene -> Node structure, allows for its services to be overriden, or to provide services not available upwards of the tree
/// </summary>
public sealed class ServiceCollection
{
    private readonly Dictionary<Type, Func<ServiceCollection, object>> ServiceDictionary;
    private WeakReference<ServiceCollection>? PreviousLayer;

    internal ServiceCollection(ServiceCollection? previous)
    {
        SetPrev(previous);
        ServiceDictionary = new(previous?.ServiceDictionary.Count ?? 10);
    }

    private static void ThrowForNotFound<TService>() 
        => throw new KeyNotFoundException($"Could not find a service for type {typeof(TService)} at any point of the node tree");

    internal void SetPrev(ServiceCollection? previous)
    {
        PreviousLayer = previous is null ? null : (new(previous));
    }

    /// <summary>
    /// Fetches the requested service from this collection or from upwards of the tree
    /// </summary>
    /// <typeparam name="TService">The type of the service that is being requested</typeparam>
    /// <returns>An object representing the requested service</returns>
    public TService GetService<TService>() where TService : class
    {
        if (!ServiceDictionary.TryGetValue(typeof(TService), out var value))
        {
            var top = PreviousLayer;
            while (top is not null && top.TryGetTarget(out var sd))
            {
                if (sd.ServiceDictionary.TryGetValue(typeof(TService), out value))
                    return (TService)value(this);
                top = sd.PreviousLayer;
            }
            ThrowForNotFound<TService>();
        }
        return (TService)value!(this);
    }

    /// <summary>
    /// Tries to fetch the requested service from this collection or from upwards of the tree
    /// </summary>
    /// <typeparam name="TService">The type of the service that is being requested</typeparam>
    /// <param name="service">An object representing the requested service</param>
    /// <returns><see langword="true"/> if the requested service was found somewhere and <paramref name="service"/> is <see langword="not"/> <see langword="null"/>. <see langword="false"/> otherwise.</returns>
    public bool TryGetService<TService>([NotNullWhen(true)] out TService? service) where TService : class
    {
        if (!ServiceDictionary.TryGetValue(typeof(TService), out var value))
        {
            var top = PreviousLayer;
            while (top is not null && top.TryGetTarget(out var sd))
            {
                if (sd.ServiceDictionary.TryGetValue(typeof(TService), out value))
                {
                    service = (TService)value(this);
                    return true;
                }
                top = sd.PreviousLayer;
            }
            service = null;
            return false;
        }
        service = (TService)value(this);
        return true;
    }

    #region Register singleton

    /// <summary>
    /// Registers or overrides a service of type <typeparamref name="TService"/> as a singleton instance
    /// </summary>
    /// <typeparam name="TService">The type of service to register</typeparam>
    /// <param name="service">The service to register</param>
    public void RegisterService<TService>(TService service) where TService : class
        => ServiceDictionary[typeof(TService)] = _ => service;

    /// <summary>
    /// Registers or overrides a service of type <typeparamref name="TService"/> behind <typeparamref name="TInterface"/> as a singleton instance
    /// </summary>
    /// <typeparam name="TService">The type of service to register</typeparam>
    /// <typeparam name="TInterface">The type of service that abstract <paramref name="service"/> away</typeparam>
    /// <param name="service">The service to register</param>
    public void RegisterService<TInterface, TService>(TService service) where TInterface : class where TService : class, TInterface
        => ServiceDictionary[typeof(TInterface)] = _ => service;

    #endregion

    #region Register Factory

    /// <summary>
    /// Registers or overrides a service of type <typeparamref name="TService"/> that will be constructed on each request
    /// </summary>
    /// <typeparam name="TService">The type of service to register</typeparam>
    /// <param name="service">The factory of the objects that offer the service</param>
    public void RegisterService<TService>(Func<ServiceCollection, TService> service) where TService : class
        => ServiceDictionary[typeof(TService)] = service;

    /// <summary>
    /// Registers or overrides a service of type <typeparamref name="TService"/> behind <typeparamref name="TInterface"/> that will be constructed on each request
    /// </summary>
    /// <typeparam name="TService">The type of service to register</typeparam>
    /// <typeparam name="TInterface">The type of service that abstract <paramref name="service"/> away</typeparam>
    /// <param name="service">The factory of the objects that offer the service</param>
    public void RegisterService<TInterface, TService>(Func<ServiceCollection, TService> service) where TInterface : class where TService : class, TInterface
        => ServiceDictionary[typeof(TInterface)] = service;

    #endregion

    #region Register Contextualized Factory

    /// <summary>
    /// Registers or overrides a service of type <typeparamref name="TService"/> that will be constructed on each request
    /// </summary>
    /// <typeparam name="TService">The type of service to register</typeparam>
    /// <typeparam name="TContext">The type of the context to pass to the <typeparamref name="TService"/> object factory</typeparam>
    /// <param name="service">The factory of the objects that offer the service</param>
    /// <param name="context">The context to pass to <paramref name="service"/></param>
    public void RegisterService<TService, TContext>(Func<ServiceCollection, TContext, TService> service, TContext context) where TService : class
        => ServiceDictionary[typeof(TService)] = col => service(col, context);

    /// <summary>
    /// Registers or overrides a service of type <typeparamref name="TService"/> behind <typeparamref name="TInterface"/> that will be constructed on each request
    /// </summary>
    /// <typeparam name="TService">The type of service to register</typeparam>
    /// <typeparam name="TInterface">The type of service that abstract <paramref name="service"/> away</typeparam>
    /// <typeparam name="TContext">The type of the context to pass to the <typeparamref name="TService"/> object factory</typeparam>
    /// <param name="service">The factory of the objects that offer the service</param>
    /// <param name="context">The context to pass to <paramref name="service"/></param>
    public void RegisterService<TInterface, TService, TContext>(Func<ServiceCollection, TContext, TService> service, TContext context) where TInterface : class where TService : class, TInterface
        => ServiceDictionary[typeof(TInterface)] = col => service(col, context);

    #endregion
}
