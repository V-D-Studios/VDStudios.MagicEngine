using System.Windows;
using VDStudios.MagicEngine.ResourceTool.GUI.Contexts;

namespace VDStudios.MagicEngine.ResourceTool.GUI;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = MainWindowContext.Instance;
    }
}
