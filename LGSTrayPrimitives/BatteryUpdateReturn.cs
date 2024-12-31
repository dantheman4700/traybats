namespace LGSTrayPrimitives;

public readonly struct BatteryUpdateReturn
{
    public int BatteryLevel { get; }
    public PowerSupplyStatus Status { get; }
    public int BatteryVoltage { get; }

    public BatteryUpdateReturn(int batteryLevel, PowerSupplyStatus status, int batteryVoltage)
    {
        BatteryLevel = batteryLevel;
        Status = status;
        BatteryVoltage = batteryVoltage;
    }
} 