using ImGuiNET;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using VDStudios.MagicEngine.Graphics;

namespace VDStudios.MagicEngine.GUILibrary.ImGUI;

/// <summary>
/// Represents a list of delegates that return strings to be submitted into the UI
/// </summary>
public class Watch : ImGUIElement
{
    /// <summary>
    /// An object that polls for data to be viewed on the watch
    /// </summary>
    public abstract class Viewer
    {
        /// <summary>
        /// Polls the data to be displayed onto the watch
        /// </summary>
        /// <param name="data">The data that has been obtained and formatted. The method is responsible for caching and optimizing</param>
        /// <returns><c>true</c> if the viewer should remain in the list, <c>false</c> otherwise</returns>
        public abstract bool Poll([NotNullWhen(true)] out string? data);
    }

    /// <summary>
    /// A <see cref="Viewer"/> that invokes a delegate when polled
    /// </summary>
    public sealed class DelegateViewer : Viewer
    {
        /// <summary>
        /// Polls the data to be displayed onto the watch
        /// </summary>
        /// <param name="data">The data that has been obtained and formatted. The method is responsible for caching and optimizing</param>
        /// <returns><c>true</c> if the viewer should remain in the list, <c>false</c> otherwise</returns>
        public delegate bool ViewMethod([NotNullWhen(true)] out string? data);

        private readonly ViewMethod meth;
        
        /// <summary>
        /// Instances a new object of type <see cref="DelegateViewer"/> 
        /// </summary>
        /// <param name="method"></param>
        public DelegateViewer(ViewMethod method)
        {
            ArgumentNullException.ThrowIfNull(method);
            meth = method;
        }

        /// <inheritdoc/>
        public override bool Poll([NotNullWhen(true)] out string? data) => meth(out data);
    }

    /// <summary>
    /// Represents a method that can be called whenever a user presses a button in the watch, to log relevant data
    /// </summary>
    /// <returns><c>true</c> if the viewer should remain in the list, <c>false</c> otherwise</returns>
    public delegate bool ViewLogger();

    /// <summary>
    /// The title of this <see cref="Watch"/>
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Instances a new object of type <see cref="Watch"/>
    /// </summary>
    /// <param name="title">The title of the window</param>
    /// <param name="viewers">The viewers for this watch</param>
    /// <param name="viewLoggers">The view loggers for this watch</param>
    public Watch(string title = "Watch", List<Viewer>? viewers = null, List<(string title, ViewLogger logger)>? viewLoggers = null)
    {
        Title = title;
        Viewers = viewers ?? new();
        ViewLoggers = viewLoggers ?? new();
    }

    /// <summary>
    /// The list of data viewers
    /// </summary>
    public List<Viewer> Viewers { get; }

    /// <summary>
    /// The list of data loggers
    /// </summary>
    public List<(string title, ViewLogger logger)> ViewLoggers { get; }

    private readonly Queue<int> ViewerRemovals = new();
    private readonly Queue<int> LoggerRemovals = new();

    /// <inheritdoc/>
    protected override void SubmitUI(TimeSpan delta, IReadOnlyCollection<ImGUIElement> subElements)
    {
        ImGui.Begin(Title);
        var viewers = CollectionsMarshal.AsSpan(Viewers);
        for (int i = 0; i < viewers.Length; i++) 
        {
            if (!viewers[i].Poll(out var dat))
            {
                ViewerRemovals.Enqueue(i);
                continue;
            }
            ImGui.Text(dat);
        }

        if(ViewLoggers.Count > 0)
        {
            var loggers = CollectionsMarshal.AsSpan(ViewLoggers);
            for (int i = 0; i < loggers.Length; i++)
            {
                var (t, l) = loggers[i];
                if (ImGui.Button(t) && !l.Invoke())
                    LoggerRemovals.Enqueue(i);
            }
        }

        while (ViewerRemovals.Count > 0)
            Viewers.RemoveAt(ViewerRemovals.Dequeue());

        while (LoggerRemovals.Count > 0)
            ViewLoggers.RemoveAt(LoggerRemovals.Dequeue());

        ImGui.End();
    }
}
