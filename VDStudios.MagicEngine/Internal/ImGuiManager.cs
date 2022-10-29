using Veldrid;

namespace VDStudios.MagicEngine.Internal;

internal abstract class ImGuiManager : IDisposable
{
    public readonly ImGuiController Controller;
    public readonly GraphicsManager OwnerManager;
    protected readonly CommandList cl;

    public bool IsActive { get; private set; } = true;

    private int widgCount;
    public int WidgetCount => widgCount;

    public void AddElement(ImGuiElement element, object? context = null)
    {
        element.AssignManager(OwnerManager);
        element.RegisterOnto(this, context);
    }

    public void ReportNewElement()
        => Interlocked.Increment(ref widgCount);

    public void ReportDeadElement()
        => Interlocked.Decrement(ref widgCount);

    public abstract void BeginDraw(InputSnapshot snapshot, GraphicsDevice gd, TimeSpan delta);

    public abstract ValueTask<CommandList> EndDraw();

    public GUIElementList GUIElements { get; } = new();

    protected void Draw(InputSnapshot snapshot, GraphicsDevice gd, TimeSpan delta)
    {
        cl.Begin();
        cl.SetFramebuffer(gd.SwapchainFramebuffer); // Prepare for ImGUI
        using (Controller.Begin()) // Lock ImGUI from other GraphicsManagers
        {
            foreach (var element in GUIElements)
                element.InternalSubmitUI(delta); // Submit UIs
            Controller.Update((float)delta.TotalSeconds, snapshot);
            Controller.Render(gd, cl); // Render
        }
        cl.End();
    }

    public ImGuiManager(ImGuiController controller, CommandList cl, GraphicsManager owner)
    {
        Controller = controller;
        OwnerManager = owner;
        this.cl = cl;
    }

    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        IsActive = false;
        if (!disposedValue)
        {
            if (disposing)
            {
                Controller.Dispose();
                cl.Dispose();
            }
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    ~ImGuiManager()
    {
        IsActive = false;
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        IsActive = false;
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

internal class ForegroundImGuiManager : ImGuiManager
{
    public ForegroundImGuiManager(ImGuiController controller, CommandList cl, GraphicsManager owner) : base(controller, cl, owner) { }
    private (InputSnapshot snapshot, GraphicsDevice gd, TimeSpan delta) dpar;

    public override void BeginDraw(InputSnapshot snapshot, GraphicsDevice gd, TimeSpan delta)
    {
        dpar = (snapshot, gd, delta);
    }

    public override ValueTask<CommandList> EndDraw()
    {
        var (sn, gd, d) = dpar;
        Draw(sn, gd, d);
        sn.ReturnToPool();
        return ValueTask.FromResult(cl);
    }
}

internal class BackgroundThreadImGuiManager : ImGuiManager
{
    private readonly Thread drtr;
    private readonly SemaphoreSlim sem;
    private (InputSnapshot snapshot, GraphicsDevice gd, TimeSpan delta) dpar;

    public BackgroundThreadImGuiManager(ImGuiController controller, CommandList cl, GraphicsManager owner) : base(controller, cl, owner) 
    {
        drtr = drtr = new Thread(DrawThreadMethod);
        sem = new(0, 1);
        drtr.Start();
    }

    public override void BeginDraw(InputSnapshot snapshot, GraphicsDevice gd, TimeSpan delta)
    {
        dpar = (snapshot, gd, delta);
        sem.Release();
    }

    public override async ValueTask<CommandList> EndDraw()
    {
        if (!sem.Wait(50))
            await sem.WaitAsync();
        return cl;
    }

    private void DrawThreadMethod()
    {
        while (IsActive)
        {
            try
            {
                sem.Wait();
                try
                {
                    var (sn, gd, d) = dpar;
                    Draw(sn, gd, d);
                    sn.ReturnToPool();
                }
                finally
                {
                    sem.Release();
                }
            }
            catch 
            {
#if DEBUG
                throw;
#endif
            }
        }
    }
}