using LGSTrayPrimitives;
using static LGSTrayPrimitives.PowerSupplyStatus;

namespace LGSTrayHID.Features
{
    internal class BatteryFeature
    {
        private const int BATTERY_FEATURE_ID = 0x1000;
        private const byte GET_BATTERY_CAPABILITY = 0x00;
        private const byte GET_BATTERY_STATUS = 0x10;

        private readonly HidppDevices _devices;
        private readonly byte _deviceId;
        private byte _featureIndex;

        public BatteryFeature(HidppDevices devices, byte deviceId)
        {
            _devices = devices;
            _deviceId = deviceId;
        }

        public async Task InitAsync()
        {
            // Get feature index
            var buffer = new byte[7] { 0x10, _deviceId, 0x00, 0x00 | HidppDevices.SW_ID, 0x00, 0x00, 0x00 };
            var ret = await _devices.WriteRead10(_devices.DevShort, buffer);

            if (ret.Length == 0) return;

            // Find battery feature
            for (byte i = 0; i < 16; i++)
            {
                buffer = new byte[7] { 0x10, _deviceId, 0x00, 0x10 | HidppDevices.SW_ID, i, 0x00, 0x00 };
                ret = await _devices.WriteRead10(_devices.DevShort, buffer);

                if (ret.Length == 0) break;

                for (byte j = 0; j < 2; j++)
                {
                    var featureId = (ret.GetParam(j * 3) << 8) + ret.GetParam(j * 3 + 1);
                    var featureIndex = ret.GetParam(j * 3 + 2);

                    if (featureId == BATTERY_FEATURE_ID)
                    {
                        _featureIndex = featureIndex;
                        return;
                    }
                }
            }
        }

        public async Task<BatteryUpdateReturn> GetBatteryStatusAsync()
        {
            var buffer = new byte[7] { 0x10, _deviceId, _featureIndex, 0x00 | HidppDevices.SW_ID, GET_BATTERY_STATUS, 0x00, 0x00 };
            var ret = await _devices.WriteRead10(_devices.DevShort, buffer);

            if (ret.Length == 0)
            {
                return new BatteryUpdateReturn();
            }

            var level = ret.GetParam(0);
            var status = (PowerSupplyStatus)ret.GetParam(1);
            var voltage = (int)(ret.GetParam(2) * 10.0 + ret.GetParam(3) * 2560.0);

            return new BatteryUpdateReturn(level, status, voltage);
        }
    }
} 