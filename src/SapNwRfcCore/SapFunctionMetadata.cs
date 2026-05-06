using SapNwRfcCore.Internal.Interop;

namespace SapNwRfcCore;

/// <summary>
/// Represents SAP RFC function metadata.
/// </summary>
public sealed class SapFunctionMetadata : ISapFunctionMetadata
{
    private readonly RfcInterop _interop;
    private readonly IntPtr _functionDescHandle;
    private SapMetadataCollection<ISapParameterMetadata>? _parameters;
    private SapMetadataCollection<ISapExceptionMetadata>? _exceptions;

    internal SapFunctionMetadata(RfcInterop interop, IntPtr functionDescHandle)
    {
        _interop = interop;
        _functionDescHandle = functionDescHandle;
    }

    /// <inheritdoc cref="ISapFunctionMetadata"/>
    public string GetName()
    {
        var resultCode = _interop.GetFunctionName(
            rfcHandle: _functionDescHandle,
            funcName: out string funcName,
            errorInfo: out var errorInfo);

        errorInfo.ThrowOnError();

        return funcName;
    }

    /// <inheritdoc cref="ISapFunctionMetadata"/>
    public ISapMetadataCollection<ISapParameterMetadata> Parameters => _parameters ??
        (_parameters = new SapMetadataCollection<ISapParameterMetadata>(GetParameterByIndex, GetParameterByName, GetParameterCount));

    private ISapParameterMetadata GetParameterByIndex(int index)
    {
        var resultCode = _interop.GetParameterDescByIndex(
            funcDesc: _functionDescHandle,
            index: (uint)index,
            paramDesc: out var paramDesc,
            errorInfo: out var errorInfo);

        errorInfo.ThrowOnError();

        return new SapParameterMetadata(_interop, paramDesc);
    }

    private int GetParameterCount()
    {
        var resultCode = _interop.GetParameterCount(
            funcDesc: _functionDescHandle,
            count: out uint count,
            errorInfo: out var errorInfo);

        errorInfo.ThrowOnError();

        return (int)count;
    }

    private SapParameterMetadata? GetParameterByName(string name)
    {
        var resultCode = _interop.GetParameterDescByName(
            funcDesc: _functionDescHandle,
            name: name,
            paramDesc: out var paramDesc,
            errorInfo: out var errorInfo);

        if (resultCode == RfcResultCode.RFC_INVALID_PARAMETER)
            return null;

        errorInfo.ThrowOnError();

        return new SapParameterMetadata(_interop, paramDesc);
    }

    /// <inheritdoc cref="ISapFunctionMetadata"/>
    public ISapMetadataCollection<ISapExceptionMetadata> Exceptions => _exceptions ??= new SapMetadataCollection<ISapExceptionMetadata>(GetExceptionByIndex, GetExceptionByName, GetExceptionCount);

    private ISapExceptionMetadata GetExceptionByIndex(int index)
    {
        var resultCode = _interop.GetExceptionDescByIndex(
            funcDesc: _functionDescHandle,
            index: (uint)index,
            excDesc: out var excDesc,
            errorInfo: out var errorInfo);

        errorInfo.ThrowOnError();

        return new SapExceptionMetadata(excDesc);
    }

    private int GetExceptionCount()
    {
        var resultCode = _interop.GetExceptionCount(
            funcDesc: _functionDescHandle,
            count: out uint count,
            errorInfo: out var errorInfo);

        errorInfo.ThrowOnError();

        return (int)count;
    }

    private ISapExceptionMetadata? GetExceptionByName(string name)
    {
        var resultCode = _interop.GetExceptionDescByName(
            funcDesc: _functionDescHandle,
            name: name,
            excDesc: out var excDesc,
            errorInfo: out var errorInfo);

        if (resultCode == RfcResultCode.RFC_INVALID_PARAMETER)
            return null;

        errorInfo.ThrowOnError();

        return new SapExceptionMetadata(excDesc);
    }

    // Used by SapServer.InstallGenericServerFunctionHandler
    internal IntPtr GetFunctionDescHandle() => _functionDescHandle;
}
