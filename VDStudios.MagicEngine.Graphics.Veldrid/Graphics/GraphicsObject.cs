namespace VDStudios.MagicEngine.Graphics.Veldrid.Graphics;

/// <summary>
/// Represents the base class for graphical operations, such as <see cref="DrawOperation"/>, <see cref="GUIElement"/>
/// </summary>
/// <remarks>
/// This class cannot be instanced or inherited by user code
/// </remarks>
public abstract class GraphicsObject : GameObject
{
    internal GraphicsObject(string facility) : base(facility, "Rendering")
    {
        ReadySemaphore = new(0, 1);
    }

    /// <summary>
    /// This <see cref="DrawOperation"/>'s unique identifier, generated automatically
    /// </summary>
    public Guid Identifier { get; } = Guid.NewGuid();

    /// <summary>
    /// The <see cref="GraphicsManager"/> this operation is registered onto
    /// </summary>
    /// <remarks>
    /// Will be null if this operation is not registered
    /// </remarks>
    public GraphicsManager? Manager { get; private set; }

    internal void NotifyIsReady() => IsReady = true;

    private bool isRegistered = false;

    internal void AssignManager(GraphicsManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        lock (ReadySemaphore)
        {
            if (isRegistered)
                throw new InvalidOperationException("This GraphicsObject is already registered on a GraphicsManager");
            isRegistered = true;
            Manager = manager;
        }
    }

    internal void VerifyManager(GraphicsManager manager)
    {
        lock (ReadySemaphore)
        {
            if (isRegistered is false)
                throw new InvalidOperationException("This GraphicsObject was not properly assigned a GraphicsManager");
            if (!ReferenceEquals(manager, Manager))
                throw new InvalidOperationException("Cannot register a GraphicsObject under a different GraphicsManager than it was first queued to. This is likely a library bug.");
        }
    }

    /// <summary>
    /// <c>true</c> when the node has been added to the scene tree and initialized
    /// </summary>
    public bool IsReady
    {
        get => _isReady;
        private set
        {
            if (value == _isReady) return;
            if (value)
                ReadySemaphore.Release();
            else
                ReadySemaphore.Wait();
            _isReady = value;
        }
    }
    private bool _isReady;
    private readonly SemaphoreSlim ReadySemaphore;

    /// <summary>
    /// Asynchronously waits until the Node has been added to the scene tree and is ready to be used
    /// </summary>
    public async ValueTask WaitUntilReadyAsync()
    {
        if (IsReady)
            return;
        if (ReadySemaphore.Wait(15))
        {
            ReadySemaphore.Release();
            return;
        }

        while (!await ReadySemaphore.WaitAsync(50))
            await Manager!.AwaitIfFaulted();
        ReadySemaphore.Release();
    }

    /// <summary>
    /// Waits until the Node has been added to the scene tree and is ready to be used
    /// </summary>
    public void WaitUntilReady()
    {
        if (IsReady)
            return;
        while (!ReadySemaphore.Wait(50))
        {
            var t = Manager!.AwaitIfFaulted();
            if (t.IsCompleted)
                t.GetAwaiter().GetResult();
        }
        ReadySemaphore.Release();
    }

    /// <summary>
    /// Waits until the Node has been added to the scene tree and is ready to be used
    /// </summary>
    public bool WaitUntilReady(int timeoutMilliseconds)
    {
        if (IsReady)
            return true;
        ValueTask t;
        if (ReadySemaphore.Wait(timeoutMilliseconds))
        {
            t = Manager!.AwaitIfFaulted();
            if (t.IsCompleted)
                t.GetAwaiter().GetResult();

            ReadySemaphore.Release();
            return true;
        }

        t = Manager!.AwaitIfFaulted();
        if (t.IsCompleted)
            t.GetAwaiter().GetResult();
        return false;
    }

    /// <summary>
    /// Asynchronously waits until the Node has been added to the scene tree and is ready to be used
    /// </summary>
    public async ValueTask<bool> WaitUntilReadyAsync(int timeoutMilliseconds)
    {
        if (IsReady)
            return true;
        if (timeoutMilliseconds > 15)
        {
            if (ReadySemaphore.Wait(15))
            {
                await Manager!.AwaitIfFaulted();
                ReadySemaphore.Release();
                return true;
            }

            if (await ReadySemaphore.WaitAsync(timeoutMilliseconds - 15))
            {
                await Manager!.AwaitIfFaulted();
                ReadySemaphore.Release();
                return true;
            }
        }
        if (await ReadySemaphore.WaitAsync(timeoutMilliseconds))
        {
            await Manager!.AwaitIfFaulted();
            ReadySemaphore.Release();
            return true;
        }
        await Manager!.AwaitIfFaulted();
        return false;
    }
}
