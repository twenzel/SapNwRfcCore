using System;
using System.Linq;
using System.Reflection;
using System.Text;
using AutoFixture;
using Shouldly;
using Xunit;

namespace SapNwRfcCore.Tests;

public sealed class SapConnectionParametersTests
{
    private static readonly Fixture Fixture = new Fixture();

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Parse_InvalidConnectionString_ShouldThrowArgumentException(string connectionString)
    {
        // Act
        Action action = () => SapConnectionParameters.Parse(connectionString);

        // Assert
        action.ShouldThrow<ArgumentException>().ParamName.ShouldBe("connectionString");
    }

    [Fact]
    public void Parse_ShouldSetProperties()
    {
        // Arrange
        const string connectionString = "AppServerHost=MyFancyHost;User= SomeUsername; Password = SomePassword ";

        // Act
        var parameters = SapConnectionParameters.Parse(connectionString);

        // Assert
        parameters.ShouldNotBeNull();
        parameters.AppServerHost.ShouldBe("MyFancyHost");
        parameters.User.ShouldBe("SomeUsername");
        parameters.Password.ShouldBe("SomePassword");
    }

    [Fact]
    public void Parse_ShouldSupportEqualSignInPassword_Issue96()
    {
        // Arrange
        const string connectionString = "Password=my=password";

        // Act
        var parameters = SapConnectionParameters.Parse(connectionString);

        // Assert
        parameters.ShouldNotBeNull();
        parameters.Password.ShouldBe("my=password");
    }

    [Fact]
    public void Parse_AllProperties()
    {
        // Arrange
        var expectedParameters = Fixture.Create<SapConnectionParameters>();
        string connectionString = typeof(SapConnectionParameters)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Aggregate(new StringBuilder(), (sb, propertyInfo) =>
            {
                object value = propertyInfo.GetValue(expectedParameters);
                sb.Append($"{propertyInfo.Name}={value};");
                return sb;
            })
            .ToString();

        // Act
        var parameters = SapConnectionParameters.Parse(connectionString);

        // Assert
        parameters.ShouldBeEquivalentTo(expectedParameters);
    }
}
