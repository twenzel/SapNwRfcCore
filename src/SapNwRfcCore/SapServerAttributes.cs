using System.Runtime.InteropServices;
using SapNwRfcCore.Internal.Interop;

namespace SapNwRfcCore;

public sealed class SapServerAttributes
{
    private readonly RfcServerAttributes _serverAttributes;

    internal SapServerAttributes(RfcServerAttributes serverAttributes)
    {
        _serverAttributes = serverAttributes;
        ServerName = Marshal.PtrToStringAuto(serverAttributes.ServerName) ?? string.Empty;
    }

    public string ServerName { get; }

    public SapRfcProtocolType Type => _serverAttributes.Type;

    public uint RegistrationCount => _serverAttributes.RegistrationCount;

    public SapRfcServerState State => _serverAttributes.State;

    public uint CurrentBusyCount => _serverAttributes.CurrentBusyCount;

    public uint PeakBusyCount => _serverAttributes.PeakBusyCount;
}
