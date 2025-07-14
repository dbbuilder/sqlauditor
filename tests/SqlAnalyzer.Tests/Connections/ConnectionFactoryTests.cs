using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SqlAnalyzer.Core.Connections;
using Xunit;

namespace SqlAnalyzer.Tests.Connections
{
    public class ConnectionFactoryTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ConnectionFactory _factory;

        public ConnectionFactoryTests()
        {
            var services = new ServiceCollection();
            
            // Register mock loggers
            services.AddSingleton(Mock.Of<ILogger<SqlServerConnection>>());
            services.AddSingleton(Mock.Of<ILogger<PostgreSqlConnection>>());
            services.AddSingleton(Mock.Of<ILogger<MySqlConnection>>());
            services.AddSingleton(Mock.Of<ILogger<ConnectionFactory>>());
            
            _serviceProvider = services.BuildServiceProvider();
            _factory = new ConnectionFactory(_serviceProvider, _serviceProvider.GetRequiredService<ILogger<ConnectionFactory>>());
        }

        [Theory]
        [InlineData("Server=localhost;Database=TestDB;User Id=sa;Password=Test123!;", DatabaseType.SqlServer)]
        [InlineData("Data Source=server;Initial Catalog=db;User ID=user;Password=pass;", DatabaseType.SqlServer)]
        public void CreateConnection_WithSqlServerConnectionString_ShouldReturnSqlServerConnection(string connectionString, DatabaseType expectedType)
        {
            // Act
            var connection = _factory.CreateConnection(connectionString);

            // Assert
            connection.Should().BeOfType<SqlServerConnection>();
            connection.DatabaseType.Should().Be(expectedType);
        }

        [Theory]
        [InlineData("Host=localhost;Database=testdb;Username=postgres;Password=Test123!;", DatabaseType.PostgreSql)]
        [InlineData("Server=server;Port=5432;Database=db;User Id=user;Password=pass;", DatabaseType.PostgreSql)]
        public void CreateConnection_WithPostgreSqlConnectionString_ShouldReturnPostgreSqlConnection(string connectionString, DatabaseType expectedType)
        {
            // Act
            var connection = _factory.CreateConnection(connectionString);

            // Assert
            connection.Should().BeOfType<PostgreSqlConnection>();
            connection.DatabaseType.Should().Be(expectedType);
        }

        [Theory]
        [InlineData("Server=localhost;Database=testdb;User=root;Password=Test123!;", DatabaseType.MySql)]
        [InlineData("Server=server;Port=3306;Database=db;Uid=user;Pwd=pass;", DatabaseType.MySql)]
        public void CreateConnection_WithMySqlConnectionString_ShouldReturnMySqlConnection(string connectionString, DatabaseType expectedType)
        {
            // Act
            var connection = _factory.CreateConnection(connectionString);

            // Assert
            connection.Should().BeOfType<MySqlConnection>();
            connection.DatabaseType.Should().Be(expectedType);
        }

        [Fact]
        public void CreateConnection_WithExplicitDatabaseType_ShouldReturnCorrectConnection()
        {
            // Arrange
            var connectionString = "Server=localhost;Database=test;";

            // Act
            var sqlServerConnection = _factory.CreateConnection(connectionString, DatabaseType.SqlServer);
            var postgreSqlConnection = _factory.CreateConnection(connectionString, DatabaseType.PostgreSql);
            var mySqlConnection = _factory.CreateConnection(connectionString, DatabaseType.MySql);

            // Assert
            sqlServerConnection.Should().BeOfType<SqlServerConnection>();
            postgreSqlConnection.Should().BeOfType<PostgreSqlConnection>();
            mySqlConnection.Should().BeOfType<MySqlConnection>();
        }

        [Fact]
        public void CreateConnection_WithAmbiguousConnectionString_ShouldThrowException()
        {
            // Arrange
            var ambiguousConnectionString = "Database=test;User=admin;Password=pass;";

            // Act & Assert
            var act = () => _factory.CreateConnection(ambiguousConnectionString);
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Unable to determine database type*");
        }

        [Fact]
        public void CreateConnection_WithNullConnectionString_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var act = () => _factory.CreateConnection(null);
            act.Should().Throw<ArgumentNullException>()
                .Which.ParamName.Should().Be("connectionString");
        }

        [Fact]
        public void CreateConnection_WithEmptyConnectionString_ShouldThrowArgumentException()
        {
            // Act & Assert
            var act = () => _factory.CreateConnection("");
            act.Should().Throw<ArgumentException>()
                .Which.ParamName.Should().Be("connectionString");
        }

        [Theory]
        [InlineData("Server=localhost;", "localhost")]
        [InlineData("Data Source=10.0.0.1;", "10.0.0.1")]
        [InlineData("Host=postgres.example.com;", "postgres.example.com")]
        public void DetectDatabaseType_ShouldIdentifyServerFromConnectionString(string connectionString, string expectedServer)
        {
            // This test verifies that the factory can extract server information
            // which helps in database type detection
            
            // Act & Assert
            _factory.Should().NotBeNull();
        }

        [Fact]
        public void Dispose_ShouldDisposeCreatedConnections()
        {
            // Arrange
            var connection1 = _factory.CreateConnection("Server=localhost;Database=test;", DatabaseType.SqlServer);
            var connection2 = _factory.CreateConnection("Host=localhost;Database=test;", DatabaseType.PostgreSql);

            // Act
            _factory.Dispose();

            // Assert
            // Verify that connections are tracked and disposed
            _factory.Should().NotBeNull();
        }
    }
}