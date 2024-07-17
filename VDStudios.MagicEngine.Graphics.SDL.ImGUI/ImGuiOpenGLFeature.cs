using ImGuiNET;
using SDL2.NET;
using VDStudios.MagicEngine.Input;
using VDStudios.MagicEngine.Extensions.ImGuiExtension;
using System.Diagnostics;

namespace VDStudios.MagicEngine.Graphics.SDL.ImGUI;

public sealed class ImGuiOpenGLFeature : IGraphicsManagerFeature<SDLGraphicsContext>
{
    private readonly List<ImGUIElement> imGUIElementBuffer = [];
    private ImGUIElementList? ImGUIElements;
    private ImGuiGLRenderer? imGuiController;
    private SDLOpenGLGraphicsManager? manager;

    private readonly static object ImGuiSync = new();

    public void FeatureAdded(GraphicsManager<SDLGraphicsContext> graphicsManager) { }

    public void ManagerStarting(GraphicsManager<SDLGraphicsContext> graphicsManager)
    {
        if (imGuiController is not null)
            throw new InvalidOperationException("Cannot add this feature object twice");

        if (graphicsManager is not SDLOpenGLGraphicsManager sdl)
            throw new ArgumentException("The manager must be of type SDLGraphicsManager", nameof(graphicsManager));

        imGuiController = new ImGuiGLRenderer(((IHandle)sdl.Window).Handle, sdl.OpenGLContextPointer);
        ImGUIElements = new(graphicsManager);
        manager = sdl;
    }

    public void Input(InputSnapshot input)
    {
        Debug.Assert(imGuiController is not null);

        for (int i = 0; i < input.KeyEvents.Count; i++)
            imGuiController.ProcessEvent(input.KeyEvents[i]);

        for (int i = 0; i < input.MouseEvents.Count; i++)
            imGuiController.ProcessEvent(input.MouseEvents[i]);

        for (int i = 0; i < input.MouseWheelEvents.Count; i++)
            imGuiController.ProcessEvent(input.MouseWheelEvents[i]);

        for (int i = 0; i < input.TextInputEvents.Count; i++)
            imGuiController.ProcessEvent(input.TextInputEvents[i]);
    }

    public void Rendering(TimeSpan delta)
    {
        Debug.Assert(imGuiController is not null);
        Debug.Assert(ImGUIElements is not null);
        Debug.Assert(manager is not null);

        lock (ImGUIElements.sync)
        {
            imGUIElementBuffer.Clear();
            foreach (var el in ImGUIElements)
                if (el.IsActive)
                    imGUIElementBuffer.Add(el);
        }

        lock (ImGuiSync)
        {
            ImGui.NewFrame();
            imGuiController.NewFrame();

            for (int i = 0; i < imGUIElementBuffer.Count; i++)
                imGUIElementBuffer[i].SubmitUI(delta, manager);

            ImGui.EndFrame();
            imGuiController.Render();
        }
    }

    public void Dispose()
    {
        imGuiController?.Dispose();
    }
}
