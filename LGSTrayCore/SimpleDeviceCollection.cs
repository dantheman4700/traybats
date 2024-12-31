using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LGSTrayPrimitives;

namespace LGSTrayCore;

public partial class SimpleDeviceCollection : ObservableObject, ILogiDeviceCollection
{
    [ObservableProperty]
    private ObservableCollection<LogiDevice> _devices = new();

    public void OnDeviceMessage(IDeviceMessage message)
    {
        switch (message)
        {
            case DeviceInitMessage init:
                HandleInitMessage(init);
                break;
            case DeviceUpdateMessage update:
                HandleUpdateMessage(update);
                break;
        }
    }

    private void HandleInitMessage(DeviceInitMessage message)
    {
        var device = Devices.FirstOrDefault(d => d.DeviceId == message.DeviceId);
        if (device == null)
        {
            device = new LogiDevice
            {
                DeviceId = message.DeviceId,
                DeviceName = message.DeviceName,
                DeviceType = message.DeviceType,
                HasBattery = message.HasBattery,
                LastUpdate = DateTime.Now
            };
            Console.WriteLine($"Adding new device: {device.DeviceName} (ID: {device.DeviceId})");
            Devices.Add(device);
        }
        else
        {
            device.DeviceName = message.DeviceName;
            device.DeviceType = message.DeviceType;
            device.HasBattery = message.HasBattery;
            device.LastUpdate = DateTime.Now;
            Console.WriteLine($"Updated existing device: {device.DeviceName} (ID: {device.DeviceId})");
        }
    }

    private void HandleUpdateMessage(DeviceUpdateMessage message)
    {
        var device = Devices.FirstOrDefault(d => d.DeviceId == message.DeviceId);
        if (device != null)
        {
            device.BatteryPercentage = message.BatteryPercentage;
            device.BatteryVoltage = message.BatteryVoltage;
            device.BatteryMileage = message.BatteryMileage;
            device.PowerSupplyStatus = message.PowerStatus;
            device.LastUpdate = message.LastUpdate;
            Console.WriteLine($"Updated device {device.DeviceName} - Battery: {device.BatteryPercentage}%, Status: {device.PowerSupplyStatus}");
        }
        else
        {
            Console.WriteLine($"Received update for unknown device: {message.DeviceId}");
        }
    }

    public void Clear()
    {
        Console.WriteLine("Clearing device collection");
        Devices.Clear();
    }

    public IEnumerable<LogiDevice> GetDevices() => Devices;
} 