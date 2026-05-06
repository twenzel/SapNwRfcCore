using SapNwRfcCore.Internal.Interop;

namespace SapNwRfcCore;

/// <summary>
/// Represents SAP RFC type metadata.
/// </summary>
public sealed class SapTypeMetadata : ISapTypeMetadata
{
    private readonly RfcInterop _interop;
    private readonly IntPtr _typeDescription;
    private SapMetadataCollection<ISapFieldMetadata>? _fields;

    internal SapTypeMetadata(RfcInterop interop, IntPtr typeDescription)
    {
        _interop = interop;
        _typeDescription = typeDescription;
    }

    /// <inheritdoc cref="ISapTypeMetadata"/>
    public string GetName()
    {
        var resultCode = _interop.GetTypeName(
            rfcHandle: _typeDescription,
            typeName: out string typeName,
            errorInfo: out var errorInfo);

        errorInfo.ThrowOnError();

        return typeName;
    }

    /// <inheritdoc cref="ISapTypeMetadata"/>
    public ISapMetadataCollection<ISapFieldMetadata> Fields => _fields ??
        (_fields = new SapMetadataCollection<ISapFieldMetadata>(GetFieldByIndex, GetFieldByName, GetFieldCount));

    private ISapFieldMetadata GetFieldByIndex(int index)
    {
        var resultCode = _interop.GetFieldDescByIndex(
            typeHandle: _typeDescription,
            index: (uint)index,
            fieldDesc: out var fieldDesc,
            errorInfo: out var errorInfo);

        errorInfo.ThrowOnError();

        return new SapFieldMetadata(_interop, fieldDesc);
    }

    private int GetFieldCount()
    {
        var resultCode = _interop.GetFieldCount(
            typeHandle: _typeDescription,
            count: out uint count,
            errorInfo: out var errorInfo);

        errorInfo.ThrowOnError();

        return (int)count;
    }

    private ISapFieldMetadata? GetFieldByName(string name)
    {
        var resultCode = _interop.GetFieldDescByName(
            typeHandle: _typeDescription,
            name: name,
            fieldDesc: out var fieldDesc,
            errorInfo: out var errorInfo);

        if (resultCode == RfcResultCode.RFC_INVALID_PARAMETER)
            return null;

        errorInfo.ThrowOnError();

        return new SapFieldMetadata(_interop, fieldDesc);
    }
}
