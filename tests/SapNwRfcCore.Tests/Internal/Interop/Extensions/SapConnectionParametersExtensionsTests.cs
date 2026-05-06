using System.Linq;
using SapNwRfcCore.Internal.Interop;
using Shouldly;
using Xunit;

namespace SapNwRfcCore.Tests.Internal.Interop.Extensions;

public sealed class SapConnectionParametersExtensionsTests
{
    [Fact]
    public void ToInterop_NoValueSet_ShouldReturnEmptyArray()
    {
        // Arrange
        var parameters = new SapConnectionParameters();

        // Act
        RfcConnectionParameter[] interopParameters = parameters.ToInterop();

        // Assert
        interopParameters.ShouldBeEmpty();
    }

    [Fact]
    public void ToInterop_ShouldMapNonNullValues()
    {
        // Arrange
        var parameters = new SapConnectionParameters
        {
            Name = "SomeName",
            Language = "EN",
        };

        // Act
        RfcConnectionParameter[] interopParameters = parameters.ToInterop();

        // Assert
        interopParameters.ShouldHaveCount(2);
        interopParameters.First().ShouldBeEquivalentToObject(new { Name = "NAME", Value = "SomeName" });
        interopParameters.Last().ShouldBeEquivalentToObject(new { Name = "LANG", Value = "EN" });
    }

    [Fact]
    public void ToInterop_ShouldUseNameFromAttribute()
    {
        // Arrange
        var parameters = new SapConnectionParameters
        {
            RepositoryPassword = "SomeRepoPassword",
        };

        // Act
        RfcConnectionParameter[] interopParameters = parameters.ToInterop();

        // Assert
        interopParameters.First().Name.ShouldBe("REPOSITORY_PASSWD");
    }
}
