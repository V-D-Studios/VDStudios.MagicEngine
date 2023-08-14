using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDStudios.MagicEngine.DrawLibrary.Geometry;
using VDStudios.MagicEngine.Graphics;
using Veldrid;
using Veldrid.SPIRV;

namespace VDStudios.MagicEngine.Internal;
/// <summary>
/// Provides a means of caching default resources for a specific <see cref="GraphicsManager"/>
/// </summary>
/// <remarks>
/// This class cannot be inherited or instantiated by outside code
/// </remarks>
public sealed class DefaultResourceCache
{
    private sealed class DRC_locker : IDisposable
    {
        private readonly DefaultResourceCache Parent;
        private readonly ReaderWriterLockSlim rwl;
        public DRC_locker(DefaultResourceCache parent, ReaderWriterLockSlim _rwl)
        {
            Parent = parent;
            rwl = _rwl;
        }

        public DRC_locker Lock()
        {
            rwl.EnterWriteLock();
            return this;
        }

        public bool Lock(TimeSpan time, out DRC_locker self)
        {
            self = this;
            return rwl.TryEnterWriteLock(time);
        }

        public bool Lock(int ms, out DRC_locker self)
        {
            self = this;
            return rwl.TryEnterWriteLock(ms);
        }

        public void Dispose()
        {
            rwl.ExitWriteLock();
        }
    }                     

    private readonly GraphicsManager Manager;
    private readonly ReaderWriterLockSlim rwl = new(LockRecursionPolicy.NoRecursion);
    private readonly DRC_locker locker;
    internal DefaultResourceCache(GraphicsManager manager)
    {
        Manager = manager;
        locker = new(this, rwl);
    }

    #region locking

    /// <summary>
    /// Locks this cache to allow for multiple writes before the cache can be used again
    /// </summary>
    /// <remarks>
    /// *ALWAYS* remember to dispose of this lock after finishing using it!
    /// </remarks>
    public IDisposable AdquireLock()
        => locker.Lock();

    /// <summary>
    /// Locks this cache to allow for multiple writes before the cache can be used again
    /// </summary>
    /// <remarks>
    /// *ALWAYS* remember to dispose of this lock after finishing using it!
    /// </remarks>
    /// <returns><see langword="true"/> if the lock was succesfully adquired within <paramref name="milliseconds"/>, <see langword="false"/> otherwise</returns>
    public bool AdquireLock(int milliseconds, [NotNullWhen(true)] out IDisposable? cacheLock)
    {
        if (locker.Lock(milliseconds, out var __locker))
        {
            cacheLock = __locker;
            return true;
        }
        else
        {
            cacheLock = null;
            return false;
        }
    }

    /// <summary>
    /// Locks this cache to allow for multiple writes before the cache can be used again
    /// </summary>
    /// <remarks>
    /// *ALWAYS* remember to dispose of this lock after finishing using it!
    /// </remarks>
    /// <returns><see langword="true"/> if the lock was succesfully adquired within <paramref name="timeout"/>, <see langword="false"/> otherwise</returns>
    public bool AdquireLock(TimeSpan timeout, [NotNullWhen(true)] out IDisposable? cacheLock)
    {
        if (locker.Lock(timeout, out var __locker))
        {
            cacheLock = __locker;
            return true;
        }
        else
        {
            cacheLock = null;
            return false;
        }
    }

    #endregion

    #region Shaders

    #region ShapeRenderer

    /// <summary>
    /// The default <see cref="ShapeRenderer{TVertex}"/> shaders
    /// </summary>
    public Shader[] DefaultShapeRendererShaders
    {
        get
        {
            rwl.EnterReadLock();
            try
            {
                return _srshaders ??= Manager.Device!.ResourceFactory.CreateFromSpirv(srvertexDefault, srfragmnDefault);
            }
            finally
            {
                rwl.ExitReadLock();
            }
        }

        set
        {
            ArgumentNullException.ThrowIfNull(value);
            rwl.EnterWriteLock();
            try
            {
                _srshaders = value;
            }
            finally
            {
                rwl.ExitWriteLock();
            }
        }
    }
    private Shader[]? _srshaders;

    /// <summary>
    /// The default vertex layout for <see cref="DefaultShapeRendererShaders"/>
    /// </summary>
    public VertexLayoutDescription DefaultShapeRendererLayout
    {
        get
        {
            rwl.EnterReadLock();
            try
            {
                return defaultShapeRendererLayout;
            }
            finally
            {
                rwl.ExitReadLock();
            }
        }
        set
        {
            rwl.EnterWriteLock();
            try
            {
                defaultShapeRendererLayout = value;
            }
            finally
            {
                rwl.ExitWriteLock();
            }
        }
    }
    private VertexLayoutDescription defaultShapeRendererLayout = new(new VertexElementDescription("Position", VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate));

    private static ShaderDescription srvertexDefault = new(ShaderStages.Vertex, DefaultShaders.DefaultShapeRendererVertexShader.GetUTF8Bytes(), "main");
    private static ShaderDescription srfragmnDefault = new(ShaderStages.Fragment, DefaultShaders.DefaultShapeRendererFragmentShader.GetUTF8Bytes(), "main");

    #endregion

    #region TexturedShapeRenderer

    /// <summary>
    /// The default <see cref="ShapeRenderer{TVertex}"/> shaders
    /// </summary>
    public Shader[] DefaultTexturedShapeRendererShaders
    {
        get
        {
            rwl.EnterReadLock();
            try
            {
                return _txtsrshaders ??= Manager.Device!.ResourceFactory.CreateFromSpirv(txtsrvertexDefault, txtsrfragmnDefault);
            }
            finally
            {
                rwl.ExitReadLock();
            }
        }

        set
        {
            ArgumentNullException.ThrowIfNull(value);
            rwl.EnterWriteLock();
            try
            {
                _txtsrshaders = value;
            }
            finally
            {
                rwl.ExitWriteLock();
            }
        }
    }
    private Shader[]? _txtsrshaders;

    /// <summary>
    /// The default vertex layout for <see cref="DefaultTexturedShapeRendererShaders"/>
    /// </summary>
    public VertexLayoutDescription DefaultTexturedShapeRendererLayout
    {
        get
        {
            rwl.EnterReadLock();
            try
            {
                return defaultTexturedShapeRendererLayout;
            }
            finally
            {
                rwl.ExitReadLock();
            }
        }
        set
        {
            rwl.EnterWriteLock();
            try
            {
                defaultTexturedShapeRendererLayout = value;
            }
            finally
            {
                rwl.ExitWriteLock();
            }
        }
    }
    private VertexLayoutDescription defaultTexturedShapeRendererLayout = new(
                new VertexElementDescription("TexturePosition", VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate),
                new VertexElementDescription("Position", VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate)
              );

    private static ShaderDescription txtsrvertexDefault = new(ShaderStages.Vertex, DefaultShaders.DefaultTexturedShapeRendererVertexShader.GetUTF8Bytes(), "main");
    private static ShaderDescription txtsrfragmnDefault = new(ShaderStages.Fragment, DefaultShaders.DefaultTexturedShapeRendererFragmentShader.GetUTF8Bytes(), "main");

    #endregion

    #endregion
}
