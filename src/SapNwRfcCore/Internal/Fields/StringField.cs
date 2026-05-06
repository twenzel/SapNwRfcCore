using System.Diagnostics.CodeAnalysis;
using SapNwRfcCore.Internal.Interop;

namespace SapNwRfcCore.Internal.Fields;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Reflection use")]
internal sealed class StringField : Field<string?>
{
    public StringField(string name, string? value)
        : base(name, value)
    {
    }

    public override void Apply(RfcInterop interop, IntPtr dataHandle)
    {
        if (Value == null)
            return;

        var resultCode = interop.SetString(
            dataHandle: dataHandle,
            name: Name,
            value: Value,
            valueLength: (uint)Value.Length,
            errorInfo: out var errorInfo);

        resultCode.ThrowOnError(errorInfo);
    }

    public static StringField Extract(RfcInterop interop, IntPtr dataHandle, string name)
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
            return new StringField(name, string.Empty);
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

        return new StringField(name, new string(buffer, 0, (int)stringLength));
    }

    [ExcludeFromCodeCoverage]
    public override string ToString()
        => $"{Name} = \"{Value}\"";
}
