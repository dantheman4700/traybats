using System;

namespace LGSTrayPrimitives;

public enum DeviceType
{
    Mouse,
    Keyboard,
    Headset,
    Bluetooth
}

public interface IDeviceMessage
{
    string DeviceId { get; }
    string DeviceName { get; }
    DeviceType DeviceType { get; }
}

public record DeviceInitMessage(
    string DeviceId,
    string DeviceName,
    bool HasBattery,
    DeviceType DeviceType
) : IDeviceMessage;

public record DeviceUpdateMessage(
    string DeviceId,
    string DeviceName,
    DeviceType DeviceType,
    int BatteryPercentage,
    PowerSupplyStatus PowerStatus,
    double BatteryVoltage,
    DateTimeOffset LastUpdate,
    double BatteryMileage = 0
) : IDeviceMessage; 