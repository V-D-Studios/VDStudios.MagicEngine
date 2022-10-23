using Veldrid;

namespace VDStudios.MagicEngine.Internal;

internal class ImGuiManager
{
    public readonly ImGuiController Controller;
    public readonly GraphicsManager OwnerManager;
    private readonly CommandList cl;

    private int widgCount;
    public int WidgetCount => widgCount;

    public void AddElement(GUIElement element, object? context = null)
    {
        element.AssignManager(OwnerManager);
        element.RegisterOnto(this, context);
    }

    public void ReportNewElement()
        => Interlocked.Increment(ref widgCount);

    public void ReportDeadElement()
        => Interlocked.Decrement(ref widgCount);

    public void BeginDraw(InputSnapshot snapshot, GraphicsDevice gd, TimeSpan delta)
    {
        if (GUIElements.Count > 0) // We check twice, as it may have changed between the first check and the lock being adquired
        {
            cl.Begin();
            cl.SetFramebuffer(gd.SwapchainFramebuffer); // Prepare for ImGUI
            using (Controller.Begin()) // Lock ImGUI from other GraphicsManagers
            {
                foreach (var element in GUIElements)
                    element.InternalSubmitUI(delta); // Submit UIs
                Controller.Update(1 / 60f, snapshot);
                Controller.Render(gd, cl); // Render
            }
            cl.End();
        }
    }

    public CommandList EndDraw()
        => cl;

    public GUIElementList GUIElements { get; } = new();

    public ImGuiManager(ImGuiController controller, CommandList cl, GraphicsManager owner)
    {
        Controller = controller;
        OwnerManager = owner;
        this.cl = cl;
    }
}
