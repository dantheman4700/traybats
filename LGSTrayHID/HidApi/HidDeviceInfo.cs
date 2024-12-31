using System.Runtime.InteropServices;

namespace LGSTrayHID.HidApi;

public enum HidBusType : int
{
    HID_API_BUS_UNKNOWN = 0x00,
    HID_API_BUS_USB = 0x01,
    HID_API_BUS_BLUETOOTH = 0x02,
    HID_API_BUS_I2C = 0x03,
    HID_API_BUS_SPI = 0x04,
}

[StructLayout(LayoutKind.Sequential)]
public readonly unsafe struct HidDeviceInfo
{
    public readonly byte* Path;
    public readonly ushort VendorId;
    public readonly ushort ProductId;
    public readonly byte* SerialNumber;
    public readonly ushort ReleaseNumber;
    public readonly byte* ManufacturerString;
    public readonly byte* ProductString;
    public readonly ushort UsagePage;
    public readonly ushort Usage;
    public readonly int InterfaceNumber;
    public readonly HidDeviceInfo* Next;
    public readonly HidBusType BusType;
}
