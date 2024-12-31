using System.Text;
using LGSTrayHID.Features;
using static LGSTrayHID.HidppDevices;

namespace LGSTrayHID
{
    public class HidppDevice
    {
        private const int MAX_INIT_RETRIES = 3;
        private const int PING_TIMEOUT = 200;
        private const int FEATURE_TIMEOUT = 1000;

        private readonly SemaphoreSlim _initSemaphore = new(1, 1);
        private readonly HidppDevices _parent;
        private readonly byte _deviceIdx;
        private readonly Dictionary<ushort, byte> _featureMap = new();
        private Func<HidppDevice, Task<BatteryUpdateReturn?>>? _getBatteryAsync;

        private string _identifier = string.Empty;
        private string _name = string.Empty;
        private bool _isInitialized;
        private int _deviceType = 3;

        public string Identifier
        {
            get => _identifier;
            private set => _identifier = value;
        }

        public string Name
        {
            get => _name;
            private set => _name = value;
        }

        public bool IsInitialized => _isInitialized;

        public int DeviceType
        {
            get => _deviceType;
            private set => _deviceType = value;
        }

        public HidppDevices Parent => _parent;
        public byte DeviceIdx => _deviceIdx;
        public Dictionary<ushort, byte> FeatureMap => _featureMap;

        public HidppDevice(HidppDevices parent, byte deviceIdx)
        {
            _parent = parent;
            _deviceIdx = deviceIdx;
        }

        public async Task<bool> InitAsync()
        {
            await _initSemaphore.WaitAsync();
            try
            {
                Console.WriteLine($"Starting device initialization for device {_deviceIdx}");
                
                // Try initialization multiple times
                for (int initAttempt = 0; initAttempt < MAX_INIT_RETRIES; initAttempt++)
                {
                    if (await TryInitializeDevice())
                    {
                        _isInitialized = true;
                        return true;
                    }
                    await Task.Delay(500); // Wait before retrying
                }

                Console.WriteLine($"Failed to initialize device {_deviceIdx} after {MAX_INIT_RETRIES} attempts");
                return false;
            }
            finally
            {
                _initSemaphore.Release();
            }
        }

        private async Task<bool> TryInitializeDevice()
        {
            try
            {
                // First ensure the device is responsive
                if (!await EnsureDeviceResponsive())
                {
                    Console.WriteLine($"Device {_deviceIdx} is not responsive");
                    return false;
                }

                // Initialize feature set
                if (!await InitializeFeatureSet())
                {
                    Console.WriteLine($"Failed to initialize feature set for device {_deviceIdx}");
                    return false;
                }

                // Initialize device information
                if (!await InitPopulateAsync())
                {
                    Console.WriteLine($"Failed to populate device information for device {_deviceIdx}");
                    return false;
                }

                // Initialize battery features
                InitializeBatteryFeatures();

                Console.WriteLine($"Successfully initialized device {_deviceIdx}: {Name} (ID: {Identifier})");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during device initialization: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> EnsureDeviceResponsive()
        {
            for (int i = 0; i < 3; i++)
            {
                if (await _parent.Ping20(_deviceIdx, PING_TIMEOUT, false))
                {
                    return true;
                }
                await Task.Delay(100);
            }
            return false;
        }

        private async Task<bool> InitializeFeatureSet()
        {
            try
            {
                _featureMap.Clear();
                
                // Find 0x0001 IFeatureSet
                var ret = await _parent.WriteRead20(_parent.DevShort, new byte[7] { 0x10, _deviceIdx, 0x00, 0x00 | SW_ID, 0x00, 0x01, 0x00 });
                if (ret.Length == 0)
                {
                    Console.WriteLine("Failed to get IFeatureSet");
                    return false;
                }
                _featureMap[0x0001] = ret.GetParam(0);
                Console.WriteLine($"Found IFeatureSet at index {ret.GetParam(0)}");

                // Get Feature Count
                ret = await _parent.WriteRead20(_parent.DevShort, new byte[7] { 0x10, _deviceIdx, _featureMap[0x0001], 0x00 | SW_ID, 0x00, 0x00, 0x00 });
                if (ret.Length == 0)
                {
                    Console.WriteLine("Failed to get feature count");
                    return false;
                }
                int featureCount = ret.GetParam(0);
                Console.WriteLine($"Found {featureCount} features");

                // Enumerate Features
                for (byte i = 0; i <= featureCount; i++)
                {
                    ret = await _parent.WriteRead20(_parent.DevShort, new byte[7] { 0x10, _deviceIdx, _featureMap[0x0001], 0x10 | SW_ID, i, 0x00, 0x00 });
                    if (ret.Length > 0)
                    {
                        ushort featureId = (ushort)((ret.GetParam(0) << 8) + ret.GetParam(1));
                        _featureMap[featureId] = i;
                        Console.WriteLine($"Found feature 0x{featureId:X4} at index {i}");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing feature set: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> InitPopulateAsync()
        {
            try
            {
                // Device name
                if (!await InitializeDeviceName())
                {
                    Console.WriteLine("Failed to initialize device name");
                    return false;
                }

                // Device identifier
                if (!await InitializeDeviceIdentifier())
                {
                    Console.WriteLine("Failed to initialize device identifier");
                    return false;
                }

                // Initialize battery feature if available
                if (_featureMap.ContainsKey(0x1000) || _featureMap.ContainsKey(0x1001) || _featureMap.ContainsKey(0x1004))
                {
                    Console.WriteLine("Device has battery feature");
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during device population: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> InitializeDeviceName()
        {
            if (!_featureMap.TryGetValue(0x0005, out byte featureId))
            {
                Console.WriteLine("Device name feature not found");
                return false;
            }

            var ret = await _parent.WriteRead10(_parent.DevShort, new byte[7] { 0x10, _deviceIdx, featureId, 0x00 | SW_ID, 0x00, 0x00, 0x00 });
            if (ret.Length == 0)
            {
                Console.WriteLine("Failed to get name length");
                return false;
            }

            int nameLength = ret[4];
            string name = "";
            int retryCount = 0;

            while (name.Length < nameLength && retryCount < 3)
            {
                ret = await _parent.WriteRead10(_parent.DevShort, new byte[7] { 0x10, _deviceIdx, featureId, 0x10 | SW_ID, (byte)name.Length, 0x00, 0x00 });
                if (ret.Length == 0)
                {
                    retryCount++;
                    await Task.Delay(50);
                    continue;
                }
                name += Encoding.UTF8.GetString(ret.AsSpan(4));
            }

            if (name.Length < nameLength)
            {
                Console.WriteLine("Failed to get complete device name");
                return false;
            }

            Name = name.TrimEnd('\0');

            // Get device type
            ret = await _parent.WriteRead10(_parent.DevShort, new byte[7] { 0x10, _deviceIdx, featureId, 0x20 | SW_ID, 0x00, 0x00, 0x00 });
            if (ret.Length > 0)
            {
                DeviceType = ret[4];
            }

            return true;
        }

        private async Task<bool> InitializeDeviceIdentifier()
        {
            if (!_featureMap.TryGetValue(0x0003, out byte featureId))
            {
                return false;
            }

            var ret = await _parent.WriteRead20(_parent.DevShort, new byte[7] { 0x10, _deviceIdx, featureId, 0x00 | SW_ID, 0x00, 0x00, 0x00 });
            if (ret.Length == 0)
            {
                return false;
            }

            string unitId = BitConverter.ToString(ret.GetParams().ToArray(), 1, 4).Replace("-", string.Empty);
            string modelId = BitConverter.ToString(ret.GetParams().ToArray(), 7, 5).Replace("-", string.Empty);

            bool serialNumberSupported = (ret.GetParam(14) & 0x1) == 0x1;
            if (serialNumberSupported)
            {
                ret = await _parent.WriteRead20(_parent.DevShort, new byte[7] { 0x10, _deviceIdx, featureId, 0x20 | SW_ID, 0x00, 0x00, 0x00 });
                if (ret.Length > 0)
                {
                    Identifier = BitConverter.ToString(ret.GetParams().ToArray(), 0, 11).Replace("-", string.Empty);
                    return true;
                }
            }

            Identifier = $"{unitId}-{modelId}";
            return true;
        }

        private void InitializeBatteryFeatures()
        {
            if (FeatureMap.ContainsKey(0x1000))
            {
                _getBatteryAsync = Battery1000.GetBatteryAsync;
                Console.WriteLine("Using Battery1000 feature");
            }
            else if (FeatureMap.ContainsKey(0x1001))
            {
                _getBatteryAsync = Battery1001.GetBatteryAsync;
                Console.WriteLine("Using Battery1001 feature");
            }
            else if (FeatureMap.ContainsKey(0x1004))
            {
                _getBatteryAsync = Battery1004.GetBatteryAsync;
                Console.WriteLine("Using Battery1004 feature");
            }
            else
            {
                Console.WriteLine("No battery features found");
            }
        }

        public async Task<BatteryUpdateReturn?> GetBatteryStatusAsync()
        {
            if (_getBatteryAsync == null)
            {
                Console.WriteLine("No battery feature available");
                return null;
            }

            try
            {
                return await _getBatteryAsync(this);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting battery status: {ex.Message}");
                return null;
            }
        }
    }
}
