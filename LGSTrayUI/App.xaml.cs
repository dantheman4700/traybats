using System.Windows;

namespace LGSTrayUI
{
    public partial class App : Application
    {
        private NotifyIconViewModel? _notifyIconViewModel;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            _notifyIconViewModel = new NotifyIconViewModel();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _notifyIconViewModel?.Dispose();
            base.OnExit(e);
        }
    }
}