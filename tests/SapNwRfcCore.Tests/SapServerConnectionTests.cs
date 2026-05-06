using System;
using AutoFixture;
using Moq;
using SapNwRfcCore.Exceptions;
using SapNwRfcCore.Internal.Interop;
using Shouldly;
using Xunit;

namespace SapNwRfcCore.Tests;

public sealed class SapServerConnectionTests
{
    private static readonly Fixture Fixture = new Fixture();
    private static readonly IntPtr RfcConnectionHandle = (IntPtr)12;
    private readonly Mock<RfcInterop> _interopMock = new Mock<RfcInterop>();

    public SapServerConnectionTests()
    {
        new SupportMutableValueTypesCustomization().Customize(Fixture);
    }

    [Fact]
    public void GetAttributes_InteropSucceeds_ShouldReturnAttributesFromInterop()
    {
        // Arrange
        var rfcAttributes = Fixture.Create<RfcAttributes>();
        RfcErrorInfo errorInfo;
        _interopMock
            .Setup(x => x.GetConnectionAttributes(RfcConnectionHandle, out rfcAttributes, out errorInfo))
            .Returns(RfcResultCode.RFC_OK);
        var serverConnection = new SapServerConnection(_interopMock.Object, RfcConnectionHandle);

        // Act
        var sapAttributes = serverConnection.GetAttributes();

        // Assert
        sapAttributes.ShouldBeEquivalentToObject(rfcAttributes);
    }

    [Fact]
    public void GetAttributes_InteropFails_ShouldThrowSapException()
    {
        // Arrange
        var rfcAttributes = Fixture.Create<RfcAttributes>();
        RfcErrorInfo errorInfo;
        _interopMock
            .Setup(x => x.GetConnectionAttributes(RfcConnectionHandle, out rfcAttributes, out errorInfo))
            .Returns(RfcResultCode.RFC_NOT_FOUND);
        var serverConnection = new SapServerConnection(_interopMock.Object, RfcConnectionHandle);

        // Act
        Action action = () => serverConnection.GetAttributes();

        // Assert
        action.ShouldThrow<SapException>();
    }
}
