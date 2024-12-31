using System.Windows;

namespace LGSTrayUI
{
    public partial class DeviceWindow : Window
    {
        public DeviceWindow()
        {
            InitializeComponent();
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Topmost = true;
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            Hide();
        }
    }
} 