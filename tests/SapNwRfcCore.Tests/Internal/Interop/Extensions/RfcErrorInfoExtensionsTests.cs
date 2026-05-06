using System;
using Moq;
using SapNwRfcCore.Exceptions;
using SapNwRfcCore.Internal.Interop;
using Shouldly;
using Xunit;

namespace SapNwRfcCore.Tests.Internal.Interop.Extensions;

public sealed class RfcErrorInfoExtensionsTests
{
    [Fact]
    public void ThrowOnError_NoError_ShouldNotThrow()
    {
        // Arrange
        var errorInfo = new RfcErrorInfo { Code = RfcResultCode.RFC_OK };

        // Act
        Action action = () => errorInfo.ThrowOnError();

        // Assert
        action.ShouldNotThrow();
    }

    [Fact]
    public void ThrowOnError_NoError_ShouldNotCallBeforeThrowAction()
    {
        // Arrange
        var errorInfo = new RfcErrorInfo { Code = RfcResultCode.RFC_OK };
        var beforeThrowActionMock = new Mock<Action>();

        // Act
        errorInfo.ThrowOnError(beforeThrowActionMock.Object);

        // Assert
        beforeThrowActionMock.Verify(x => x(), Times.Never);
    }

    [Fact]
    public void ThrowOnError_Error_ShouldCallBeforeThrowActionAndThrowRfcException()
    {
        // Arrange
        var errorInfo = new RfcErrorInfo
        {
            Code = RfcResultCode.RFC_CLOSED,
            Message = "Connection closed",
        };
        var beforeThrowActionMock = new Mock<Action>();

        // Act
        Action action = () => errorInfo.ThrowOnError(beforeThrowActionMock.Object);

        // Assert
        action.ShouldThrow<SapException>()
            .Message.ShouldBe("SAP RFC Error: RFC_CLOSED with message: Connection closed");
        beforeThrowActionMock.Verify(x => x(), Times.Once);
    }
}
