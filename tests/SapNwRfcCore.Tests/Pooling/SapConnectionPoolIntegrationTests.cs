using System;
using SapNwRfcCore.Exceptions;
using SapNwRfcCore.Pooling;
using Shouldly;
using Xunit;

namespace SapNwRfcCore.Tests.Pooling;

public sealed class SapConnectionPoolIntegrationTests
{
    private const string ConnectionString = "AppServerHost=localhost; SystemNumber=00; User=MY_SAP_USER; Password=SECRET; Client=100; Language=EN; PoolSize=5; Trace=8";

    [Fact(Skip = "SAP libs not available on CI/CD pipeline")]
    public void InvokeFunction_UnreachableHost_ShouldThrowSapCommunicationFailedException()
    {
        // Arrange
        var pool = new SapConnectionPool(ConnectionString);
        var connection = new SapPooledConnection(pool);

        // Act
        Action action = () => connection.InvokeFunction("TestFunc");

        // Assert
        action.ShouldThrow<SapCommunicationFailedException>();
    }

    [Fact(Skip = "SAP libs not available on CI/CD pipeline")]
    public void DisposeAfterFailedConnectionAttempt_InvalidSapConnection_ShouldNotThrow()
    {
        // Arrange
        var input = new TestInput();
        var pool = new SapConnectionPool(ConnectionString);
        var connection = new SapPooledConnection(pool);

        // Act
        try
        { connection.InvokeFunction<TestOutput>("TestFunc", input); }
        catch { }
        Action action = () => connection.Dispose();

        // Assert
        action.ShouldNotThrow();
    }

    public sealed class TestInput
    {
        public string Name { get; set; }
    }

    public sealed class TestOutput
    {
        public string Name { get; set; }
    }
}
