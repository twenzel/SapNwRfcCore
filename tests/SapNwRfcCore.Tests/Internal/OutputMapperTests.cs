using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Moq;
using SapNwRfcCore.Internal;
using SapNwRfcCore.Internal.Interop;
using Shouldly;
using Xunit;

namespace SapNwRfcCore.Tests.Internal;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Used as generic output types")]
public sealed class OutputMapperTests
{
    private static readonly IntPtr DataHandle = (IntPtr)123;
    private readonly Mock<RfcInterop> _interopMock = new Mock<RfcInterop>();

    private delegate void GetStringCallback(IntPtr dataHandle, string name, char[] buffer, uint bufferLength, out uint stringLength, out RfcErrorInfo errorInfo);

    [Theory]
    [InlineData("")]
    [InlineData("hello")]
    public void Extract_String_ShouldMapFromString(string value)
    {
        // Assert
        string stringValue = value;
        uint stringLength = (uint)stringValue.Length;
        RfcErrorInfo errorInfo;
        var resultCodeQueue = new Queue<RfcResultCode>();
        resultCodeQueue.Enqueue(RfcResultCode.RFC_BUFFER_TOO_SMALL);
        resultCodeQueue.Enqueue(RfcResultCode.RFC_OK);
        _interopMock
            .Setup(x => x.GetString(It.IsAny<IntPtr>(), It.IsAny<string>(), It.IsAny<char[]>(), It.IsAny<uint>(), out stringLength, out errorInfo))
            .Callback(new GetStringCallback((IntPtr dataHandle, string name, char[] buffer, uint bufferLength, out uint sl, out RfcErrorInfo ei) =>
            {
                ei = default;
                sl = stringLength;
                if (buffer.Length <= 0 || bufferLength <= 0)
                    return;
                Array.Copy(stringValue.ToCharArray(), buffer, stringValue.Length);
            }))
            .Returns(resultCodeQueue.Dequeue);

        // Act
        var result = OutputMapper.Extract<StringModel>(_interopMock.Object, DataHandle);

        // Assert
        uint discard;
        _interopMock.Verify(
            x => x.GetString(DataHandle, "STRINGVALUE", Array.Empty<char>(), 0, out discard, out errorInfo),
            Times.Once);
        _interopMock.Verify(
            x => x.GetString(DataHandle, "STRINGVALUE", It.IsAny<char[]>(), stringLength + 1, out discard, out errorInfo),
            Times.Once);
        result.ShouldNotBeNull();
        result.StringValue.ShouldBe(stringValue);
    }

    [Fact]
    public void Extract_EmptyString_ShouldMapAsEmptyString()
    {
        // Arrange
        RfcErrorInfo errorInfo;
        uint stringLength = 0;
        _interopMock.Setup(x => x.GetString(It.IsAny<IntPtr>(), It.IsAny<string>(), It.IsAny<char[]>(), It.IsAny<uint>(), out stringLength, out errorInfo));

        // Act
        var result = OutputMapper.Extract<StringModel>(_interopMock.Object, DataHandle);

        // Assert
        uint discard;
        _interopMock.Verify(
            x => x.GetString(DataHandle, "STRINGVALUE", Array.Empty<char>(), 0, out discard, out errorInfo),
            Times.Once);
        result.ShouldNotBeNull();
        result.StringValue.ShouldBeEmpty();
    }

    private sealed class StringModel
    {
        public string StringValue { get; set; }
    }

    [Theory]
    [InlineData(123)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void Extract_Int_ShouldMapFromInt(int value)
    {
        // Arrange
        int intValue = value;
        RfcErrorInfo errorInfo;
        _interopMock.Setup(x => x.GetInt(DataHandle, "INTVALUE", out intValue, out errorInfo));

        // Act
        var result = OutputMapper.Extract<IntModel>(_interopMock.Object, DataHandle);

        // Assert
        result.ShouldNotBeNull();
        result.IntValue.ShouldBe(intValue);
    }

    private sealed class IntModel
    {
        public int IntValue { get; set; }
    }

    [Theory]
    [InlineData(66778L)]
    [InlineData(long.MinValue)]
    [InlineData(long.MaxValue)]
    public void Extract_Long_ShouldMapFromInt8(long value)
    {
        // Arrange
        var longValue = value;
        RfcErrorInfo errorInfo;
        _interopMock.Setup(x => x.GetInt8(DataHandle, "LONGVALUE", out longValue, out errorInfo));

        // Act
        var result = OutputMapper.Extract<LongModel>(_interopMock.Object, DataHandle);

        // Assert
        result.ShouldNotBeNull();
        result.LongValue.ShouldBe(longValue);
    }

    private sealed class LongModel
    {
        public long LongValue { get; set; }
    }

    [Theory]
    [InlineData(1234.5d)]
    [InlineData(double.MinValue)]
    [InlineData(double.MaxValue)]
    public void Extract_Double_ShouldMapFromFloat(double value)
    {
        // Arrange
        var doubleValue = value;
        RfcErrorInfo errorInfo;
        _interopMock.Setup(x => x.GetFloat(DataHandle, "DOUBLEVALUE", out doubleValue, out errorInfo));

        // Act
        var result = OutputMapper.Extract<DoubleModel>(_interopMock.Object, DataHandle);

        // Assert
        result.ShouldNotBeNull();
        result.DoubleValue.ShouldBe(doubleValue);
    }

    private sealed class DoubleModel
    {
        public double DoubleValue { get; set; }
    }

    [Theory]
    [InlineData(123.56)]
    [InlineData(-123.56)]
    public void Extract_Decimal_ShouldMapFromDecimalString(decimal value)
    {
        // Assert
        string stringValue = value.ToString("G", CultureInfo.InvariantCulture);
        uint stringLength = (uint)stringValue.Length;
        RfcErrorInfo errorInfo;
        var resultCodeQueue = new Queue<RfcResultCode>();
        resultCodeQueue.Enqueue(RfcResultCode.RFC_BUFFER_TOO_SMALL);
        resultCodeQueue.Enqueue(RfcResultCode.RFC_OK);
        _interopMock
            .Setup(x => x.GetString(It.IsAny<IntPtr>(), It.IsAny<string>(), It.IsAny<char[]>(), It.IsAny<uint>(), out stringLength, out errorInfo))
            .Callback(new GetStringCallback((IntPtr dataHandle, string name, char[] buffer, uint bufferLength, out uint sl, out RfcErrorInfo ei) =>
            {
                ei = default;
                sl = stringLength;
                if (buffer.Length <= 0 || bufferLength <= 0)
                    return;
                Array.Copy(stringValue.ToCharArray(), buffer, stringValue.Length);
            }))
            .Returns(resultCodeQueue.Dequeue);

        // Act
        var result = OutputMapper.Extract<DecimalModel>(_interopMock.Object, DataHandle);

        // Assert
        uint discard;
        _interopMock.Verify(
            x => x.GetString(DataHandle, "DECIMALVALUE", Array.Empty<char>(), 0, out discard, out errorInfo),
            Times.Once);
        _interopMock.Verify(
            x => x.GetString(DataHandle, "DECIMALVALUE", It.IsAny<char[]>(), stringLength + 1, out discard, out errorInfo),
            Times.Once);
        result.ShouldNotBeNull();
        result.DecimalValue.ShouldBe(value);
    }

    [Fact]
    public void Extract_Decimal_EmptyString_ShouldMapToDecimalZero()
    {
        // Assert
        uint stringLength = 0;
        RfcErrorInfo errorInfo;
        _interopMock.Setup(x => x.GetString(It.IsAny<IntPtr>(), It.IsAny<string>(), It.IsAny<char[]>(), It.IsAny<uint>(), out stringLength, out errorInfo));

        // Act
        var result = OutputMapper.Extract<DecimalModel>(_interopMock.Object, DataHandle);

        // Assert
        uint discard;
        _interopMock.Verify(
            x => x.GetString(DataHandle, "DECIMALVALUE", Array.Empty<char>(), 0, out discard, out errorInfo),
            Times.Once);
        result.ShouldNotBeNull();
        result.DecimalValue.ShouldBe(0M);
    }

    private sealed class DecimalModel
    {
        public decimal DecimalValue { get; set; }
    }

    private delegate void GetXStringCallback(IntPtr dataHandle, string name, byte[] buffer, uint bufferLength, out uint xstringLength, out RfcErrorInfo errorInfo);

    [Theory]
    [InlineData(new byte[] { 1 })]
    [InlineData(new byte[] { 1, 2 })]
    [InlineData(new byte[] { 1, 2, 3 })]
    public void Extract_ByteBufferLengthArray_ShouldMapFromByteArray(byte[] value)
    {
        // Arrange
        uint byteLength = (uint)value.Length;
        RfcErrorInfo errorInfo;
        _interopMock
           .Setup(x => x.GetXString(It.IsAny<IntPtr>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<uint>(), out byteLength, out errorInfo))
           .Callback(new GetXStringCallback((IntPtr dataHandle, string name, byte[] buffer, uint bufferLength, out uint sl, out RfcErrorInfo ei) =>
           {
               ei = default;
               sl = byteLength;
               Array.Copy(value, buffer, value.Length);
           }));

        // Act
        var result = OutputMapper.Extract<FixedLengthBytesModel>(_interopMock.Object, DataHandle);

        // Assert
        uint discard;
        _interopMock.Verify(
            x => x.GetXString(DataHandle, "BYTESVALUE", It.IsAny<byte[]>(), 3, out discard, out errorInfo),
            Times.Once);
        result.ShouldNotBeNull();
        for (int i = 0; i < value.Length; i++)
            result.BytesValue[i].ShouldBe(value[i]);
    }

    [Theory]
    [InlineData(new byte[] { 1 })]
    [InlineData(new byte[] { 1, 2 })]
    [InlineData(new byte[] { 1, 2, 3 })]
    public void Extract_ByteArray_ShouldMapFromByteArray(byte[] value)
    {
        // Arrange
        uint byteLength = (uint)value.Length;
        RfcErrorInfo errorInfo;
        var resultCodeQueue = new Queue<RfcResultCode>();
        resultCodeQueue.Enqueue(RfcResultCode.RFC_BUFFER_TOO_SMALL);
        resultCodeQueue.Enqueue(RfcResultCode.RFC_OK);
        _interopMock
            .Setup(x => x.GetXString(It.IsAny<IntPtr>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<uint>(), out byteLength, out errorInfo))
            .Callback(new GetXStringCallback((IntPtr dataHandle, string name, byte[] buffer, uint bufferLength, out uint sl, out RfcErrorInfo ei) =>
            {
                ei = default;
                sl = byteLength;
                if (buffer.Length <= 0 || bufferLength <= 0)
                    return;
                Array.Copy(value, buffer, value.Length);
            }))
            .Returns(resultCodeQueue.Dequeue);

        // Act
        var result = OutputMapper.Extract<BytesModel>(_interopMock.Object, DataHandle);

        // Assert
        uint discard;
        _interopMock.Verify(
            x => x.GetXString(DataHandle, "BYTESVALUE", Array.Empty<byte>(), 0, out discard, out errorInfo),
            Times.Once);
        _interopMock.Verify(
            x => x.GetXString(DataHandle, "BYTESVALUE", It.IsAny<byte[]>(), byteLength, out discard, out errorInfo),
            Times.Once);
        result.ShouldNotBeNull();
        result.BytesValue.ShouldBeEquivalentTo(value);
    }

    [Fact]
    public void Extract_EmptyByteArray_ShouldMapAsEmptyByteArray()
    {
        // Arrange
        uint byteLength = 0;
        RfcErrorInfo errorInfo;
        _interopMock.Setup(x => x.GetXString(It.IsAny<IntPtr>(), It.IsAny<string>(), It.IsAny<byte[]>(), byteLength, out byteLength, out errorInfo));

        // Act
        var result = OutputMapper.Extract<BytesModel>(_interopMock.Object, DataHandle);

        // Assert
        uint discard;
        _interopMock.Verify(
            x => x.GetXString(DataHandle, "BYTESVALUE", Array.Empty<byte>(), byteLength, out discard, out errorInfo),
            Times.Once);
        result.ShouldNotBeNull();
        result.BytesValue.ShouldBeEmpty();
    }

    private sealed class FixedLengthBytesModel
    {
        [SapBufferLength(3)]
        public byte[] BytesValue { get; set; }
    }

    private sealed class BytesModel
    {
        public byte[] BytesValue { get; set; }
    }

    private delegate void GetCharsCallback(IntPtr dataHandle, string name, char[] buffer, uint bufferLength, out RfcErrorInfo errorInfo);

    [Theory]
    [InlineData(new char[] { '1', '2', '3' })]
    public void Extract_CharArray_ShouldMapFromCharArray(char[] value)
    {
        // Arrange
        RfcErrorInfo errorInfo;

        _interopMock
            .Setup(x => x.GetChars(It.IsAny<IntPtr>(), It.IsAny<string>(), It.IsAny<char[]>(), It.IsAny<uint>(), out errorInfo))
            .Callback(new GetCharsCallback((IntPtr dataHandle, string name, char[] buffer, uint bufferLength, out RfcErrorInfo ei) =>
            {
                Array.Copy(value, buffer, value.Length);
                ei = default;
            }));

        // Act
        var result = OutputMapper.Extract<CharsModel>(_interopMock.Object, DataHandle);

        // Assert
        _interopMock.Verify(
            x => x.GetChars(DataHandle, "CHARSVALUE", It.IsAny<char[]>(), 3, out errorInfo),
            Times.Once);
        result.ShouldNotBeNull();
        result.CharsValue.ShouldBeEquivalentTo(value);
    }

    [Fact]
    public void Extract_EmptyCharArray_ShouldMapAsEmptyCharArray()
    {
        // Arrange
        RfcErrorInfo errorInfo;
        uint bufferLength = 0;
        _interopMock.Setup(x => x.GetChars(It.IsAny<IntPtr>(), It.IsAny<string>(), It.IsAny<char[]>(), bufferLength, out errorInfo));

        // Act
        var result = OutputMapper.Extract<EmptyCharsModel>(_interopMock.Object, DataHandle);

        // Assert
        _interopMock.Verify(
            x => x.GetChars(DataHandle, "CHARSVALUE", Array.Empty<char>(), 0, out errorInfo),
            Times.Once);
        result.ShouldNotBeNull();
        result.CharsValue.ShouldBeEmpty();
    }

    private sealed class CharsModel
    {
        [SapBufferLength(3)]
        public char[] CharsValue { get; set; }
    }

    private sealed class EmptyCharsModel
    {
        public char[] CharsValue { get; set; }
    }

    private delegate void GetDateCallback(IntPtr dataHandle, string name, char[] buffer, out RfcErrorInfo errorInfo);

    [Fact]
    public void Extract_DateTime_ShouldMapFromDate()
    {
        // Arrange
        const string value = "20200405";
        RfcErrorInfo errorInfo;
        _interopMock
            .Setup(x => x.GetDate(It.IsAny<IntPtr>(), It.IsAny<string>(), It.IsAny<char[]>(), out errorInfo))
            .Callback(new GetDateCallback((IntPtr dataHandle, string name, char[] buffer, out RfcErrorInfo ei) =>
            {
                Array.Copy(value.ToCharArray(), buffer, value.Length);
                ei = default;
            }));

        // Act
        var result = OutputMapper.Extract<DateTimeModel>(_interopMock.Object, DataHandle);

        // Assert
        _interopMock.Verify(
            x => x.GetDate(DataHandle, "DATETIMEVALUE", It.IsAny<char[]>(), out errorInfo),
            Times.Once);
        _interopMock.Verify(
            x => x.GetDate(DataHandle, "NULLABLEDATETIMEVALUE", It.IsAny<char[]>(), out errorInfo),
            Times.Once);
        result.ShouldNotBeNull();
        result.DateTimeValue.ShouldBe(new DateTime(2020, 04, 05));
        result.NullableDateTimeValue.ShouldBe(new DateTime(2020, 04, 05));
    }

    [Theory]
    [InlineData("00000000")]
    [InlineData("        ")]
    [InlineData("abcdefgh")]
    public void Extract_NonNullableDateTime_ZeroOrEmptyOrInvalidDate_ShouldMapToMinimumDateTime(string value)
    {
        // Arrange
        RfcErrorInfo errorInfo;
        _interopMock
            .Setup(x => x.GetDate(It.IsAny<IntPtr>(), It.IsAny<string>(), It.IsAny<char[]>(), out errorInfo))
            .Callback(new GetDateCallback((IntPtr dataHandle, string name, char[] buffer, out RfcErrorInfo ei) =>
            {
                Array.Copy(value.ToCharArray(), buffer, value.Length);
                ei = default;
            }));

        // Act
        var result = OutputMapper.Extract<DateTimeModel>(_interopMock.Object, DataHandle);

        // Assert
        _interopMock.Verify(
            x => x.GetDate(DataHandle, "DATETIMEVALUE", It.IsAny<char[]>(), out errorInfo),
            Times.Once);
        result.ShouldNotBeNull();
        result.DateTimeValue.ShouldBe(DateTime.MinValue);
    }

    [Theory]
    [InlineData("00000000")]
    [InlineData("        ")]
    [InlineData("abcdefgh")]
    public void Extract_NullableDateTime_ZeroOrEmptyOrInvalidDate_ShouldMapToNullDateTime(string value)
    {
        // Arrange
        RfcErrorInfo errorInfo;
        _interopMock
            .Setup(x => x.GetDate(It.IsAny<IntPtr>(), It.IsAny<string>(), It.IsAny<char[]>(), out errorInfo))
            .Callback(new GetDateCallback((IntPtr dataHandle, string name, char[] buffer, out RfcErrorInfo ei) =>
            {
                Array.Copy(value.ToCharArray(), buffer, value.Length);
                ei = default;
            }));

        // Act
        var result = OutputMapper.Extract<DateTimeModel>(_interopMock.Object, DataHandle);

        // Assert
        _interopMock.Verify(
            x => x.GetDate(DataHandle, "NULLABLEDATETIMEVALUE", It.IsAny<char[]>(), out errorInfo),
            Times.Once);
        result.ShouldNotBeNull();
        result.NullableDateTimeValue.ShouldBeNull();
    }

    private sealed class DateTimeModel
    {
        public DateTime DateTimeValue { get; set; }

        public DateTime? NullableDateTimeValue { get; set; }
    }

    private delegate void GetTimeCallback(IntPtr dataHandle, string name, char[] buffer, out RfcErrorInfo errorInfo);

    [Fact]
    public void Extract_TimeSpan_ShouldMapFromTime()
    {
        // Arrange
        const string value = "123456";
        RfcErrorInfo errorInfo;
        _interopMock
            .Setup(x => x.GetTime(It.IsAny<IntPtr>(), It.IsAny<string>(), It.IsAny<char[]>(), out errorInfo))
            .Callback(new GetTimeCallback((IntPtr dataHandle, string name, char[] buffer, out RfcErrorInfo ei) =>
            {
                Array.Copy(value.ToCharArray(), buffer, value.Length);
                ei = default;
            }));

        // Act
        var result = OutputMapper.Extract<TimeSpanModel>(_interopMock.Object, DataHandle);

        // Assert
        _interopMock.Verify(
            x => x.GetTime(DataHandle, "TIMESPANVALUE", It.IsAny<char[]>(), out errorInfo),
            Times.Once);
        _interopMock.Verify(
            x => x.GetTime(DataHandle, "NULLABLETIMESPANVALUE", It.IsAny<char[]>(), out errorInfo),
            Times.Once);
        result.ShouldNotBeNull();
        result.TimeSpanValue.ShouldBe(new TimeSpan(12, 34, 56));
        result.NullableTimeSpanValue.ShouldBe(new TimeSpan(12, 34, 56));
    }

    [Theory]
    [InlineData("000000")]
    [InlineData("      ")]
    [InlineData("abcdef")]
    public void Extract_NonNullableTimeSpan_ZeroOrEmptyOrInvalidTime_ShouldMapToZeroTimeSpan(string value)
    {
        // Arrange
        RfcErrorInfo errorInfo;
        _interopMock
            .Setup(x => x.GetTime(It.IsAny<IntPtr>(), It.IsAny<string>(), It.IsAny<char[]>(), out errorInfo))
            .Callback(new GetTimeCallback((IntPtr dataHandle, string name, char[] buffer, out RfcErrorInfo ei) =>
            {
                Array.Copy(value.ToCharArray(), buffer, value.Length);
                ei = default;
            }));

        // Act
        var result = OutputMapper.Extract<TimeSpanModel>(_interopMock.Object, DataHandle);

        // Assert
        _interopMock.Verify(
            x => x.GetTime(DataHandle, "TIMESPANVALUE", It.IsAny<char[]>(), out errorInfo),
            Times.Once);
        result.ShouldNotBeNull();
        result.TimeSpanValue.ShouldBe(TimeSpan.Zero);
    }

    [Theory]
    [InlineData("000000")]
    [InlineData("      ")]
    [InlineData("abcdef")]
    public void Extract_NullableTimeSpan_ZeroOrEmptyOrInvalidTime_ShouldMapToNullTimeSpan(string value)
    {
        // Arrange
        RfcErrorInfo errorInfo;
        _interopMock
            .Setup(x => x.GetTime(It.IsAny<IntPtr>(), It.IsAny<string>(), It.IsAny<char[]>(), out errorInfo))
            .Callback(new GetTimeCallback((IntPtr dataHandle, string name, char[] buffer, out RfcErrorInfo ei) =>
            {
                Array.Copy(value.ToCharArray(), buffer, value.Length);
                ei = default;
            }));

        // Act
        var result = OutputMapper.Extract<TimeSpanModel>(_interopMock.Object, DataHandle);

        // Assert
        _interopMock.Verify(
            x => x.GetTime(DataHandle, "NULLABLETIMESPANVALUE", It.IsAny<char[]>(), out errorInfo),
            Times.Once);
        result.ShouldNotBeNull();
        result.NullableTimeSpanValue.ShouldBeNull();
    }

    private sealed class TimeSpanModel
    {
        public TimeSpan TimeSpanValue { get; set; }

        public TimeSpan? NullableTimeSpanValue { get; set; }
    }

    [Fact]
    public void Extract_TableWithRows_ShouldMapToArrayOfElements()
    {
        // Arrange
        var tableHandle = (IntPtr)3334;
        var rowHandle = (IntPtr)4445;
        uint rowCount = 3;
        int intValue = 888;
        RfcErrorInfo errorInfo;
        _interopMock.Setup(x => x.GetTable(It.IsAny<IntPtr>(), It.IsAny<string>(), out tableHandle, out errorInfo));
        _interopMock.Setup(x => x.GetRowCount(It.IsAny<IntPtr>(), out rowCount, out errorInfo));
        _interopMock.Setup(x => x.MoveToFirstRow(It.IsAny<IntPtr>(), out errorInfo));
        _interopMock.Setup(x => x.GetCurrentRow(It.IsAny<IntPtr>(), out errorInfo)).Returns(rowHandle);
        _interopMock.Setup(x => x.GetInt(It.IsAny<IntPtr>(), It.IsAny<string>(), out intValue, out errorInfo));

        // Act
        var result = OutputMapper.Extract<ArrayModel>(_interopMock.Object, DataHandle);

        // Assert
        _interopMock.Verify(
            x => x.GetTable(DataHandle, "ELEMENTS", out tableHandle, out errorInfo),
            Times.Once);
        _interopMock.Verify(
            x => x.GetRowCount(tableHandle, out rowCount, out errorInfo),
            Times.Once);
        _interopMock.Verify(
            x => x.MoveToFirstRow(tableHandle, out errorInfo),
            Times.Once);
        _interopMock.Verify(
            x => x.GetCurrentRow(tableHandle, out errorInfo),
            Times.Exactly(3));
        _interopMock.Verify(
            x => x.GetInt(rowHandle, "VALUE", out intValue, out errorInfo),
            Times.Exactly(3));
        _interopMock.Verify(x => x.MoveToNextRow(tableHandle, out errorInfo), Times.Exactly(3));
        result.ShouldNotBeNull();
        result.Elements.ShouldHaveCount(3);
        result.Elements.First().Value.ShouldBe(888);
    }

    [Fact]
    public void Extract_TableWithLessRowsThanAnnounced_ShouldReturnExtractedRows()
    {
        // Arrange
        var tableHandle = (IntPtr)3334;
        var rowHandle = (IntPtr)4445;
        uint rowCount = 3;
        int intValue = 888;
        RfcErrorInfo errorInfo;
        _interopMock.Setup(x => x.GetTable(It.IsAny<IntPtr>(), It.IsAny<string>(), out tableHandle, out errorInfo));
        _interopMock.Setup(x => x.GetRowCount(It.IsAny<IntPtr>(), out rowCount, out errorInfo));
        _interopMock.Setup(x => x.MoveToFirstRow(It.IsAny<IntPtr>(), out errorInfo));
        _interopMock.Setup(x => x.GetCurrentRow(It.IsAny<IntPtr>(), out errorInfo)).Returns(rowHandle);
        _interopMock.Setup(x => x.GetInt(It.IsAny<IntPtr>(), It.IsAny<string>(), out intValue, out errorInfo));

        _interopMock
            .Setup(x => x.MoveToNextRow(It.IsAny<IntPtr>(), out errorInfo))
            .Returns(RfcResultCode.RFC_TABLE_MOVE_EOF);

        // Act
        var result = OutputMapper.Extract<ArrayModel>(_interopMock.Object, DataHandle);

        // Assert
        result.ShouldNotBeNull();
        result.Elements.ShouldHaveCount(1);
    }

    [Fact]
    public void Extract_TableWithZeroRows_ShouldNotCallMoveToFirstRow()
    {
        // Arrange
        var tableHandle = (IntPtr)3334;
        var rowHandle = (IntPtr)4445;
        uint rowCount = 0;
        RfcErrorInfo errorInfo;
        _interopMock.Setup(x => x.GetTable(It.IsAny<IntPtr>(), It.IsAny<string>(), out tableHandle, out errorInfo));
        _interopMock.Setup(x => x.GetRowCount(It.IsAny<IntPtr>(), out rowCount, out errorInfo));
        _interopMock.Setup(x => x.MoveToFirstRow(It.IsAny<IntPtr>(), out errorInfo));
        _interopMock.Setup(x => x.GetCurrentRow(It.IsAny<IntPtr>(), out errorInfo)).Returns(rowHandle);

        _interopMock
            .Setup(x => x.MoveToNextRow(It.IsAny<IntPtr>(), out errorInfo))
            .Returns(RfcResultCode.RFC_TABLE_MOVE_EOF);

        // Act
        var result = OutputMapper.Extract<ArrayModel>(_interopMock.Object, DataHandle);

        // Assert
        _interopMock.Verify(
            x => x.GetTable(DataHandle, "ELEMENTS", out tableHandle, out errorInfo),
            Times.Once);
        _interopMock.Verify(
            x => x.GetRowCount(tableHandle, out rowCount, out errorInfo),
            Times.Once);
        _interopMock.Verify(
            x => x.MoveToFirstRow(tableHandle, out errorInfo),
            Times.Never);
        _interopMock.Verify(
            x => x.GetCurrentRow(tableHandle, out errorInfo),
            Times.Never);
        _interopMock.Verify(x => x.MoveToNextRow(tableHandle, out errorInfo), Times.Never);
        result.ShouldNotBeNull();
        result.Elements.ShouldHaveCount(0);
    }

    private sealed class ArrayModel
    {
        public ArrayElement[] Elements { get; set; }
    }

    private sealed class ArrayElement
    {
        public int Value { get; set; }
    }

    [Fact]
    public void Extract_TableWithRows_ShouldMapToEnumerableOfElements()
    {
        // Arrange
        var tableHandle = (IntPtr)3334;
        var rowHandle = (IntPtr)4445;
        uint rowCount = 3;
        int intValue = 888;
        RfcErrorInfo errorInfo;
        _interopMock.Setup(x => x.GetTable(It.IsAny<IntPtr>(), It.IsAny<string>(), out tableHandle, out errorInfo));
        _interopMock.Setup(x => x.GetRowCount(It.IsAny<IntPtr>(), out rowCount, out errorInfo));
        _interopMock.Setup(x => x.MoveToFirstRow(It.IsAny<IntPtr>(), out errorInfo));
        _interopMock.Setup(x => x.GetCurrentRow(It.IsAny<IntPtr>(), out errorInfo)).Returns(rowHandle);
        _interopMock.Setup(x => x.GetInt(It.IsAny<IntPtr>(), It.IsAny<string>(), out intValue, out errorInfo));

        // Act
        var result = OutputMapper.Extract<EnumerableModel>(_interopMock.Object, DataHandle);

        // access all elements at least once to complete yield
        var enumerator = result.Elements.GetEnumerator();

        // first moveNext starts the yield and counts as MoveToFirstRow
        // subsequent as MoveToNextRow
        enumerator.MoveNext();
        enumerator.MoveNext();
        enumerator.MoveNext();

        // Assert
        _interopMock.Verify(
            x => x.GetTable(DataHandle, "ELEMENTS", out tableHandle, out errorInfo),
            Times.Once);
        _interopMock.Verify(
            x => x.GetRowCount(tableHandle, out rowCount, out errorInfo),
            Times.Once);
        _interopMock.Verify(
            x => x.MoveToFirstRow(tableHandle, out errorInfo),
            Times.Once);
        _interopMock.Verify(
            x => x.GetCurrentRow(tableHandle, out errorInfo),
            Times.Exactly(3));
        _interopMock.Verify(
            x => x.GetInt(rowHandle, "VALUE", out intValue, out errorInfo),
            Times.Exactly(3));
        _interopMock.Verify(
            x => x.MoveToNextRow(tableHandle, out errorInfo),
            Times.Exactly(2));

        result.ShouldNotBeNull();
        result.Elements.First().Value.ShouldBe(888);
    }

    [Fact]
    public void Extract_TableWithRows_ShouldNotYieldEnumerableElementsIfNotAccessed()
    {
        // Arrange
        var tableHandle = (IntPtr)3334;
        var rowHandle = (IntPtr)4445;
        uint rowCount = 3;
        int intValue = 888;
        RfcErrorInfo errorInfo;
        _interopMock.Setup(x => x.GetTable(It.IsAny<IntPtr>(), It.IsAny<string>(), out tableHandle, out errorInfo));
        _interopMock.Setup(x => x.GetRowCount(It.IsAny<IntPtr>(), out rowCount, out errorInfo));
        _interopMock.Setup(x => x.MoveToFirstRow(It.IsAny<IntPtr>(), out errorInfo));
        _interopMock.Setup(x => x.GetCurrentRow(It.IsAny<IntPtr>(), out errorInfo)).Returns(rowHandle);
        _interopMock.Setup(x => x.GetInt(It.IsAny<IntPtr>(), It.IsAny<string>(), out intValue, out errorInfo));

        // Act
        var result = OutputMapper.Extract<EnumerableModel>(_interopMock.Object, DataHandle);

        // Assert
        _interopMock.Verify(
            x => x.GetTable(DataHandle, "ELEMENTS", out tableHandle, out errorInfo),
            Times.Once);
        _interopMock.Verify(
            x => x.GetRowCount(tableHandle, out rowCount, out errorInfo),
            Times.Once);
        _interopMock.Verify(
            x => x.MoveToFirstRow(tableHandle, out errorInfo),
            Times.Never);
        _interopMock.Verify(
            x => x.GetCurrentRow(tableHandle, out errorInfo),
            Times.Never);
        _interopMock.Verify(
            x => x.GetInt(rowHandle, "VALUE", out intValue, out errorInfo),
            Times.Never);
        _interopMock.Verify(x => x.MoveToNextRow(tableHandle, out errorInfo), Times.Never);

        result.ShouldNotBeNull();
        result.Elements.ShouldNotBeNull();
    }

    private sealed class EnumerableModel
    {
        public IEnumerable<EnumerableElement> Elements { get; set; }
    }

    private sealed class EnumerableElement
    {
        public int Value { get; set; }
    }

    [Fact]
    public void Extract_Structure_ShouldMapToNestedObject()
    {
        // Arrange
        var structHandle = (IntPtr)443534;
        var intValue = 123;
        RfcErrorInfo errorInfo;
        _interopMock.Setup(x => x.GetStructure(It.IsAny<IntPtr>(), It.IsAny<string>(), out structHandle, out errorInfo));
        _interopMock.Setup(x => x.GetInt(It.IsAny<IntPtr>(), It.IsAny<string>(), out intValue, out errorInfo));

        // Act
        var result = OutputMapper.Extract<NestedModel>(_interopMock.Object, DataHandle);

        // Assert
        _interopMock.Verify(
            x => x.GetStructure(DataHandle, "INNERMODEL", out structHandle, out errorInfo),
            Times.Once);
        _interopMock.Verify(
            x => x.GetInt(structHandle, "VALUE", out intValue, out errorInfo),
            Times.Once);
        result.ShouldNotBeNull();
        result.InnerModel.ShouldNotBeNull();
        result.InnerModel.Value.ShouldBe(123);
    }

    private sealed class NestedModel
    {
        public InnerModel InnerModel { get; set; }
    }

    private sealed class InnerModel
    {
        public int Value { get; set; }
    }

    [Fact]
    public void Extract_PropertyWithSapNameAttribute_ShouldMapUsingRfcName()
    {
        // Arrange
        int value = 334;
        RfcErrorInfo errorInfo;
        _interopMock.Setup(x => x.GetInt(DataHandle, "I34", out value, out errorInfo));

        // Act
        var result = OutputMapper.Extract<IntAttributeModel>(_interopMock.Object, DataHandle);

        // Assert
        result.ShouldNotBeNull();
        result.IntValue.ShouldBe(334);
    }

    private sealed class IntAttributeModel
    {
        [SapName("I34")]
        public int IntValue { get; set; }
    }

    [Fact]
    public void Extract_PropertyWithIgnoreAttribute_ShouldBeIgnored()
    {
        // Arrange
        int value = 123;
        RfcErrorInfo errorInfo;
        _interopMock.Setup(x => x.GetInt(DataHandle, "VALUE", out value, out errorInfo));

        // Act
        var result = OutputMapper.Extract<IgnoreAttributeModel>(_interopMock.Object, DataHandle);

        // Assert
        _interopMock.Verify(x => x.GetInt(DataHandle, "VALUE", out value, out errorInfo), Times.Never);

        result.ShouldNotBeNull();
        result.Value.ShouldBe(0);
    }

    private sealed class IgnoreAttributeModel
    {
        [SapIgnore]
        public int Value { get; set; }
    }

    [Fact]
    public void Extract_UnknownTypeThatCannotBeExtracted_ShouldThrowException()
    {
        // Arrange & Act
        var action = () => OutputMapper.Extract<UnknownTypeModel>(_interopMock.Object, DataHandle);

        // Assert
        action.ShouldThrow<InvalidOperationException>()
            .Message.ShouldBe("No matching extract method found for type Single");
    }

    private sealed class UnknownTypeModel
    {
        public float Float { get; set; }
    }
}
