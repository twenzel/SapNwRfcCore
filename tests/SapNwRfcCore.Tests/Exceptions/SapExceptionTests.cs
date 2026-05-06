using System;
using AutoFixture;
using SapNwRfcCore.Exceptions;
using SapNwRfcCore.Internal.Interop;
using Shouldly;
using Xunit;

namespace SapNwRfcCore.Tests.Exceptions;

public sealed class SapExceptionTests
{
    private static readonly Fixture Fixture = new Fixture();

    [Fact]
    public void ShouldInheritFromException()
    {
        // Assert
        typeof(Exception).IsAssignableFrom(typeof(SapException)).ShouldBeTrue();
    }

    [Fact]
    public void Constructor_CodeAndErrorInfoMessage_ShouldSetMessageAndSetResultCode()
    {
        // Act
        var errorInfo = new RfcErrorInfo { Message = "Some message" };
        var exception = new SapException(RfcResultCode.RFC_NOT_FOUND, errorInfo);

        // Assert
        exception.Message.ShouldBe("SAP RFC Error: RFC_NOT_FOUND with message: Some message");
        exception.ResultCode.ShouldBe(SapResultCode.NotFound);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Constructor_CodeAndErrorInfoWithoutMessage_ShouldSetFixedMessageAndSetResultCode(string message)
    {
        // Act
        var errorInfo = new RfcErrorInfo { Message = message };
        var exception = new SapException(RfcResultCode.RFC_CANCELED, errorInfo);

        // Assert
        exception.Message.ShouldBe("SAP RFC Error: RFC_CANCELED");
        exception.ResultCode.ShouldBe(SapResultCode.Canceled);
    }

    [Fact]
    public void Constructor_ShouldCopyErrorInfo()
    {
        // Arrange
        var errorInfo = new RfcErrorInfo
        {
            ErrorGroup = RfcErrorGroup.LOGON_FAILURE,
            Key = "Some Key",
            Message = "Some Message",
            AbapMsgClass = "Some AbapMsgClass",
            AbapMsgType = "Some AbapMsgType",
            AbapMsgNumber = "Some AbapMsgNumber",
            AbapMsgV1 = "Some AbapMsgV1",
            AbapMsgV2 = "Some AbapMsgV2",
            AbapMsgV3 = "Some AbapMsgV3",
            AbapMsgV4 = "Some AbapMsgV4",
        };

        // Act
        var exception = new SapException(RfcResultCode.RFC_CLOSED, errorInfo);

        // Assert
        exception.ErrorInfo.ErrorGroup.ShouldBe(SapErrorGroup.LogonFailure);
        exception.ErrorInfo.Key.ShouldBe(errorInfo.Key);
        exception.ErrorInfo.Message.ShouldBe(errorInfo.Message);
        exception.ErrorInfo.AbapMessageClass.ShouldBe(errorInfo.AbapMsgClass);
        exception.ErrorInfo.AbapMessageType.ShouldBe(errorInfo.AbapMsgType);
        exception.ErrorInfo.AbapMessageNumber.ShouldBe(errorInfo.AbapMsgNumber);
        exception.ErrorInfo.AbapMessageV1.ShouldBe(errorInfo.AbapMsgV1);
        exception.ErrorInfo.AbapMessageV2.ShouldBe(errorInfo.AbapMsgV2);
        exception.ErrorInfo.AbapMessageV3.ShouldBe(errorInfo.AbapMsgV3);
        exception.ErrorInfo.AbapMessageV4.ShouldBe(errorInfo.AbapMsgV4);
    }
}
