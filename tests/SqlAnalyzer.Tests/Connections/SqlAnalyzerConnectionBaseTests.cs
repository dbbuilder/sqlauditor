using System;
using System.Data;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SqlAnalyzer.Core.Connections;
using Xunit;

namespace SqlAnalyzer.Tests.Connections
{
    public class SqlAnalyzerConnectionBaseTests
    {
        private readonly Mock<ILogger<TestConnection>> _mockLogger;
        private readonly TestConnection _connection;

        public SqlAnalyzerConnectionBaseTests()
        {
            _mockLogger = new Mock<ILogger<TestConnection>>();
            _connection = new TestConnection("Server=localhost;Database=TestDB;", _mockLogger.Object);
        }

        [Fact]
        public void Constructor_ShouldSetConnectionString()
        {
            _connection.ConnectionString.Should().Be("Server=localhost;Database=TestDB;");
        }

        [Fact]
        public void Constructor_ShouldParseConnectionString()
        {
            _connection.ParseConnectionStringCalled.Should().BeTrue();
        }

        [Fact]
        public async Task OpenAsync_ShouldOpenConnection()
        {
            await _connection.OpenAsync();
            
            _connection.IsOpen.Should().BeTrue();
        }

        [Fact]
        public async Task CloseAsync_ShouldCloseConnection()
        {
            await _connection.OpenAsync();
            await _connection.CloseAsync();
            
            _connection.IsOpen.Should().BeFalse();
        }

        [Fact]
        public async Task BeginTransactionAsync_ShouldCreateTransaction()
        {
            await _connection.OpenAsync();
            
            var transaction = await _connection.BeginTransactionAsync();
            
            transaction.Should().NotBeNull();
        }

        [Fact]
        public async Task BeginTransactionAsync_WhenConnectionClosed_ShouldThrow()
        {
            var act = async () => await _connection.BeginTransactionAsync();
            
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public void Dispose_ShouldCloseConnection()
        {
            _connection.OpenAsync().Wait();
            
            _connection.Dispose();
            
            _connection.IsDisposed.Should().BeTrue();
            _connection.IsOpen.Should().BeFalse();
        }

        [Fact]
        public void CreateCommand_WithParameters_ShouldAddParameters()
        {
            var parameters = new System.Collections.Generic.Dictionary<string, object>
            {
                { "@param1", "value1" },
                { "@param2", 123 }
            };

            var command = _connection.TestCreateCommand("SELECT * FROM Test", CommandType.Text, parameters);
            
            command.Parameters.Count.Should().Be(2);
        }

        // Test implementation for abstract base class
        private class TestConnection : SqlAnalyzerConnectionBase
        {
            public bool ParseConnectionStringCalled { get; private set; }
            public bool IsOpen { get; private set; }
            public bool IsDisposed { get; private set; }

            public override DatabaseType DatabaseType => DatabaseType.SqlServer;

            public TestConnection(string connectionString, ILogger<TestConnection> logger) 
                : base(connectionString, logger)
            {
            }

            protected override void ParseConnectionString()
            {
                ParseConnectionStringCalled = true;
                DatabaseName = "TestDB";
                ServerName = "localhost";
            }

            protected override IDbConnection CreateConnection()
            {
                var mockConnection = new Mock<IDbConnection>();
                mockConnection.Setup(c => c.State).Returns(() => IsOpen ? ConnectionState.Open : ConnectionState.Closed);
                mockConnection.Setup(c => c.Open()).Callback(() => IsOpen = true);
                mockConnection.Setup(c => c.Close()).Callback(() => IsOpen = false);
                mockConnection.Setup(c => c.BeginTransaction()).Returns(Mock.Of<IDbTransaction>());
                mockConnection.Setup(c => c.BeginTransaction(It.IsAny<IsolationLevel>())).Returns(Mock.Of<IDbTransaction>());
                mockConnection.Setup(c => c.CreateCommand()).Returns(Mock.Of<IDbCommand>());
                mockConnection.Setup(c => c.Dispose()).Callback(() => { IsDisposed = true; IsOpen = false; });
                
                return mockConnection.Object;
            }

            public override Task<bool> TestConnectionAsync()
            {
                return Task.FromResult(true);
            }

            public override Task<DataTable> ExecuteQueryAsync(string query, System.Collections.Generic.Dictionary<string, object> parameters = null)
            {
                return Task.FromResult(new DataTable());
            }

            public override Task<DataTable> ExecuteStoredProcedureAsync(string procedureName, System.Collections.Generic.Dictionary<string, object> parameters = null)
            {
                return Task.FromResult(new DataTable());
            }

            public override Task<DataSet> ExecuteQueryMultipleAsync(string query, System.Collections.Generic.Dictionary<string, object> parameters = null)
            {
                return Task.FromResult(new DataSet());
            }

            public override Task<string> GetDatabaseVersionAsync()
            {
                return Task.FromResult("Test Version");
            }

            public override Task<decimal> GetDatabaseSizeAsync()
            {
                return Task.FromResult(100m);
            }

            public IDbCommand TestCreateCommand(string query, CommandType commandType, System.Collections.Generic.Dictionary<string, object> parameters)
            {
                return CreateCommandWithRetry(query, commandType, parameters);
            }

            public new async Task OpenAsync()
            {
                await base.OpenAsync();
                IsOpen = true;
            }

            public new async Task CloseAsync()
            {
                await base.CloseAsync();
                IsOpen = false;
            }
        }
    }
}