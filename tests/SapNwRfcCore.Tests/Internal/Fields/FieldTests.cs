using System;
using SapNwRfcCore.Internal.Fields;
using SapNwRfcCore.Internal.Interop;
using Shouldly;
using Xunit;

namespace SapNwRfcCore.Tests.Internal.Fields;

public sealed class FieldTests
{
    [Fact]
    public void Equals_SameNameAndValue_ShouldReturnTrue()
    {
        // Arrange
        var fieldA = new TestField("abc", true);
        var fieldB = new TestField("abc", true);

        // Act
        var result = fieldA.Equals(fieldB);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_DifferentName_ShouldReturnFalse()
    {
        // Arrange
        var fieldA = new TestField("abc", true);
        var fieldB = new TestField("abcd", true);

        // Act
        var result = fieldA.Equals(fieldB);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_DifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var fieldA = new TestField("abc", true);
        var fieldB = new TestField("abc", false);

        // Act
        var result = fieldA.Equals(fieldB);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_DifferentObjectType_ShouldReturnFalse()
    {
        // Arrange
        var field = new TestField("abc", true);

        // Act
        var result = field.Equals(123);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void GetHashCode_SameNameAndValue_ShouldReturnSameHash()
    {
        // Arrange
        var fieldA = new TestField("abc", true);
        var fieldB = new TestField("abc", true);

        // Act
        var hashA = fieldA.GetHashCode();
        var hashB = fieldB.GetHashCode();

        // Assert
        hashA.ShouldBe(hashB);
    }

    [Fact]
    public void GetHashCode_DifferentName_ShouldReturnDifferentHash()
    {
        // Arrange
        var fieldA = new TestField("abc", true);
        var fieldB = new TestField("abcd", true);

        // Act
        var hashA = fieldA.GetHashCode();
        var hashB = fieldB.GetHashCode();

        // Assert
        hashA.ShouldNotBe(hashB);
    }

    [Fact]
    public void GetHashCode_DifferentValue_ShouldReturnDifferentHash()
    {
        // Arrange
        var fieldA = new TestField("abc", true);
        var fieldB = new TestField("abc", false);

        // Act
        var hashA = fieldA.GetHashCode();
        var hashB = fieldB.GetHashCode();

        // Assert
        hashA.ShouldNotBe(hashB);
    }

    private class TestField : Field<bool>
    {
        public TestField(string name, bool value)
            : base(name, value)
        {
        }

        public override void Apply(RfcInterop interop, IntPtr dataHandle)
        {
        }
    }
}
