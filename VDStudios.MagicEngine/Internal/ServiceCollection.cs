using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.Internal;

/// <summary>
/// Represents a collection of services that, following the Game -> Scene -> Node structure, allows for its services to be overriden, or to provide services not available upwards of the tree
/// </summary>
/// <remarks>
/// If this object is not disposed, it will not dispose of any of the disposable services it might have
/// </remarks>
public sealed class ServiceCollection : IDisposable
{
    private enum ServiceFlags
    {
        Scoped,
        Singleton
    }
    private readonly record struct ServiceInfo(Func<ServiceCollection, object> Factory, ServiceFlags Flags);

    private readonly List<WeakReference<IDisposable>?> DisposableServices;
    private readonly Dictionary<Type, ServiceInfo> ServiceDictionary;
    private WeakReference<ServiceCollection>? PreviousLayer;

    internal ServiceCollection(ServiceCollection? previous)
    {
        SetPrev(previous);
        DisposableServices = new(previous?.DisposableServices.Capacity ?? 5);
        ServiceDictionary = new(previous?.ServiceDictionary.Count ?? 10);
    }

    private static void ThrowForNotFound<TService>() 
        => throw new KeyNotFoundException($"Could not find a service for type {typeof(TService)} at any point of the node tree");

    internal void SetPrev(ServiceCollection? previous)
    {
        PreviousLayer = previous is null ? null : (new(previous));
    }

    private TService VerifyService<TService>(ServiceInfo info, bool isFromUpstream) where TService : class
    {
        var service = (TService)info.Factory(this);

        if (service is IDisposable disp && (info.Flags is ServiceFlags.Scoped || info.Flags is ServiceFlags.Singleton && isFromUpstream is false))
            DisposableServices.Add(new WeakReference<IDisposable>(disp));

        return service;
    }

    /// <summary>
    /// Notifies this <see cref="ServiceCollection"/> that <paramref name="service"/> is to be disposed; preventing potential exceptions when this <see cref="ServiceCollection"/> is disposed
    /// </summary>
    /// <param name="service">The service to be disposed -- Must be a scoped service or a singleton service owned by this instance</param>
    public void DisposeService(IDisposable service)
    {
        var ind = DisposableServices.FindIndex(x => x?.TryGetTarget(out var t) is true && t == service);
        if (ind is -1)
            throw new ArgumentException("Could not dispose of the passed parameter service as it is not a disposable service owned by this ServiceCollection -- It may be owned by a ServiceCollection upwards of the tree", nameof(service));
        lock (ServiceDictionary)
            DisposableServices[ind] = null;
        service.Dispose();
    }

    /// <summary>
    /// Fetches the requested service from this collection or from upwards of the tree
    /// </summary>
    /// <typeparam name="TService">The type of the service that is being requested</typeparam>
    /// <returns>An object representing the requested service</returns>
    public TService GetService<TService>() where TService : class
    {
        ServiceInfo value;
        bool obtained;
        lock (ServiceDictionary)
            obtained = ServiceDictionary.TryGetValue(typeof(TService), out value);
        if (!obtained)
        {
            var top = PreviousLayer;
            while (top is not null && top.TryGetTarget(out var sd))
            {
                lock (sd.ServiceDictionary)
                    if (sd.ServiceDictionary.TryGetValue(typeof(TService), out value))
                        return VerifyService<TService>(value, true);
                top = sd.PreviousLayer;
            }
            ThrowForNotFound<TService>();
        }
        return VerifyService<TService>(value, false);
    }

    /// <summary>
    /// Tries to fetch the requested service from this collection or from upwards of the tree
    /// </summary>
    /// <typeparam name="TService">The type of the service that is being requested</typeparam>
    /// <param name="service">An object representing the requested service</param>
    /// <returns><see langword="true"/> if the requested service was found somewhere and <paramref name="service"/> is <see langword="not"/> <see langword="null"/>. <see langword="false"/> otherwise.</returns>
    public bool TryGetService<TService>([NotNullWhen(true)] out TService? service) where TService : class
    {
        ServiceInfo value;
        bool obtained;
        lock (ServiceDictionary)
            obtained = ServiceDictionary.TryGetValue(typeof(TService), out value);
        if (!obtained) 
        {
            var top = PreviousLayer;
            while (top is not null && top.TryGetTarget(out var sd))
            {
                lock (sd.ServiceDictionary) 
                    if (sd.ServiceDictionary.TryGetValue(typeof(TService), out value))
                    {
                        service = VerifyService<TService>(value, true);
                        return true;
                    }
                top = sd.PreviousLayer;
            }
            service = null;
            return false;
        }
        service = VerifyService<TService>(value, false);
        return true;
    }

    #region Register singleton

    /// <summary>
    /// Registers or overrides a service of type <typeparamref name="TService"/> as a singleton instance
    /// </summary>
    /// <typeparam name="TService">The type of service to register</typeparam>
    /// <param name="service">The service to register</param>
    /// <param name="isSingleton">Whether this service should be treated as a singleton instance service or not</param>
    public void RegisterService<TService>(TService service, bool isSingleton = false) where TService : class
    {
        lock (ServiceDictionary)
            ServiceDictionary[typeof(TService)] = new(_ => service, isSingleton ? ServiceFlags.Singleton : ServiceFlags.Scoped);
    }

    /// <summary>
    /// Registers or overrides a service of type <typeparamref name="TService"/> behind <typeparamref name="TInterface"/> as a singleton instance
    /// </summary>
    /// <typeparam name="TService">The type of service to register</typeparam>
    /// <typeparam name="TInterface">The type of service that abstract <paramref name="service"/> away</typeparam>
    /// <param name="service">The service to register</param>
    /// <param name="isSingleton">Whether this service should be treated as a singleton instance service or not</param>
    public void RegisterService<TInterface, TService>(TService service, bool isSingleton = false) where TInterface : class where TService : class, TInterface
    {
        lock (ServiceDictionary)
            ServiceDictionary[typeof(TInterface)] = new(_ => service, isSingleton ? ServiceFlags.Singleton : ServiceFlags.Scoped);
    }

    #endregion

    #region Register Factory

    /// <summary>
    /// Registers or overrides a service of type <typeparamref name="TService"/> that will be constructed on each request
    /// </summary>
    /// <typeparam name="TService">The type of service to register</typeparam>
    /// <param name="service">The factory of the objects that offer the service</param>
    /// <param name="isSingleton">Whether this service should be treated as a singleton instance service or not</param>
    public void RegisterService<TService>(Func<ServiceCollection, TService> service, bool isSingleton = false) where TService : class
    {
        lock (ServiceDictionary)
            ServiceDictionary[typeof(TService)] = new(service, isSingleton ? ServiceFlags.Singleton : ServiceFlags.Scoped);
    }

    /// <summary>
    /// Registers or overrides a service of type <typeparamref name="TService"/> behind <typeparamref name="TInterface"/> that will be constructed on each request
    /// </summary>
    /// <typeparam name="TService">The type of service to register</typeparam>
    /// <typeparam name="TInterface">The type of service that abstract <paramref name="service"/> away</typeparam>
    /// <param name="service">The factory of the objects that offer the service</param>
    /// <param name="isSingleton">Whether this service should be treated as a singleton instance service or not</param>
    public void RegisterService<TInterface, TService>(Func<ServiceCollection, TService> service, bool isSingleton = false) where TInterface : class where TService : class, TInterface
    {
        lock (ServiceDictionary)
            ServiceDictionary[typeof(TInterface)] = new(service, isSingleton ? ServiceFlags.Singleton : ServiceFlags.Scoped);
    }

    #endregion

    #region Register Contextualized Factory

    /// <summary>
    /// Registers or overrides a service of type <typeparamref name="TService"/> that will be constructed on each request
    /// </summary>
    /// <remarks>
    /// If the Service implements <see cref="IDisposable"/>, this collection will automatically take care of disposing of it when its disposed itself; potentially in a different thread. Exceptions thrown by services being disposed are ignored.
    /// </remarks>
    /// <typeparam name="TService">The type of service to register</typeparam>
    /// <typeparam name="TContext">The type of the context to pass to the <typeparamref name="TService"/> object factory</typeparam>
    /// <param name="service">The factory of the objects that offer the service</param>
    /// <param name="context">The context to pass to <paramref name="service"/></param>
    /// <param name="isSingleton">Whether this service should be treated as a singleton instance service or not</param>
    public void RegisterService<TService, TContext>(Func<ServiceCollection, TContext, TService> service, TContext context, bool isSingleton = false) where TService : class
    {
        lock (ServiceDictionary)
            ServiceDictionary[typeof(TService)] = new(col => service(col, context), isSingleton ? ServiceFlags.Singleton : ServiceFlags.Scoped);
    }

    /// <summary>
    /// Registers or overrides a service of type <typeparamref name="TService"/> behind <typeparamref name="TInterface"/> that will be constructed on each request
    /// </summary>
    /// <typeparam name="TService">The type of service to register</typeparam>
    /// <typeparam name="TInterface">The type of service that abstract <paramref name="service"/> away</typeparam>
    /// <typeparam name="TContext">The type of the context to pass to the <typeparamref name="TService"/> object factory</typeparam>
    /// <remarks>
    /// If the Service implements <see cref="IDisposable"/>, this collection will automatically take care of disposing of it when its disposed itself; potentially in a different thread. Exceptions thrown by services being disposed are ignored.
    /// </remarks>
    /// <param name="service">The factory of the objects that offer the service</param>
    /// <param name="context">The context to pass to <paramref name="service"/></param>
    /// <param name="isSingleton">Whether this service should be treated as a singleton instance service or not</param>
    public void RegisterService<TInterface, TService, TContext>(Func<ServiceCollection, TContext, TService> service, TContext context, bool isSingleton = false) where TInterface : class where TService : class, TInterface
    {
        lock (ServiceDictionary)
            ServiceDictionary[typeof(TInterface)] = new(col => service(col, context), isSingleton ? ServiceFlags.Singleton : ServiceFlags.Scoped);
    }

    #endregion

    #region IDisposable

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (ServiceDictionary)
        {
            var servs = CollectionsMarshal.AsSpan(DisposableServices);
            for (int i = 0; i < servs.Length; i++)
                if (servs[i] is WeakReference<IDisposable> wr && wr.TryGetTarget(out var target))
                    try
                    {
                        target.Dispose();
                    }
                    catch { }
            DisposableServices.Clear();
        }
    }

    #endregion
}
