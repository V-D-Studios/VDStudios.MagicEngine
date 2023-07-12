using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.ResourceTool.GUI.Contexts;
public class MainWindowContext
{
    public MainWindowContext() { }

    public static string WindowTitle { get; } = "Magic Engine Resource Tool";

    public static string CopyrightNotice { get; } = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? throw new Exception("The current assembly does not have a copyright notice");

    public static string AppTitle { get; } = "Magic Engine Resource Tool";

    public static Version AppVersion { get; } = Assembly.GetExecutingAssembly().GetName().Version ?? throw new Exception("The current assembly does not have a version");

    public static string AppVersionString { get; } = AppVersion.ToString();

    public static string AppAuthors { get; } = "Diego García";

    public static Version MagicEngineVersion { get; } = typeof(Game).Assembly.GetName().Version ?? throw new Exception("The MagicEngine assembly does not have a version");

    public static string MagicEngineVersionString { get; } = MagicEngineVersion.ToString();

    public static MainWindowContext Instance { get; } = new();
}
