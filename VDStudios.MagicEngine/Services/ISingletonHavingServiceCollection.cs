using System.Diagnostics;

namespace VDStudios.MagicEngine.Services;

internal interface ISingletonHavingServiceCollection
{
    public Dictionary<Type, object> InstantiatedSingletons { get; }

    public ServiceCollection ThisAsServiceCollection()
    {
        Debug.Assert(this is ServiceCollection, $"Interface implementer {GetType()} is unexpectedly not a ServiceCollection");
        return (ServiceCollection)this;
    }

    public object FetchSingleton(ServiceInfo info)
    {
        lock (InstantiatedSingletons)
            if (InstantiatedSingletons.TryGetValue(info.Type, out var obj))
                return obj;
            else
            {
                var o = info.Factory(info.Type, ThisAsServiceCollection());
                InstantiatedSingletons.Add(info.Type, o);
                return o;
            }
    }
}
