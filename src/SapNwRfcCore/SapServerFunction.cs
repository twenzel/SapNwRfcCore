using SapNwRfcCore.Internal;
using SapNwRfcCore.Internal.Dynamic;
using SapNwRfcCore.Internal.Interop;

namespace SapNwRfcCore;

/// <summary>
/// Represents an SAP RFC server function.
/// </summary>
public sealed class SapServerFunction : ISapServerFunction
{
    private readonly RfcInterop _interop;
    private readonly IntPtr _functionHandle;
    private readonly IntPtr _functionDescriptionHandle;
    private SapFunctionMetadata? _functionMetadata;

    internal SapServerFunction(
        RfcInterop interop,
        IntPtr functionHandle,
        IntPtr functionDescriptionHandle)
    {
        _interop = interop;
        _functionHandle = functionHandle;
        _functionDescriptionHandle = functionDescriptionHandle;
    }

    /// <inheritdoc cref="ISapServerFunction"/>
    public string GetName()
    {
        var resultCode = _interop.GetFunctionName(
            rfcHandle: _functionDescriptionHandle,
            funcName: out string functionName,
            errorInfo: out var errorInfo);

        resultCode.ThrowOnError(errorInfo);

        return functionName;
    }

    /// <inheritdoc cref="ISapFunction"/>
    public bool HasParameter(string parameterName)
    {
        var resultCode = _interop.GetParameterDescByName(
            funcDesc: _functionDescriptionHandle,
            name: parameterName,
            paramDesc: out var _,
            errorInfo: out var _);

        return resultCode == RfcResultCode.RFC_OK;
    }

    /// <inheritdoc cref="ISapFunction"/>
    public ISapFunctionMetadata Metadata => _functionMetadata ?? (_functionMetadata = new SapFunctionMetadata(_interop, _functionDescriptionHandle));

    /// <inheritdoc cref="ISapServerFunction"/>
    public TOutput GetParameters<TOutput>()
    {
        if (typeof(TOutput) == typeof(object))
        {
            return (TOutput)(object)new DynamicRfcFunction(_interop, _functionHandle, Metadata);
        }

        return OutputMapper.Extract<TOutput>(_interop, _functionHandle);
    }

    /// <inheritdoc cref="ISapServerFunction"/>
    public void SetResult(object input)
    {
        InputMapper.Apply(_interop, _functionHandle, input);
    }
}
