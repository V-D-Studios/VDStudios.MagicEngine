using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace VDStudios.MagicEngine.Services;

/// <summary>
/// Represents a scope that manages <see cref="ServiceLifetime.Transient"/> and <see cref="ServiceLifetime.Scoped"/> services from a <see cref="ServiceCollection"/>
/// </summary>
public sealed class ServiceScope : ServiceCollection, IDisposable
{
    private readonly ServiceCollection serviceCollection;
    private readonly List<object>? services;
    private readonly List<IDisposable>? disposables;
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

    internal ServiceScope(ServiceCollection collection) : base(collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        serviceCollection = collection;
    }

    private TService VerifyService<TService>(ServiceInfo info) where TService : class
        => (TService)VerifyService(info);

    private object VerifyService(ServiceInfo info)
        => info.Type is ServiceLifetime.Scoped or ServiceLifetime.Transient
            ? throw new InvalidOperationException("Scoped and Transient services must be obtained from a ServiceScope")
            : info.Factory(this);

    void IDisposable.Dispose()
        => Dispose();

    /// <inheritdoc/>
    public new void Dispose()
    {
        AssertEnabled();
        ThrowIfDisabled();

        enabled = false;
        if (disposables is null || disposables.Count is 0) return;

        List<Exception>? excs = null;
        foreach (var disp in disposables)
            try
            {
                disp.Dispose();
            }
            catch (Exception e)
            {
                (excs ??= new()).Add(e);
            }
    }

    internal override object VerifyService(ServiceInfo info)
    {
        throw new NotImplementedException();
    }

    internal override ServiceInfo InternalGetService(Type type)
    {
        throw new NotImplementedException();
    }

    internal override bool InternalTryGetService(Type type, [MaybeNullWhen(false), NotNullWhen(true)] out ServiceInfo info)
    {
        throw new NotImplementedException();
    }
}
