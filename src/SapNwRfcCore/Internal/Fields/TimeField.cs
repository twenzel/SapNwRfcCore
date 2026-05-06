using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using SapNwRfcCore.Internal.Interop;

namespace SapNwRfcCore.Internal.Fields;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Reflection use")]
internal sealed class TimeField : Field<TimeSpan?>
{
    private const string RfcTimeFormat = "hhmmss";
    private static readonly string ZeroRfcTimeString = new string('0', 6);
    private static readonly string EmptyRfcTimeString = new string(' ', 6);

    public TimeField(string name, TimeSpan? value)
        : base(name, value)
    {
    }

    public override void Apply(RfcInterop interop, IntPtr dataHandle)
    {
        string stringValue = Value?.ToString(RfcTimeFormat, CultureInfo.InvariantCulture) ?? ZeroRfcTimeString;

        var resultCode = interop.SetTime(
            dataHandle: dataHandle,
            name: Name,
            time: stringValue.ToCharArray(),
            out var errorInfo);

        resultCode.ThrowOnError(errorInfo);
    }

    public static TimeField Extract(RfcInterop interop, IntPtr dataHandle, string name)
    {
        char[] buffer = EmptyRfcTimeString.ToCharArray();

        var resultCode = interop.GetTime(
            dataHandle: dataHandle,
            name: name,
            emptyTime: buffer,
            out var errorInfo);

        resultCode.ThrowOnError(errorInfo);

        var timeString = new string(buffer);

        if (timeString == EmptyRfcTimeString || timeString == ZeroRfcTimeString)
            return new TimeField(name, null);

        if (!TimeSpan.TryParseExact(timeString, RfcTimeFormat, CultureInfo.InvariantCulture, out var time))
            return new TimeField(name, null);

        return new TimeField(name, time);
    }

    [ExcludeFromCodeCoverage]
    public override string ToString()
        => $"{Name} = {Value:hh:mm:ss}";
}
