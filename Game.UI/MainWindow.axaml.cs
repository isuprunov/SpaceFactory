using Avalonia.Controls;

namespace Game.UI;

public partial class MainWindow : Window
{
    private MainWindow() => InitializeComponent();

    public MainWindow(object? dataContext) : this() => DataContext = dataContext;
}