using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;
using MyPcGuard.Services.Common;
using MyPcGuard.Services.Linux;
using MyPcGuard.Services.Mac;
using MyPcGuard.Services.Windows;
using MyPcGuard.ViewModels;
using MyPcGuard.Views;

namespace MyPcGuard;

public partial class App : Application
{
    private ServiceProvider? serviceProvider;
    private MainWindow? mainWindow;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            serviceProvider = ConfigureServices();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var viewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();

                mainWindow = new MainWindow
                {
                    DataContext = viewModel,
                    ShowInTaskbar = true,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    WindowState = WindowState.Normal
                };
                desktop.MainWindow = mainWindow;
            }
        }
        catch (Exception ex)
        {
            WriteStartupLog(ex);
            throw;
        }

        base.OnFrameworkInitializationCompleted();

        if (mainWindow is not null)
        {
            Dispatcher.UIThread.Post(() =>
            {
                mainWindow.Show();
                mainWindow.Activate();
            });
        }
    }

    private static void WriteStartupLog(Exception ex)
    {
        try
        {
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "mypcguard-startup-error.log"), ex.ToString());
        }
        catch
        {
        }
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IRiskEngine, RiskEngine>();
        services.AddSingleton<IScanOrchestrator, ScanOrchestrator>();
        services.AddSingleton<IReportGenerator, HtmlReportGenerator>();
        services.AddSingleton<IConfirmationDialogService, ConfirmationDialogService>();
        services.AddSingleton<IUserSettingsService, UserSettingsService>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<IActionHistoryService, ActionHistoryService>();
        services.AddSingleton<IQuarantineService, QuarantineService>();
        services.AddTransient<MainWindowViewModel>();

        RegisterPlatformServices(services, PlatformDetector.Detect());

        return services.BuildServiceProvider();
    }

    private static void RegisterPlatformServices(IServiceCollection services, OperatingSystemType operatingSystem)
    {
        switch (operatingSystem)
        {
            case OperatingSystemType.Windows:
                services.AddSingleton<ISystemInfoService, WindowsSystemInfoService>();
                services.AddSingleton<IProcessScanner, WindowsProcessScanner>();
                services.AddSingleton<IStartupScanner, WindowsStartupScanner>();
                services.AddSingleton<IAutostartActionService, WindowsAutostartActionService>();
                services.AddSingleton<IServiceScanner, WindowsServiceScanner>();
                services.AddSingleton<INetworkScanner, WindowsNetworkScanner>();
                services.AddSingleton<IDefenderScanner, WindowsDefenderScanner>();
                services.AddSingleton<IDefenderActionService, WindowsDefenderActionService>();
                break;
            case OperatingSystemType.Linux:
                services.AddSingleton<ISystemInfoService, LinuxSystemInfoService>();
                services.AddSingleton<IProcessScanner, LinuxProcessScanner>();
                services.AddSingleton<IStartupScanner, LinuxStartupScanner>();
                services.AddSingleton<IAutostartActionService, UnsupportedAutostartActionService>();
                services.AddSingleton<IServiceScanner, LinuxServiceScanner>();
                services.AddSingleton<INetworkScanner, LinuxNetworkScanner>();
                services.AddSingleton<IDefenderScanner, LinuxDefenderScanner>();
                services.AddSingleton<IDefenderActionService, UnsupportedDefenderActionService>();
                break;
            case OperatingSystemType.MacOS:
                services.AddSingleton<ISystemInfoService, MacSystemInfoService>();
                services.AddSingleton<IProcessScanner, MacProcessScanner>();
                services.AddSingleton<IStartupScanner, MacStartupScanner>();
                services.AddSingleton<IAutostartActionService, UnsupportedAutostartActionService>();
                services.AddSingleton<IServiceScanner, MacServiceScanner>();
                services.AddSingleton<INetworkScanner, MacNetworkScanner>();
                services.AddSingleton<IDefenderScanner, MacDefenderScanner>();
                services.AddSingleton<IDefenderActionService, UnsupportedDefenderActionService>();
                break;
            default:
                services.AddSingleton<ISystemInfoService, LinuxSystemInfoService>();
                services.AddSingleton<IProcessScanner, LinuxProcessScanner>();
                services.AddSingleton<IStartupScanner, LinuxStartupScanner>();
                services.AddSingleton<IAutostartActionService, UnsupportedAutostartActionService>();
                services.AddSingleton<IServiceScanner, LinuxServiceScanner>();
                services.AddSingleton<INetworkScanner, LinuxNetworkScanner>();
                services.AddSingleton<IDefenderScanner, LinuxDefenderScanner>();
                services.AddSingleton<IDefenderActionService, UnsupportedDefenderActionService>();
                break;
        }
    }
}
