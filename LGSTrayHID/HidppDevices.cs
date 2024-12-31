using System.Threading.Channels;
using LGSTrayHID.HidApi;
using static LGSTrayHID.HidApi.HidApi;

namespace LGSTrayHID
{
    public class HidppDevices : IDisposable
    {
        public const byte SW_ID = 0x0A;
        private byte PING_PAYLOAD = 0x55;

        private bool _isReading = true;
        private const int READ_TIMEOUT = 100;

        private readonly Dictionary<byte, HidppDevice> _deviceCollection = new();
        public IReadOnlyDictionary<byte, HidppDevice> DeviceCollection => _deviceCollection;

        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly Channel<byte[]> _channel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(5)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = true,
        });

        private nint _devShort = IntPtr.Zero;
        public nint DevShort => _devShort;

        private nint _devLong = IntPtr.Zero;
        public nint DevLong => _devLong;

        private int _disposeCount = 0;
        public bool Disposed => _disposeCount > 0;

        private Task? _readTaskShort;
        private Task? _readTaskLong;

        public HidppDevices(nint devShort, nint devLong)
        {
            _devShort = devShort;
            _devLong = devLong;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.Increment(ref _disposeCount) == 1)
            {
                _isReading = false;
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

        ~HidppDevices()
        {
            Dispose(disposing: false);
        }

        public async Task StartAsync()
        {
            // Start the read threads
            _readTaskShort = Task.Run(() => ReadThread(_devShort, 7));
            _readTaskLong = Task.Run(() => ReadThread(_devLong, 20));

            // Initialize devices
            await SetUp();
        }

        public IEnumerable<HidppDevice> GetDevices() => _deviceCollection.Values;

        private async Task ReadThread(nint dev, int bufferSize)
        {
            Console.WriteLine($"Starting read thread for device {dev:X} with buffer size {bufferSize}");
            byte[] buffer = new byte[bufferSize];
            while (_isReading)
            {
                var ret = HidReadTimeout(dev, buffer, (nuint)bufferSize, READ_TIMEOUT);
                if (!_isReading) { break; }

                if (ret < 0)
                {
                    break;
                }
                else if (ret == 0)
                {
                    continue;
                }

                await ProcessMessage(buffer);
            }

            HidClose(dev);
        }

        private async Task ProcessMessage(byte[] buffer)
        {
            if ((buffer[2] == 0x41) && ((buffer[4] & 0x40) == 0))
            {
                byte deviceIdx = buffer[1];
                if (true || !_deviceCollection.ContainsKey(deviceIdx))
                {
                    _deviceCollection[deviceIdx] = new(this, deviceIdx);
                    new Thread(async () =>
                    {
                        try
                        {
                            await Task.Delay(1000);
                            await _deviceCollection[deviceIdx].InitAsync();
                        }
                        catch (Exception) { }
                    }).Start();
                }
            }
            else
            {
                await _channel.Writer.WriteAsync(buffer);
            }
        }

        public async Task<byte[]> WriteRead10(nint hidDevicePtr, byte[] buffer, int timeout = 100)
        {
            ObjectDisposedException.ThrowIf(_disposeCount > 0, this);

            bool locked = await _semaphore.WaitAsync(100);
            if (!locked)
            {
                return [];
            }

            try
            {
                await WriteAsync(hidDevicePtr, buffer);

                CancellationTokenSource cts = new();
                cts.CancelAfter(timeout);

                byte[] ret;
                while (!cts.IsCancellationRequested)
                {
                    try
                    {
                        ret = await _channel.Reader.ReadAsync(cts.Token);

                        if ((ret[2] == 0x8F) || (ret[2] == buffer[2]))
                        {
                            return ret;
                        }
                    }
                    catch (OperationCanceledException) { break; }
                }

                return [];
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<Hidpp20> WriteRead20(nint hidDevicePtr, Hidpp20 buffer, int timeout = 100, bool ignoreHID10 = true)
        {
            ObjectDisposedException.ThrowIf(_disposeCount > 0, this);

            bool locked = await _semaphore.WaitAsync(100);
            if (!locked)
            {
                return (Hidpp20)Array.Empty<byte>();
            }

            try
            {
                await WriteAsync(hidDevicePtr, (byte[])buffer);

                CancellationTokenSource cts = new();
                cts.CancelAfter(timeout);

                Hidpp20 ret;
                while (!cts.IsCancellationRequested)
                {
                    try
                    {
                        ret = await _channel.Reader.ReadAsync(cts.Token);

                        if (!ignoreHID10 && (ret.GetFeatureIndex() == 0x8F))
                        {
                            // HID++ 1.0 response or timeout
                            break;
                        }

                        if ((ret.GetFeatureIndex() == buffer.GetFeatureIndex()) && (ret.GetSoftwareId() == SW_ID))
                        {
                            return ret;
                        }
                    }
                    catch (OperationCanceledException) { break; }
                }

                return (Hidpp20)Array.Empty<byte>();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> Ping20(byte deviceId, int timeout = 100, bool ignoreHIDPP10 = true)
        {
            ObjectDisposedException.ThrowIf(_disposeCount > 0, this);

            byte pingPayload = ++PING_PAYLOAD;
            Hidpp20 buffer = new byte[7] { 0x10, deviceId, 0x00, 0x10 | SW_ID, 0x00, 0x00, pingPayload };
            Hidpp20 ret = await WriteRead20(_devShort, buffer, timeout, ignoreHIDPP10);
            if (ret.Length == 0)
            {
                return false;
            }

            return (ret.GetFeatureIndex() == 0x00) && (ret.GetSoftwareId() == SW_ID) && (ret.GetParam(2) == pingPayload);
        }

        private async Task WriteAsync(nint dev, byte[] buffer)
        {
            var ret = HidWrite(dev, buffer, (nuint)buffer.Length);
            await Task.CompletedTask;
        }

        private async Task SetUp()
        {
            if ((_devShort == IntPtr.Zero) || (_devLong == IntPtr.Zero))
            {
                Console.WriteLine("Device handles are not valid");
                return;
            }

            Console.WriteLine("Starting device setup");

            // Start read threads with higher priority for better response
            Thread t1 = new(async () => { await ReadThread(_devShort, 7); })
            {
                Priority = ThreadPriority.AboveNormal
            };
            t1.Start();

            Thread t2 = new(async () => { await ReadThread(_devLong, 20); })
            {
                Priority = ThreadPriority.AboveNormal
            };
            t2.Start();

            Console.WriteLine("Read threads started");

            // Wait for threads to initialize
            await Task.Delay(100);

            byte[] ret;
            const int INIT_TIMEOUT = 2000; // Increased timeout for initialization

            // Read number of devices on receiver
            Console.WriteLine("Querying number of devices");
            ret = await WriteRead10(_devShort, [0x10, 0xFF, 0x81, 0x02, 0x00, 0x00, 0x00], INIT_TIMEOUT);
            byte numDeviceFound = 0;
            if ((ret.Length > 0) && (ret[2] == 0x81) && (ret[3] == 0x02))
            {
                numDeviceFound = ret[5];
                Console.WriteLine($"Found {numDeviceFound} devices on receiver");
            }
            else
            {
                Console.WriteLine("Failed to get device count or no response");
            }

            if (numDeviceFound > 0)
            {
                // Force arrival announce with increased timeout
                Console.WriteLine("Requesting device announcements");
                ret = await WriteRead10(_devShort, [0x10, 0xFF, 0x80, 0x02, 0x02, 0x00, 0x00], INIT_TIMEOUT);
                if (ret.Length > 0)
                {
                    Console.WriteLine("Device announcement request successful");
                }
                else
                {
                    Console.WriteLine("Device announcement request failed");
                }
            }

            // Wait for device announcements
            await Task.Delay(1000);

            if (_deviceCollection.Count == 0)
            {
                Console.WriteLine("No devices announced, attempting manual enumeration");
                // Manual device enumeration
                for (byte i = 1; i <= 6; i++)
                {
                    Console.WriteLine($"Pinging device index {i}");
                    var ping = await Ping20(i, 500, false);
                    if (ping)
                    {
                        Console.WriteLine($"Device responded at index {i}");
                        var deviceIdx = i;
                        _deviceCollection[deviceIdx] = new(this, deviceIdx);
                    }
                }

                foreach ((byte idx, var device) in _deviceCollection)
                {
                    Console.WriteLine($"Initializing device {idx}");
                    try
                    {
                        await device.InitAsync();
                        Console.WriteLine($"Device {idx} initialized successfully");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to initialize device {idx}: {ex.Message}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"Found {_deviceCollection.Count} devices through announcements");
            }

            Console.WriteLine("Device setup completed");
        }
    }
}
