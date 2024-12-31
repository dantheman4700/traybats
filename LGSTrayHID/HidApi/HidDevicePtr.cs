﻿#define PRINT
using System.Threading.Channels;
using static LGSTrayHID.HidApi.HidApi;

namespace LGSTrayHID.HidApi
{
    public readonly struct HidDevicePtr
    {
        private readonly nint _ptr;

        private HidDevicePtr(nint ptr)
        {
            _ptr = ptr;
        }

        public static implicit operator nint(HidDevicePtr ptr) => ptr._ptr;

        public static implicit operator HidDevicePtr(nint ptr) => new(ptr);

        public Task<int> WriteAsync(byte[] buffer)
        {
#if DEBUG && PRINT
            PrintBuffer($"0x{_ptr:X} - W", buffer);
#endif
            var ret = HidWrite(this, buffer, (nuint)buffer.Length);

            return Task.FromResult(ret);
        }

        public int Read(byte[] buffer, int count, int timeout)
        {
            var ret = HidReadTimeout(this, buffer, (nuint)count, timeout);
#if DEBUG && PRINT
            PrintBuffer($"0x{_ptr:X} - R", buffer, ret < 1);
#endif
            return ret;
        }

#if DEBUG && PRINT
        private static int count = 0;
        private static readonly Channel<string> _channel = Channel.CreateUnbounded<string>();

        static HidDevicePtr()
        {
            Thread t1 = new(async () =>
            {
                while (true)
                {
                    var str = await _channel.Reader.ReadAsync();
                    Console.WriteLine(str);
                }
            });
            t1.Start();
        }

        private static void PrintBuffer(string prefix, byte[] buffer, bool ignore = false)
        {
            if (ignore)
            {
                return;
            }

            var arr = string.Join(" ", Array.ConvertAll(buffer, x => x.ToString("X02")));
            var str = $"{count:d04} - {prefix}: {arr}";
            _channel.Writer.TryWrite(str);

            count++;
        }
#endif
    }
}
