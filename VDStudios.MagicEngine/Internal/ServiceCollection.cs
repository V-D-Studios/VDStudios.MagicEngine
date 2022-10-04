using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.Internal;
internal sealed class ServiceCollection
{
    private readonly ServiceCollection? PreviousLayer;
    private readonly Dictionary<Type, object> ServiceDictionary;
    private readonly int layer;

    public ServiceCollection(ServiceCollection? previous)
    {
        PreviousLayer = previous;
        layer = previous is null ? 0 : previous.layer + 1;
        ServiceDictionary = new(previous?.ServiceDictionary.Count ?? 10);
    }

    private static void ThrowForNotFound<TService>() 
        => throw new KeyNotFoundException($"Could not find a service for type {typeof(TService)} at any point of the node tree");

    public TService? Get<TService>() where TService : class
    {
        if (!ServiceDictionary.TryGetValue(typeof(TService), out var value))
        {
            if (layer <= 0)
                ThrowForNotFound<TService>();
            var top = PreviousLayer;
            while (top is not null)
            {
                if (top.ServiceDictionary.TryGetValue(typeof(TService), out value))
                    return (TService)value;
                top = top.PreviousLayer;
            }
            ThrowForNotFound<TService>();
        }
        return (TService)value!;
    }

    public bool TryGet<TService>([NotNullWhen(true)] out TService? service) where TService : class
    {
        if (!ServiceDictionary.TryGetValue(typeof(TService), out var value))
        {
            if (layer <= 0)
            {
                service = null;
                return false;
            }
            var top = PreviousLayer;
            while (top is not null)
            {
                if (top.ServiceDictionary.TryGetValue(typeof(TService), out value))
                {
                    service = (TService)value;
                    return true;
                }
                top = top.PreviousLayer;
            }
            service = null;
            return false;
        }
        service = (TService)value;
        return true;
    }

    public void Set<TService>(TService service) where TService : class
        => ServiceDictionary[typeof(TService)] = service;

    public void Set<TInterface, TService>(TService service) where TInterface : class where TService : class
        => ServiceDictionary[typeof(TInterface)] = service;
}
