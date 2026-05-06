using System.Runtime.InteropServices;

namespace SapNwRfcCore.Internal.Interop;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct RfcExceptionDescription
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string Key;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
    public string Message;
}
