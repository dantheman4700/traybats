using LGSTrayPrimitives;

namespace LGSTrayHID.Features
{
    public readonly record struct BatteryUpdateReturn
    {
        public readonly double batteryPercentage;
        public readonly PowerSupplyStatus status;
        public readonly int batteryMVolt;

        public BatteryUpdateReturn()
        {
            batteryPercentage = 0;
            status = PowerSupplyStatus.POWER_SUPPLY_STATUS_UNKNOWN;
            batteryMVolt = -1;
        }

        public BatteryUpdateReturn(double batteryPercentage, PowerSupplyStatus status, int batteryMVolt)
        {
            this.batteryPercentage = batteryPercentage;
            this.status = status;
            this.batteryMVolt = batteryMVolt;
        }
    }
} 