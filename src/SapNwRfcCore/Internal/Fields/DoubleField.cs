using System;
using System.Diagnostics.CodeAnalysis;
using SapNwRfcCore.Internal.Interop;

namespace SapNwRfcCore.Internal.Fields;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Reflection use")]
internal sealed class DoubleField : Field<double>
{
    public DoubleField(string name, double value)
        : base(name, value)
    {
    }

    public override void Apply(RfcInterop interop, IntPtr dataHandle)
    {
        var resultCode = interop.SetFloat(
            dataHandle: dataHandle,
            name: Name,
            value: Value,
            errorInfo: out var errorInfo);

        resultCode.ThrowOnError(errorInfo);
    }

    public static DoubleField Extract(RfcInterop interop, IntPtr dataHandle, string name)
    {
        var resultCode = interop.GetFloat(
            dataHandle: dataHandle,
            name: name,
            out double value,
            out var errorInfo);

        resultCode.ThrowOnError(errorInfo);

        return new DoubleField(name, value);
    }

    [ExcludeFromCodeCoverage]
    public override string ToString()
        => $"{Name} = {Value}";
}
