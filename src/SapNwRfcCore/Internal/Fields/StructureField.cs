using System.Diagnostics.CodeAnalysis;
using SapNwRfcCore.Internal.Interop;

namespace SapNwRfcCore.Internal.Fields;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Reflection use")]
internal sealed class StructureField<TStructure> : Field<TStructure>
{
    public StructureField(string name, TStructure value)
        : base(name, value)
    {
    }

    public override void Apply(RfcInterop interop, IntPtr dataHandle)
    {
        var resultCode = interop.GetStructure(
            dataHandle: dataHandle,
            name: Name,
            structHandle: out IntPtr structHandle,
            out var errorInfo);

        resultCode.ThrowOnError(errorInfo);

        InputMapper.Apply(interop, structHandle, Value);
    }

    public static StructureField<T> Extract<T>(RfcInterop interop, IntPtr dataHandle, string name)
    {
        var resultCode = interop.GetStructure(
            dataHandle: dataHandle,
            name: name,
            structHandle: out IntPtr structHandle,
            out var errorInfo);

        resultCode.ThrowOnError(errorInfo);

        var structValue = OutputMapper.Extract<T>(interop, structHandle);

        return new StructureField<T>(name, structValue);
    }

    [ExcludeFromCodeCoverage]
    public override string ToString()
        => $"{Name} = {typeof(TStructure)}";
}
