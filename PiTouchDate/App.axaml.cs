using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PiTouchDate.Services;
using PiTouchDate.ViewModels;
using PiTouchDate.Views;
using Splat;

namespace PiTouchDate;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        RegisterServices(Locator.CurrentMutable);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void RegisterServices(IMutableDependencyResolver services)
    {
        var configurationService = new ConfigurationService();
        services.RegisterConstant<ConfigurationService>(configurationService);
    }
}