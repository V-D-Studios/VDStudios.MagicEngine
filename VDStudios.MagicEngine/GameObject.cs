﻿using System.Runtime.CompilerServices;
using Serilog;
using VDStudios.MagicEngine.Internal;
using VDStudios.MagicEngine.Logging;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents an object of the <see cref="Game"/>
/// </summary>
/// <remarks>
/// This class cannot be inherited directly by user code
/// </remarks>
public abstract class GameObject : IDisposable, IGameObject
{
    private object? ____sync;
    private readonly Lazy<ILogger> logSync;
    internal readonly string Facility;
    internal readonly string Area;

    internal object Sync => ____sync ??= new();

    /// <inheritdoc/>
    public string? Name { get; init; }

    /// <summary>
    /// Instances a new GameObject
    /// </summary>
    /// <param name="area">Logging information. The area the GameObject belongs to</param>
    /// <param name="facility">Logging information. The facility the GameObject operates for</param>
    /// <param name="game">The game this <see cref="GameObject"/> belongs to</param>
    internal GameObject(Game game, string facility, string area)
    {
        ArgumentException.ThrowIfNullOrEmpty(area);
        ArgumentException.ThrowIfNullOrEmpty(facility);
        logSync = new(InternalCreateLogger, LazyThreadSafetyMode.ExecutionAndPublication);
        Facility = facility;
        Game = game;
        Area = area;
        Random = Game.Random;
        GameDeferredCallSchedule = Game.DeferredCallSchedule;
    }

    /// <summary>
    /// A Logger that belongs to this <see cref="GameObject"/> and is attached to <see cref="Game.Logger"/>
    /// </summary>
    protected ILogger Log => logSync.Value;

    /// <summary>
    /// The RandomNumberGenerator for this <see cref="GameObject"/>
    /// </summary>
    /// <remarks>
    /// The very same one as <see cref="Game.Random"/>
    /// </remarks>
    protected Random Random { get; }

    /// <summary>
    /// The Game's <see cref="MagicEngine.DeferredExecutionSchedule"/>, can be used to defer calls in the update thread. Points to the same object as <see cref="Game.DeferredCallSchedule"/>
    /// </summary>
    /// <remarks>
    /// This schedule is updated every game frame, as such, it's subject to update rate drops and its maximum time resolution is <see cref="Game.UpdateFrameThrottle"/>
    /// </remarks>
    protected DeferredExecutionSchedule GameDeferredCallSchedule { get; }

    internal ILogger? InternalLog
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
#if FEATURE_INTERNAL_LOGGING
            lock (logSync)
                return __inlog ??= new GameLogger(Game.InternalLogger, Area, Facility, Name, GetType());
#else
            return null;
#endif
        }
    }

#if FEATURE_INTERNAL_LOGGING
    private GameLogger? __inlog;
#endif

    /// <summary>
    /// Creates a new <see cref="ILogger"/> for this <see cref="GameObject"/>
    /// </summary>
    /// <remarks>
    /// This method is used internally to lazyly initialize <see cref="Log"/>
    /// </remarks>
    /// <param name="gameLogger"></param>
    /// <param name="area"></param>
    /// <param name="facility"></param>
    /// <returns></returns>
    protected virtual ILogger CreateLogger(ILogger gameLogger, string area, string facility)
        => new GameLogger(gameLogger, area, facility, Name, GetType());

    /// <inheritdoc/>
    public Game Game { get; }

    private ILogger InternalCreateLogger()
        => CreateLogger(Game.Logger, Area, Facility);

    #region IDisposable

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> if this object is disposed at the time of this method being called
    /// </summary>
    /// <exception cref="ObjectDisposedException"></exception>
    protected internal void ThrowIfDisposed()
    {
        if (IsDisposed)
            throw new ObjectDisposedException(GetType().Name);
    }

    /// <inheritdoc/>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Disposes of this <see cref="GameObject"/> and any of its resources
    /// </summary>
    /// <remarks>
    /// Child classes looking to override this method should instead refer to <see cref="GameObject.Dispose(bool)"/>
    /// </remarks>
    public void Dispose()
    {
        InternalDispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Always call base.Dispose AFTER your own disposal code
    /// </summary>
    /// <param name="disposing"></param>
    internal virtual void InternalDispose(bool disposing)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        try
        {
            if (disposing)
                AboutToDispose?.Invoke(this, Game.TotalTime);
        }
        finally
        {
            Dispose(disposing);
            IsDisposed = true;
        }
    }

    /// <inheritdoc/>
    ~GameObject()
    {
        InternalDispose(false);
    }

    /// <summary>
    /// Runs when the object is being disposed. Don't call this! It'll be called automatically! Call <see cref="GameObject.Dispose()"/> instead
    /// </summary>
    /// <param name="disposing">Whether this method was called through <see cref="IDisposable.Dispose"/> or by the GC calling this object's finalizer</param>
    protected virtual void Dispose(bool disposing) { }

    /// <inheritdoc/>
    public event GeneralGameEvent<GameObject>? AboutToDispose;

    #endregion
}
