using System;
using System.Diagnostics.CodeAnalysis;
using SapNwRfcCore.Internal.Interop;

namespace SapNwRfcCore.Internal.Fields;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Reflection use")]
internal sealed class LongField : Field<long>
{
    public LongField(string name, long value)
        : base(name, value)
    {
    }

    public override void Apply(RfcInterop interop, IntPtr dataHandle)
    {
        var resultCode = interop.SetInt8(
            dataHandle: dataHandle,
            name: Name,
            value: Value,
            errorInfo: out var errorInfo);

        resultCode.ThrowOnError(errorInfo);
    }

    public static LongField Extract(RfcInterop interop, IntPtr dataHandle, string name)
    {
        var resultCode = interop.GetInt8(
            dataHandle: dataHandle,
            name: name,
            out long value,
            out var errorInfo);

        resultCode.ThrowOnError(errorInfo);

        return new LongField(name, value);
    }

    [ExcludeFromCodeCoverage]
    public override string ToString()
        => $"{Name} = {Value}L";
}
