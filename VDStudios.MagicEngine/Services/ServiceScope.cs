﻿using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace VDStudios.MagicEngine.Services;

/// <summary>
/// Represents a scope that manages <see cref="ServiceLifetime.Transient"/> and <see cref="ServiceLifetime.Scoped"/> services from a <see cref="ServiceCollection"/>
/// </summary>
public sealed class ServiceScope : ServiceCollection, IDisposable
{
    private readonly ISingletonHavingServiceCollection serviceCollection;
    private readonly ConcurrentDictionary<Type, object> scopeds = new();
    private readonly List<IDisposable> disposables = new();
    private bool enabled;

    internal void Enable()
        => enabled = true;

    private void AssertEnabled()
        => Debug.Assert(enabled, "This ServiceScope was unexpectedly disabled");

    private void ThrowIfDisabled()
    {
        if (enabled is false)
            throw new ObjectDisposedException(nameof(ServiceScope));
    }

    internal ServiceScope(ISingletonHavingServiceCollection collection) : base(collection.ThisAsServiceCollection())
    {
        ArgumentNullException.ThrowIfNull(collection);
        serviceCollection = collection;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        AssertEnabled();
        ThrowIfDisabled();

        enabled = false;
        if (disposables is null || disposables.Count is 0) return;

        List<Exception>? excs = null;
        lock (disposables) 
            foreach (var disp in disposables)
                try
                {
                    disp.Dispose();
                }
                catch (Exception e)
                {
                    (excs ??= new()).Add(e);
                }

        disposables.Clear();
        scopeds.Clear();
        if (excs is not null && excs.Count > 0)
            throw new AggregateException(excs);
    }

    internal override object VerifyService(ServiceInfo info)
    {
        object service = info.Lifetime == ServiceLifetime.Scoped
            ? scopeds.GetOrAdd(info.Type, info.Factory, this)
            : info.Lifetime == ServiceLifetime.Singleton
            ? serviceCollection.FetchSingleton(info)
            : info.Lifetime == ServiceLifetime.Transient
            ? info.Factory(info.Type, this)
            : throw new InvalidOperationException($"Unknown ServiceLifetime {info.Lifetime}");

        if (service is IDisposable disp)
            lock (disposables)
                disposables.Add(disp);

        return service;
    }

    internal override ServiceInfo InternalGetService(Type type)
        => serviceCollection.ThisAsServiceCollection().InternalGetService(type);

    internal override bool InternalTryGetService(Type type, [MaybeNullWhen(false), NotNullWhen(true)] out ServiceInfo info)
        => serviceCollection.ThisAsServiceCollection().InternalTryGetService(type, out info);
}
