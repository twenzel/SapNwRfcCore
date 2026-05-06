using System;
using FluentAssertions;
using SapNwRfcCore.Exceptions;
using Xunit;

namespace SapNwRfcCore.Tests.Exceptions;

public sealed class SapLibraryNotFoundExceptionTests
{
    [Fact]
    public void Constructor_ShouldSetInnerException()
    {
        // Arrange
        var innerException = new DllNotFoundException();

        // Act
        var exception = new SapLibraryNotFoundException(innerException);

        // Assert
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void Constructor_ShouldSetMessage()
    {
        // Act
        var exception = new SapLibraryNotFoundException(innerException: default);

        // Assert
        exception.Message.Should().MatchRegex("The SAP RFC libraries were not found in the output folder or in a folder contained in the systems .* environment variable");
    }
}
