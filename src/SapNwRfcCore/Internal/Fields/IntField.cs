using System;
using System.Diagnostics.CodeAnalysis;
using SapNwRfcCore.Internal.Interop;

namespace SapNwRfcCore.Internal.Fields;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Reflection use")]
internal sealed class IntField : Field<int>
{
    public IntField(string name, int value)
        : base(name, value)
    {
    }

    public override void Apply(RfcInterop interop, IntPtr dataHandle)
    {
        var resultCode = interop.SetInt(
            dataHandle: dataHandle,
            name: Name,
            value: Value,
            errorInfo: out var errorInfo);

        resultCode.ThrowOnError(errorInfo);
    }

    public static IntField Extract(RfcInterop interop, IntPtr dataHandle, string name)
    {
        var resultCode = interop.GetInt(
            dataHandle: dataHandle,
            name: name,
            out int value,
            out var errorInfo);

        resultCode.ThrowOnError(errorInfo);

        return new IntField(name, value);
    }

    [ExcludeFromCodeCoverage]
    public override string ToString()
        => $"{Name} = {Value}";
}
