using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Game.UI.ViewModel;

namespace Game.UI;

public class App : Application
{
   

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            throw new Exception("IClassicDesktopStyleApplicationLifetime is not supported");

        desktop.MainWindow = new MainWindow(new MainViewModel());


        base.OnFrameworkInitializationCompleted();
    }

    public class SecretData
    {
        public required string RegistryPassword { get; set; }
    }
}