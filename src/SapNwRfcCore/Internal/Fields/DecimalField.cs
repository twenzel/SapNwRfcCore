using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using SapNwRfcCore.Internal.Interop;

namespace SapNwRfcCore.Internal.Fields;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Reflection use")]
internal sealed class DecimalField : Field<decimal>
{
    public DecimalField(string name, decimal value)
        : base(name, value)
    {
    }

    public override void Apply(RfcInterop interop, IntPtr dataHandle)
    {
        var stringValue = Value.ToString(CultureInfo.InvariantCulture);

        var resultCode = interop.SetString(
            dataHandle: dataHandle,
            name: Name,
            value: stringValue,
            valueLength: (uint)stringValue.Length,
            errorInfo: out var errorInfo);

        resultCode.ThrowOnError(errorInfo);
    }

    public static DecimalField Extract(RfcInterop interop, IntPtr dataHandle, string name)
    {
        var resultCode = interop.GetString(
            dataHandle: dataHandle,
            name: name,
            stringBuffer: Array.Empty<char>(),
            bufferLength: 0,
            stringLength: out uint stringLength,
            errorInfo: out var errorInfo);

        if (resultCode != RfcResultCode.RFC_BUFFER_TOO_SMALL)
        {
            resultCode.ThrowOnError(errorInfo);
            return new DecimalField(name, 0);
        }

        var buffer = new char[stringLength + 1];
        resultCode = interop.GetString(
            dataHandle: dataHandle,
            name: name,
            stringBuffer: buffer,
            bufferLength: (uint)buffer.Length,
            stringLength: out _,
            errorInfo: out errorInfo);

        resultCode.ThrowOnError(errorInfo);

        var decimalValue = decimal.Parse(new string(buffer, 0, (int)stringLength), CultureInfo.InvariantCulture);

        return new DecimalField(name, decimalValue);
    }

    [ExcludeFromCodeCoverage]
    public override string ToString()
        => $"{Name} = {Value.ToString(CultureInfo.InvariantCulture)}M";
}
