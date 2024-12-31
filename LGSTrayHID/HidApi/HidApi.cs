using System.Runtime.InteropServices;

namespace LGSTrayHID.HidApi;

[StructLayout(LayoutKind.Sequential)]
public readonly struct HidApiVersion
{
    readonly int Major;
    readonly int Minor;
    readonly int Patch;

    public override readonly string ToString()
    {
        return $"{Major}.{Minor}.{Patch}";
    }
}

public static partial class HidApi
{
    [LibraryImport("hidapi", EntryPoint = "hid_init")]
    public static partial int HidInit();

    [LibraryImport("hidapi", EntryPoint = "hid_exit")]
    public static partial int HidExit();

    [LibraryImport("hidapi", EntryPoint = "hid_enumerate")]
    public static unsafe partial HidDeviceInfo* HidEnumerate(ushort vendor_id, ushort product_id);

    [LibraryImport("hidapi", EntryPoint = "hid_free_enumeration")]
    public static unsafe partial void HidFreeEnumeration(HidDeviceInfo* devs);

    [LibraryImport("hidapi", EntryPoint = "hid_open_path")]
    private static unsafe partial nint _HidOpenPath(byte* path);

    public static unsafe nint HidOpenPath(ref HidDeviceInfo dev)
    {
        return _HidOpenPath(dev.Path);
    }

    [LibraryImport("hidapi", EntryPoint = "hid_close")]
    public static unsafe partial void HidClose(nint dev);

    [LibraryImport("hidapi", EntryPoint = "hid_write")]
    public static unsafe partial int HidWrite(nint dev, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), In] byte[] data, nuint length);

    [LibraryImport("hidapi", EntryPoint = "hid_read_timeout")]
    public static unsafe partial int HidReadTimeout(nint dev, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), Out] byte[] data, nuint length, int milliseconds);

    [LibraryImport("hidapi", EntryPoint = "hid_version")]
    private static unsafe partial HidApiVersion* _HidVersion();

    public unsafe static HidApiVersion HidVersion()
    {
        return *_HidVersion();
    }
}
