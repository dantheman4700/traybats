using System.Windows;
using System.Windows.Input;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LGSTrayHID;
using LGSTrayHID.HidApi;
using static LGSTrayHID.HidApi.HidApi;
using System.Timers;
using System.Runtime.InteropServices;
using LGSTrayPrimitives;
using System.Drawing;
using Application = System.Windows.Application;
using Microsoft.Extensions.Configuration;

namespace LGSTrayUI;

public partial class NotifyIconViewModel : ObservableObject, IDisposable
{
    private const ushort LOGITECH_VENDOR_ID = 0x046D;
    private const ushort G703_LIGHTSPEED_PRODUCT_ID = 0xC539;

    private readonly NotifyIcon _notifyIcon;
    private readonly System.Timers.Timer _updateTimer;
    private HidppDevices? _device;
    private nint _devShort;
    private nint _devLong;

    [ObservableProperty]
    private string _deviceName = string.Empty;

    [ObservableProperty]
    private int _batteryPercentage;

    [ObservableProperty]
    private double _batteryVoltage;

    [ObservableProperty]
    private int _batteryMileage;

    [ObservableProperty]
    private PowerSupplyStatus _powerSupplyStatus;

    [ObservableProperty]
    private DateTime _lastUpdate;

    private bool _disposed;

    private readonly UserSettingsWrapper _userSettings;

    [RelayCommand]
    private void ShowWindow()
    {
        var point = System.Windows.Forms.Control.MousePosition;
        var window = new DeviceWindow
        {
            DataContext = this,
            Left = point.X - 100,
            Top = point.Y - 100
        };
        window.Show();
    }

    [RelayCommand]
    private void Exit()
    {
        Dispose();
        Application.Current.Shutdown();
    }

    public NotifyIconViewModel(IConfiguration configuration)
    {
        _userSettings = new UserSettingsWrapper(configuration);
        Console.WriteLine("Initializing NotifyIconViewModel");
        _notifyIcon = new NotifyIcon
        {
            Icon = Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location),
            ContextMenuStrip = new ContextMenuStrip(),
            Visible = true
        };

        Console.WriteLine("Setting up context menu");
        _notifyIcon.ContextMenuStrip.Items.Add("Show", null, (s, e) => ShowWindow());
        _notifyIcon.ContextMenuStrip.Items.Add("Exit", null, (s, e) => Exit());
        _notifyIcon.MouseClick += (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                ShowWindow();
            }
        };

        _updateTimer = new System.Timers.Timer(5000); // Update every 5 seconds
        _updateTimer.Elapsed += async (s, e) => await UpdateDeviceStatus();

        Task.Run(async () =>
        {
            try
            {
                Console.WriteLine("Getting HID API version");
                var version = HidVersion();
                Console.WriteLine($"HID API Version: {version}");

                Console.WriteLine("Initializing HID API");
                if (HidInit() < 0)
                {
                    Console.WriteLine("HID API initialization failed");
                    return;
                }

                Console.WriteLine("HID API initialized successfully");

                Console.WriteLine("Starting device initialization");
                if (await InitializeDevice())
                {
                    Console.WriteLine("Device initialized successfully");
                    _updateTimer.Start();
                }
                else
                {
                    Console.WriteLine("Failed to initialize device");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during initialization: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        });
    }

    private bool IsSupportedDevice(ushort productId)
    {
        return productId == G703_LIGHTSPEED_PRODUCT_ID;
    }

    private unsafe (nint devShort, nint devLong)? FindDevices()
    {
        Console.WriteLine("Enumerating devices");
        var deviceList = HidApi.HidEnumerate(LOGITECH_VENDOR_ID, 0);
        if (deviceList == null)
        {
            Console.WriteLine("No devices found");
            return null;
        }

        Console.WriteLine("Found devices:");
        var currentDevice = deviceList;
        nint devShort = IntPtr.Zero;
        nint devLong = IntPtr.Zero;

        while (currentDevice != null)
        {
            var path = currentDevice->GetPath();
            Console.WriteLine($"VID: {currentDevice->VendorId:X4}, PID: {currentDevice->ProductId:X4}, Path: {path}");

            if (IsSupportedDevice(currentDevice->ProductId))
            {
                var messageType = currentDevice->GetHidppMessageType();
                if (messageType == HidppMessageType.SHORT)
                {
                    Console.WriteLine("Found device with SHORT interface");
                    devShort = HidApi.HidOpenPath(ref *currentDevice);
                }
                else if (messageType == HidppMessageType.LONG)
                {
                    Console.WriteLine("Found device with LONG interface");
                    devLong = HidApi.HidOpenPath(ref *currentDevice);
                }
            }

            currentDevice = currentDevice->Next;
        }

        HidApi.HidFreeEnumeration(deviceList);

        if (devShort == IntPtr.Zero)
        {
            Console.WriteLine("No device with SHORT interface found");
            return null;
        }

        if (devLong == IntPtr.Zero)
        {
            Console.WriteLine("No device with LONG interface found");
            return null;
        }

        return (devShort, devLong);
    }

    private async Task<bool> InitializeDevice()
    {
        var devices = FindDevices();
        if (!devices.HasValue)
        {
            return false;
        }

        (_devShort, _devLong) = devices.Value;

        Console.WriteLine("Creating HidppDevices instance");
        _device = new HidppDevices(_devShort, _devLong);
        await _device.StartAsync();

        return true;
    }

    private async Task UpdateDeviceStatus()
    {
        if (_device == null)
        {
            Console.WriteLine("Device is null");
            return;
        }

        foreach (var device in _device.GetDevices())
        {
            DeviceName = device.Name;
            var batteryStatus = await device.GetBatteryStatusAsync();
            if (batteryStatus.HasValue)
            {
                BatteryPercentage = (int)batteryStatus.Value.batteryPercentage;
                BatteryVoltage = batteryStatus.Value.batteryMVolt / 1000.0;
                PowerSupplyStatus = batteryStatus.Value.status;
                LastUpdate = DateTime.Now;
                _notifyIcon.Text = $"{DeviceName}, {BatteryPercentage}% - {LastUpdate:HH:mm:ss}";
            }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _notifyIcon.Dispose();
            _updateTimer?.Stop();
            _updateTimer?.Dispose();
            _device?.Dispose();
            if (_devShort != IntPtr.Zero)
            {
                HidClose(_devShort);
                _devShort = IntPtr.Zero;
            }
            if (_devLong != IntPtr.Zero)
            {
                HidClose(_devLong);
                _devLong = IntPtr.Zero;
            }
        }
    }
}
