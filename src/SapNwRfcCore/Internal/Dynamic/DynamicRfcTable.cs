using System.Collections;
using System.Dynamic;
using SapNwRfcCore.Internal.Interop;

namespace SapNwRfcCore.Internal.Dynamic;

internal sealed class DynamicRfcTable : DynamicObject, IReadOnlyList<object>
{
    private readonly RfcInterop _interop;
    private readonly IntPtr _tableHandle;
    private readonly ISapTypeMetadata? _typeMetadata;
    private uint? _rowCount;

    public DynamicRfcTable(RfcInterop interop, IntPtr tableHandle, ISapTypeMetadata? sapTypeMetadata)
    {
        _interop = interop;
        _tableHandle = tableHandle;
        _typeMetadata = sapTypeMetadata;
    }

    private uint GetRowCount()
    {
        if (!_rowCount.HasValue)
        {
            var resultCode = _interop.GetRowCount(
                tableHandle: _tableHandle,
                rowCount: out uint rowCount,
                errorInfo: out var errorInfo);

            errorInfo.ThrowOnError();

            _rowCount = rowCount;
        }

        return _rowCount.Value;
    }

    private bool TryGetValueByIndex(uint index, out object result)
    {
        var resultCode = _interop.MoveTo(
            tableHandle: _tableHandle,
            index: index,
            errorInfo: out var errorInfo);

        resultCode.ThrowOnError(errorInfo);

        IntPtr rowHandle = _interop.GetCurrentRow(
            tableHandle: _tableHandle,
            errorInfo: out errorInfo);

        errorInfo.ThrowOnError();

        result = new DynamicRfcStructure(_interop, rowHandle, _typeMetadata);
        return true;
    }

    public override IEnumerable<string> GetDynamicMemberNames()
    {
        yield return "Count";
    }

    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        if (binder.Name == "Count")
        {
            result = GetRowCount();
            return true;
        }

        return base.TryGetMember(binder, out result);
    }

    public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
    {
        if (indexes.Length != 1)
            throw new ArgumentException("Invalid length", nameof(indexes));

        var index = indexes[0];

        if (index is int i)
            return TryGetValueByIndex((uint)i, out result);

        if (index is uint u)
            return TryGetValueByIndex(u, out result);

        throw new ArgumentException("Invalid index type", nameof(indexes));
    }

    public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
    {
        if (binder.Name == "GetMetadata" && binder.CallInfo.ArgumentCount == 0)
        {
            result = _typeMetadata;
            return true;
        }

        return base.TryInvokeMember(binder, args, out result);
    }

    public int Count => (int)GetRowCount();

    public object this[int index]
    {
        get
        {
            if (TryGetValueByIndex((uint)index, out object result))
            {
                return result;
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public IEnumerator<object> GetEnumerator()
    {
        var moveFirstResultCode = _interop.MoveToFirstRow(
            tableHandle: _tableHandle,
            errorInfo: out var moveFirstErrorInfo);

        if (moveFirstResultCode == RfcResultCode.RFC_TABLE_MOVE_BOF)
            yield break;

        moveFirstResultCode.ThrowOnError(moveFirstErrorInfo);

        while (true)
        {
            IntPtr rowHandle = _interop.GetCurrentRow(
                tableHandle: _tableHandle,
                errorInfo: out var errorInfo);

            errorInfo.ThrowOnError();

            yield return new DynamicRfcStructure(_interop, rowHandle, _typeMetadata);

            var moveNextResultCode = _interop.MoveToNextRow(
                tableHandle: _tableHandle,
                errorInfo: out var moveNextErrorInfo);

            if (moveNextResultCode == RfcResultCode.RFC_TABLE_MOVE_EOF)
                yield break;

            moveNextResultCode.ThrowOnError(moveNextErrorInfo);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
