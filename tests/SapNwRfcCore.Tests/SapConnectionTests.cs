using System;
using AutoFixture;
using Moq;
using SapNwRfcCore.Exceptions;
using SapNwRfcCore.Internal.Interop;
using Shouldly;
using Xunit;

namespace SapNwRfcCore.Tests;

public sealed class SapConnectionTests
{
    private static readonly Fixture Fixture = new Fixture();
    private static readonly IntPtr RfcConnectionHandle = (IntPtr)12;
    private static readonly IntPtr FunctionDescriptionHandle = (IntPtr)34;
    private readonly Mock<RfcInterop> _interopMock = new Mock<RfcInterop>();

    public SapConnectionTests()
    {
        new SupportMutableValueTypesCustomization().Customize(Fixture);
    }

    [Fact]
    public void Connect_ConnectionSucceeds_ShouldOpenConnection()
    {
        // Arrange
        var parameters = new SapConnectionParameters { AppServerHost = "my-server.com" };
        var connection = new SapConnection(_interopMock.Object, parameters);

        // Act
        connection.Connect();

        // Assert
        RfcErrorInfo errorInfo;
        _interopMock.Verify(
            x => x.OpenConnection(It.IsAny<RfcConnectionParameter[]>(), 1, out errorInfo),
            Times.Once);
    }

    [Fact]
    public void Connect_ConnectionFailed_ShouldThrow()
    {
        // Arrange
        var connection = new SapConnection(_interopMock.Object, new SapConnectionParameters());
        var errorInfo = new RfcErrorInfo { Code = RfcResultCode.RFC_TIMEOUT };
        _interopMock
            .Setup(x => x.OpenConnection(It.IsAny<RfcConnectionParameter[]>(), It.IsAny<uint>(), out errorInfo));

        // Act
        Action action = () => connection.Connect();

        // Assert
        action.ShouldThrow<SapException>().Message.ShouldBe("SAP RFC Error: RFC_TIMEOUT");
    }

    [Fact]
    public void Disconnect_NotConnected_ShouldNotDisconnect()
    {
        // Arrange
        var connection = new SapConnection(_interopMock.Object, new SapConnectionParameters());

        // Act
        connection.Disconnect();

        // Assert
        RfcErrorInfo errorInfo;
        _interopMock.Verify(x => x.CloseConnection(It.IsAny<IntPtr>(), out errorInfo), Times.Never);
    }

    [Fact]
    public void Disconnect_Connected_ShouldDisconnect()
    {
        // Arrange
        var connection = new SapConnection(_interopMock.Object, new SapConnectionParameters());
        RfcErrorInfo errorInfo;
        _interopMock
            .Setup(x => x.OpenConnection(It.IsAny<RfcConnectionParameter[]>(), It.IsAny<uint>(), out errorInfo))
            .Returns(RfcConnectionHandle);

        connection.Connect();

        // Act
        connection.Disconnect();

        // Assert
        _interopMock.Verify(x => x.CloseConnection(RfcConnectionHandle, out errorInfo), Times.Once);
    }

    [Fact]
    public void Disconnect_DisconnectionFailed_ShouldThrowException()
    {
        // Arrange
        var connection = new SapConnection(_interopMock.Object, new SapConnectionParameters());
        RfcErrorInfo errorInfo;
        _interopMock
            .Setup(x => x.OpenConnection(It.IsAny<RfcConnectionParameter[]>(), It.IsAny<uint>(), out errorInfo))
            .Returns(RfcConnectionHandle);
        _interopMock
            .Setup(x => x.CloseConnection(It.IsAny<IntPtr>(), out errorInfo))
            .Returns(RfcResultCode.RFC_CANCELED);

        connection.Connect();

        // Act
        Action action = () => connection.Disconnect();

        // Assert
        action.ShouldThrow<SapException>()
            .Message.ShouldBe("SAP RFC Error: RFC_CANCELED");
    }

    [Fact]
    public void Dispose_ShouldDisconnect()
    {
        // Arrange
        var connection = new SapConnection(_interopMock.Object, new SapConnectionParameters());
        RfcErrorInfo errorInfo;
        _interopMock
            .Setup(x => x.OpenConnection(It.IsAny<RfcConnectionParameter[]>(), It.IsAny<uint>(), out errorInfo))
            .Returns(RfcConnectionHandle);

        connection.Connect();

        // Act
        connection.Dispose();

        // Assert
        _interopMock.Verify(x => x.CloseConnection(RfcConnectionHandle, out errorInfo), Times.Once);
    }

    [Fact]
    public void Dispose_DisconnectionFailed_ShouldNotThrow()
    {
        // Arrange
        var connection = new SapConnection(_interopMock.Object, new SapConnectionParameters());
        RfcErrorInfo errorInfo;
        _interopMock
            .Setup(x => x.OpenConnection(It.IsAny<RfcConnectionParameter[]>(), It.IsAny<uint>(), out errorInfo))
            .Returns(RfcConnectionHandle);
        _interopMock
            .Setup(x => x.CloseConnection(It.IsAny<IntPtr>(), out errorInfo))
            .Returns(RfcResultCode.RFC_CANCELED);

        connection.Connect();

        // Act
        Action action = () => connection.Dispose();

        // Assert
        action.ShouldNotThrow();
    }

    [Fact]
    public void IsConnected_Connected_ShouldReturnTrue()
    {
        // Arrange
        var connection = new SapConnection(_interopMock.Object, new SapConnectionParameters());
        RfcErrorInfo errorInfo;
        _interopMock
            .Setup(x => x.OpenConnection(It.IsAny<RfcConnectionParameter[]>(), It.IsAny<uint>(), out errorInfo))
            .Returns(RfcConnectionHandle);
        int isValid = 1;
        _interopMock
            .Setup(x => x.IsConnectionHandleValid(RfcConnectionHandle, out isValid, out errorInfo))
            .Returns(RfcResultCode.RFC_OK);

        connection.Connect();

        // Act
        bool isConnected = connection.IsValid;

        // Assert
        isConnected.ShouldBeTrue();
    }

    [Fact]
    public void IsConnected_Disconnected_ShouldReturnFalse()
    {
        // Arrange
        var connection = new SapConnection(_interopMock.Object, new SapConnectionParameters());
        RfcErrorInfo errorInfo;
        _interopMock
            .Setup(x => x.OpenConnection(It.IsAny<RfcConnectionParameter[]>(), It.IsAny<uint>(), out errorInfo))
            .Returns(RfcConnectionHandle);
        int isValid = 1;
        _interopMock
            .Setup(x => x.IsConnectionHandleValid(RfcConnectionHandle, out isValid, out errorInfo))
            .Returns(RfcResultCode.RFC_OK);

        connection.Connect();
        connection.Disconnect();

        // Act
        bool isConnected = connection.IsValid;

        // Assert
        isConnected.ShouldBeFalse();
    }

    [Fact]
    public void IsConnected_ConnectedButLibraryReturnsError_ShouldReturnFalse()
    {
        // Arrange
        var connection = new SapConnection(_interopMock.Object, new SapConnectionParameters());
        RfcErrorInfo errorInfo;
        _interopMock
            .Setup(x => x.OpenConnection(It.IsAny<RfcConnectionParameter[]>(), It.IsAny<uint>(), out errorInfo))
            .Returns(RfcConnectionHandle);
        int isValid = 1;
        _interopMock
            .Setup(x => x.IsConnectionHandleValid(RfcConnectionHandle, out isValid, out errorInfo))
            .Returns(RfcResultCode.RFC_CLOSED);

        connection.Connect();

        // Act
        bool isConnected = connection.IsValid;

        // Assert
        isConnected.ShouldBeFalse();
    }

    [Fact]
    public void IsConnected_ConnectedButLibrarySaysWereDisconnected_ShouldReturnFalse()
    {
        // Arrange
        var connection = new SapConnection(_interopMock.Object, new SapConnectionParameters());
        RfcErrorInfo errorInfo;
        _interopMock
            .Setup(x => x.OpenConnection(It.IsAny<RfcConnectionParameter[]>(), It.IsAny<uint>(), out errorInfo))
            .Returns(RfcConnectionHandle);
        int isValid = 0;
        _interopMock
            .Setup(x => x.IsConnectionHandleValid(RfcConnectionHandle, out isValid, out errorInfo))
            .Returns(RfcResultCode.RFC_OK);

        connection.Connect();

        // Act
        bool isConnected = connection.IsValid;

        // Assert
        isConnected.ShouldBeFalse();
    }

    [Fact]
    public void CreateFunction_ShouldCreateFunction()
    {
        // Arrange
        var connection = new SapConnection(_interopMock.Object, new SapConnectionParameters());
        RfcErrorInfo errorInfo;
        _interopMock
            .Setup(x => x.OpenConnection(It.IsAny<RfcConnectionParameter[]>(), It.IsAny<uint>(), out errorInfo))
            .Returns(RfcConnectionHandle);
        _interopMock
            .Setup(x => x.GetFunctionDesc(RfcConnectionHandle, "FunctionA", out errorInfo))
            .Returns(FunctionDescriptionHandle);

        connection.Connect();

        // Act
        var function = connection.CreateFunction("FunctionA");

        // Assert
        function.ShouldNotBeNull();
        _interopMock.Verify(x => x.CreateFunction(FunctionDescriptionHandle, out errorInfo), Times.Once);
    }

    [Fact]
    public void IsValid_ConnectionHandleValid_ShouldReturnTrue()
    {
        // Arrange
        var connection = new SapConnection(_interopMock.Object, new SapConnectionParameters());
        int isValidValue = 1;
        RfcErrorInfo errorInfo;
        _interopMock
            .Setup(x => x.OpenConnection(It.IsAny<RfcConnectionParameter[]>(), It.IsAny<uint>(), out errorInfo))
            .Returns(RfcConnectionHandle);
        _interopMock
            .Setup(x => x.IsConnectionHandleValid(RfcConnectionHandle, out isValidValue, out errorInfo))
            .Returns(RfcResultCode.RFC_OK);

        connection.Connect();

        // Arrange
        var isValid = connection.IsValid;

        // Assert
        isValid.ShouldBeTrue();
    }

    [Fact]
    public void IsValid_ConnectionHandleInvalid_ShouldReturnFalse()
    {
        // Arrange
        var connection = new SapConnection(_interopMock.Object, new SapConnectionParameters());
        int isValidValue = 0;
        RfcErrorInfo errorInfo;
        _interopMock
            .Setup(x => x.OpenConnection(It.IsAny<RfcConnectionParameter[]>(), It.IsAny<uint>(), out errorInfo))
            .Returns(RfcConnectionHandle);
        _interopMock
            .Setup(x => x.IsConnectionHandleValid(RfcConnectionHandle, out isValidValue, out errorInfo))
            .Returns(RfcResultCode.RFC_OK);

        connection.Connect();

        // Arrange
        var isValid = connection.IsValid;

        // Assert
        isValid.ShouldBeFalse();
    }

    [Fact]
    public void Ping_NotConnected_ShouldReturnFalse()
    {
        // Arrange
        var connection = new SapConnection(_interopMock.Object, new SapConnectionParameters());

        // Act
        var pingResult = connection.Ping();

        // Assert
        pingResult.ShouldBeFalse();
    }

    [Fact]
    public void Ping_Connected_SuccessfulPing_ShouldReturnTrue()
    {
        // Arrange
        RfcErrorInfo errorInfo;
        _interopMock
            .Setup(x => x.OpenConnection(It.IsAny<RfcConnectionParameter[]>(), It.IsAny<uint>(), out errorInfo))
            .Returns(RfcConnectionHandle);
        _interopMock
            .Setup(x => x.Ping(RfcConnectionHandle, out errorInfo))
            .Returns(RfcResultCode.RFC_OK);
        var connection = new SapConnection(_interopMock.Object, new SapConnectionParameters());
        connection.Connect();

        // Act
        var pingResult = connection.Ping();

        // Assert
        pingResult.ShouldBeTrue();
    }

    [Fact]
    public void Ping_Connected_PingTimeout_ShouldReturnFalse()
    {
        // Arrange
        RfcErrorInfo errorInfo;
        _interopMock
            .Setup(x => x.OpenConnection(It.IsAny<RfcConnectionParameter[]>(), It.IsAny<uint>(), out errorInfo))
            .Returns(RfcConnectionHandle);
        _interopMock
            .Setup(x => x.Ping(RfcConnectionHandle, out errorInfo))
            .Returns(RfcResultCode.RFC_TIMEOUT);
        var connection = new SapConnection(_interopMock.Object, new SapConnectionParameters());
        connection.Connect();

        // Act
        var pingResult = connection.Ping();

        // Assert
        pingResult.ShouldBeFalse();
    }

    [Fact]
    public void GetAttributes_GettingConnectionAttributesSucceeds_ShouldReturnConnectionAttributes()
    {
        // Arrange
        var rfcAttributes = Fixture.Create<RfcAttributes>();
        rfcAttributes.Reserved = null;
        RfcErrorInfo errorInfo;
        _interopMock
            .Setup(x => x.GetConnectionAttributes(It.IsAny<IntPtr>(), out rfcAttributes, out errorInfo))
            .Returns(RfcResultCode.RFC_OK);
        var connection = new SapConnection(_interopMock.Object, new SapConnectionParameters());

        // Act
        var attributes = connection.GetAttributes();

        // Assert
        attributes.ShouldNotBeNull();
        attributes.ShouldBeEquivalentToObject(rfcAttributes);
    }

    [Fact]
    public void GetAttributes_GettingTheAttributesFails_ShouldThrowException()
    {
        // Arrange
        var rfcAttributes = Fixture.Create<RfcAttributes>();
        RfcErrorInfo errorInfo;
        _interopMock
            .Setup(x => x.GetConnectionAttributes(It.IsAny<IntPtr>(), out rfcAttributes, out errorInfo))
            .Returns(RfcResultCode.RFC_CLOSED);
        var connection = new SapConnection(_interopMock.Object, new SapConnectionParameters());

        // Act
        Action action = () => connection.GetAttributes();

        // Assert
        action.ShouldThrow<SapException>()
            .Message.ShouldBe("SAP RFC Error: RFC_CLOSED");
    }
}
