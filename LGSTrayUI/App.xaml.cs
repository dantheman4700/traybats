using Microsoft.Extensions.Configuration;
using System.Windows;
using Tomlyn.Extensions.Configuration;

namespace LGSTrayUI;

public partial class App : Application
{
    private NotifyIconViewModel? _notifyIcon;
    private IConfiguration? _configuration;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddTomlFile("appsettings.toml", optional: false, reloadOnChange: true);

        _configuration = builder.Build();
        _notifyIcon = new NotifyIconViewModel(_configuration);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _notifyIcon?.Dispose();
        base.OnExit(e);
    }
}