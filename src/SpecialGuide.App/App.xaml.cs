using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SpecialGuide.Core.Models;
using SpecialGuide.Core.Services;

namespace SpecialGuide.App;

public partial class App : Application
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<Overlay.RadialMenuWindow>();
                services.AddSingleton<IRadialMenu>(sp => sp.GetRequiredService<Overlay.RadialMenuWindow>());
                services.AddSingleton<HookService>();
                services.AddSingleton<OverlayService>();
                services.AddSingleton<RadialMenuService>();
                services.AddSingleton<CaptureService>();
                services.AddSingleton<OpenAIService>();
                services.AddSingleton<AudioService>();
                services.AddSingleton<SuggestionService>();
                services.AddSingleton<ClipboardService>();
                services.AddSingleton<SettingsService>();
                services.AddSingleton<IWin32Api, Win32Api>();
                services.AddSingleton<ActiveWindowService>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        _host.Start();
        var window = _host.Services.GetRequiredService<MainWindow>();
        window.Hide();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        base.OnExit(e);
    }
}
