using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SapNwRfcCore.Exceptions;
using SapNwRfcCore.Pooling;
using Shouldly;
using Xunit;

namespace SapNwRfcCore.Tests.Pooling;

public sealed class SapConnectionPoolTests
{
    private static readonly SapConnectionParameters ConnectionParameters = new SapConnectionParameters();

    [Fact]
    public void Constructor_WithoutConnectionFactory_ShouldNotThrow()
    {
        // Act
        var action = () => new SapConnectionPool(ConnectionParameters, connectionFactory: null);

        // Assert
        action.ShouldNotThrow();
    }

    [Fact]
    public void GetConnection_ShouldOpenConnection()
    {
        // Arrange
        var connectionMock = new Mock<ISapConnection>();
        var pool = new SapConnectionPool(ConnectionParameters, connectionFactory: _ => connectionMock.Object);

        // Act
        var connection = pool.GetConnection();

        // Assert
        connection.ShouldBe(connectionMock.Object);
        connectionMock.Verify(x => x.Connect(), Times.Once);
    }

    [Fact]
    public void GetConnection_ShouldReturnDifferentConnectionsUpToPoolSize()
    {
        // Arrange
        var pool = new SapConnectionPool(
            ConnectionParameters,
            poolSize: 2,
            connectionFactory: _ => Mock.Of<ISapConnection>());

        // Act
        var connection1 = pool.GetConnection();
        var connection2 = pool.GetConnection();

        // Assert
        connection1.ShouldNotBeNull();
        connection2.ShouldNotBeNull();
        connection1.ShouldNotBe(connection2);
    }

    [Fact]
    public void GetConnection_AfterReturnConnection_ShouldReturnSameConnection()
    {
        // Arrange
        var pool = new SapConnectionPool(
            ConnectionParameters,
            poolSize: 3,
            connectionFactory: _ => Mock.Of<ISapConnection>());

        // Act
        var connection1 = pool.GetConnection();
        pool.ReturnConnection(connection1);
        var connection2 = pool.GetConnection();

        // Assert
        connection1.ShouldNotBeNull();
        connection2.ShouldBe(connection1);
    }

    [Fact]
    public void GetConnection_AfterForgetConnection_ShouldReturnDifferentConnection()
    {
        // Arrange
        var pool = new SapConnectionPool(
            ConnectionParameters,
            poolSize: 3,
            connectionFactory: _ => Mock.Of<ISapConnection>());

        // Act
        var connection1 = pool.GetConnection();
        pool.ForgetConnection(connection1);
        var connection2 = pool.GetConnection();

        // Assert
        connection1.ShouldNotBeNull();
        connection2.ShouldNotBeNull();
        connection2.ShouldNotBe(connection1);
    }

    [Fact]
    public void GetConnection_ConnectionFactoryReturnsNull_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var pool = new SapConnectionPool(
            ConnectionParameters,
            poolSize: 1,
            connectionFactory: _ => null);

        // Act
        Action action = () => pool.GetConnection();

        // Assert
        action.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void GetConnection_CalledTwice_ConnectionFactoryReturnsNullFirst_ShouldNotCausePoolStarvation()
    {
        // Arrange
        ISapConnection firstConnection = null;
        var secondConnection = Mock.Of<ISapConnection>();
        var connectionFactoryMock = new Mock<Func<SapConnectionParameters, ISapConnection>>();
        connectionFactoryMock
            .SetupSequence(x => x(It.IsAny<SapConnectionParameters>()))
            .Returns(firstConnection)
            .Returns(secondConnection);
        var pool = new SapConnectionPool(
            ConnectionParameters,
            poolSize: 1,
            connectionFactory: connectionFactoryMock.Object);

        // Act
        try
        { pool.GetConnection(); }
        catch { }
        Action action = () => pool.GetConnection();

        // Assert
        action.ExecutionTime().ShouldBeLessThan(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void GetConnection_FailsToConnect_ShouldLetThroughException()
    {
        // Arrange
        var failingConnectionMock = new Mock<ISapConnection>();
        failingConnectionMock.Setup(x => x.Connect()).Throws(new SapCommunicationFailedException(default));
        var pool = new SapConnectionPool(
            ConnectionParameters,
            poolSize: 1,
            connectionFactory: _ => failingConnectionMock.Object);

        // Act
        Action action = () => pool.GetConnection();

        // Assert
        action.ShouldThrow<SapCommunicationFailedException>();
    }

    [Fact]
    public void GetConnection_CallMultipleTypes_FailsToConnect_ShouldNotCausePoolStarvation()
    {
        // Arrange
        var failingConnectionMock = new Mock<ISapConnection>();
        failingConnectionMock.Setup(x => x.Connect()).Throws(new SapCommunicationFailedException(default));
        var pool = new SapConnectionPool(
            ConnectionParameters,
            poolSize: 3,
            connectionFactory: _ => failingConnectionMock.Object);

        // Act
        Action action = () =>
        {
            try
            { pool.GetConnection(); }
            catch { }
            try
            { pool.GetConnection(); }
            catch { }
            try
            { pool.GetConnection(); }
            catch { }
            try
            { pool.GetConnection(); }
            catch { }
            try
            { pool.GetConnection(); }
            catch { }
            try
            { pool.GetConnection(); }
            catch { }
        };

        // Assert
        action.ExecutionTime().ShouldBeLessThan(TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public void GetConnection_CalledTwice_ConnectTakesSomeTimeButFails_ShouldReleaseBlockingSecondGetConnectionCall()
    {
        // Arrange
        var firstConnectionMock = new Mock<ISapConnection>();
        firstConnectionMock.Setup(x => x.Connect())
            .Callback(() => Thread.Sleep(250))
            .Throws(new SapCommunicationFailedException(default));
        var secondConnection = Mock.Of<ISapConnection>();
        var connectionFactoryMock = new Mock<Func<SapConnectionParameters, ISapConnection>>();
        connectionFactoryMock
            .SetupSequence(x => x(It.IsAny<SapConnectionParameters>()))
            .Returns(firstConnectionMock.Object)
            .Returns(secondConnection);
        var pool = new SapConnectionPool(
            ConnectionParameters,
            poolSize: 1,
            connectionFactory: connectionFactoryMock.Object);

        // Act
        var taskTookConnection = new ManualResetEventSlim();
        Task.Run(() =>
        {
            pool.GetConnection();
            taskTookConnection.Set();
        });
        Action action = () => pool.GetConnection();
        taskTookConnection.Wait(TimeSpan.FromSeconds(2));

        // Assert
        action.ExecutionTime().ShouldBeLessThan(TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public void ReturnConnection_ExceedPoolSize_GetConnectionShouldBlockAndReturnPreviousConnection()
    {
        // Arrange
        var pool = new SapConnectionPool(
            ConnectionParameters,
            poolSize: 1,
            connectionFactory: _ => Mock.Of<ISapConnection>());

        var connection1 = pool.GetConnection();
        ISapConnection connection2 = null;

        // Act
        var taskStarted = new ManualResetEventSlim();
        Task.Run(() =>
        {
            taskStarted.Set();
            Thread.Sleep(150);
            pool.ReturnConnection(connection1);
        });
        Action action = () => connection2 = pool.GetConnection();
        taskStarted.Wait();

        // Assert
        var executionTime = action.ExecutionTime();
        executionTime.ShouldBeLessThan(TimeSpan.FromMilliseconds(500));
        executionTime.ShouldBeGreaterThan(TimeSpan.FromMilliseconds(100));
        connection2.ShouldNotBeNull();
        connection2.ShouldBe(connection1);
    }

    [Fact]
    public void ReturnConnection_ExceedPoolSize_GetConnectionShouldBlockAndReturnFirstReturnedConnection()
    {
        // Arrange
        var pool = new SapConnectionPool(
            ConnectionParameters,
            poolSize: 2,
            connectionFactory: _ => Mock.Of<ISapConnection>());

        var connection1 = pool.GetConnection();
        var connection2 = pool.GetConnection();
        ISapConnection connection3 = null;

        // Act
        var taskStarted = new ManualResetEventSlim();
        Task.Run(() =>
        {
            taskStarted.Set();
            Thread.Sleep(150);
            pool.ReturnConnection(connection1);
            Thread.Sleep(150);
            pool.ReturnConnection(connection2);
        });
        Action action = () => connection3 = pool.GetConnection();
        taskStarted.Wait();

        // Assert
        var executionTime = action.ExecutionTime();
        executionTime.ShouldBeLessThan(TimeSpan.FromMilliseconds(275));
        executionTime.ShouldBeGreaterThan(TimeSpan.FromMilliseconds(100));
        connection3.ShouldNotBeNull();
        connection3.ShouldBe(connection1);
    }

    [Fact]
    public void ForgetConnection_ExceedPoolSize_GetConnectionShouldBlockAndReturnNewConnection()
    {
        // Arrange
        var pool = new SapConnectionPool(
            ConnectionParameters,
            poolSize: 1,
            connectionFactory: _ => Mock.Of<ISapConnection>());

        var connection1 = pool.GetConnection();
        ISapConnection connection2 = null;

        // Act
        var taskStarted = new ManualResetEventSlim();
        Task.Run(() =>
        {
            taskStarted.Set();
            Thread.Sleep(150);
            pool.ForgetConnection(connection1);
        });
        Action action = () => connection2 = pool.GetConnection();
        taskStarted.Wait();

        // Assert
        var executionTime = action.ExecutionTime();
        executionTime.ShouldBeLessThan(TimeSpan.FromMilliseconds(500));
        executionTime.ShouldBeGreaterThan(TimeSpan.FromMilliseconds(100));
        connection2.ShouldNotBeNull();
        connection2.ShouldNotBe(connection1);
    }

    [Fact]
    public void ForgetConnection_ShouldDisposeConnection()
    {
        // Arrange
        var connectionMock = new Mock<ISapConnection>();
        var pool = new SapConnectionPool(ConnectionParameters, connectionFactory: _ => connectionMock.Object);
        var connection = pool.GetConnection();

        // Act
        pool.ForgetConnection(connection);

        // Assert
        connectionMock.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_ShouldDisposeIdleConnections()
    {
        // Arrange
        var connectionMock = new Mock<ISapConnection>();
        var pool = new SapConnectionPool(ConnectionParameters, connectionFactory: _ => connectionMock.Object);
        var connection1 = pool.GetConnection();
        var connection2 = pool.GetConnection();
        pool.ReturnConnection(connection1);
        pool.ReturnConnection(connection2);

        // Act
        pool.Dispose();

        // Assert
        connectionMock.Verify(x => x.Dispose(), Times.Exactly(2));
    }

    [Fact]
    public void Wait_ShouldDisposeIdleConnections()
    {
        // Arrange
        var connectionMock = new Mock<ISapConnection>();
        var pool = new SapConnectionPool(
            ConnectionParameters,
            connectionIdleTimeout: TimeSpan.FromMilliseconds(150),
            idleDetectionInterval: TimeSpan.FromMilliseconds(25),
            connectionFactory: _ => connectionMock.Object);
        pool.ReturnConnection(pool.GetConnection());

        // Assert
        connectionMock.Verify(x => x.Dispose(), Times.Never);

        // Act
        Thread.Sleep(200);

        // Assert
        connectionMock.Verify(x => x.Dispose(), Times.Once);
    }
}
