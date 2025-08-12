using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SpecialGuide.Core.Models;
using SpecialGuide.Core.Services;

namespace SpecialGuide.App;

public partial class App : Application
{
    private IHost? _host;
    private CancellationTokenSource? _cts;

    protected override async Task OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _cts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken.None);
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.Configure<Settings>(context.Configuration.GetSection("Settings"));
                services.AddSingleton<Overlay.RadialMenuWindow>();
                services.AddSingleton<IRadialMenu>(sp => sp.GetRequiredService<Overlay.RadialMenuWindow>());
                services.AddSingleton<HookService>();
                services.AddSingleton<OverlayService>();
                services.AddSingleton<CaptureService>();
                services.AddSingleton<OpenAIService>();
                services.AddSingleton<AudioService>();
                services.AddSingleton<SettingsService>();
                services.AddSingleton<ClipboardService>();
                services.AddSingleton<WindowService>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync(_cts.Token);
        var window = _host.Services.GetRequiredService<MainWindow>();
        window.Hide();
    }

    protected override async Task OnExit(ExitEventArgs e)
    {
        if (_host != null && _cts != null)
        {
            await _host.StopAsync(_cts.Token);
            _host.Dispose();
            _cts.Dispose();
        }
        base.OnExit(e);
    }
}
