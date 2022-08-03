using ImGuiNET;
using SDL2.NET;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.GUILibrary.ImGUI;

/// <summary>
/// Represents a list of delegates that return strings to be submitted into the UI
/// </summary>
public class Watch : GUIElement
{
    /// <summary>
    /// Represents a method that polls for data to be viewed on the watch
    /// </summary>
    /// <param name="data">The data that has been obtained and formatted. The method is responsible for caching and optimizing</param>
    /// <returns><c>true</c> if the viewer should remain in the list, <c>false</c> otherwise</returns>
    public delegate bool Viewer([NotNullWhen(true)] out string? data);

    /// <summary>
    /// The title of this <see cref="Watch"/>
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Instances a new object of type <see cref="Watch"/>
    /// </summary>
    /// <param name="title">The title of the window</param>
    /// <param name="viewers">The viewer methods for this watch</param>
    public Watch(string title = "Watch", List<Viewer>? viewers = null)
    {
        Title = title;
        Viewers = viewers ?? new();
    }

    /// <summary>
    /// The list of data viewers
    /// </summary>
    public List<Viewer> Viewers { get; }

    private readonly Queue<int> Removals = new();

    /// <inheritdoc/>
    protected override void SubmitUI(TimeSpan delta, IReadOnlyCollection<GUIElement> subElements)
    {
        ImGui.Begin(Title);
        var span = CollectionsMarshal.AsSpan(Viewers);
        for (int i = 0; i < span.Length; i++) 
        {
            if (!span[i].Invoke(out var dat))
            {
                Removals.Enqueue(i);
                continue;
            }
            ImGui.Text(dat);
        }
        ImGui.End();
    }
}
