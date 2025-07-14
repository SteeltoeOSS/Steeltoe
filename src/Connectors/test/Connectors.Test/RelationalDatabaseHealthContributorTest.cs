// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using MySqlConnector;
using Npgsql;
using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Connectors.Test;

public sealed class RelationalDatabaseHealthContributorTest
{
    [Fact]
    public async Task PostgreSQL_Not_Connected_Returns_Down_Status()
    {
        DbConnection connection = new NpgsqlConnection("Server=localhost;Port=9999;Timeout=1");

        using var healthContributor = new RelationalDatabaseHealthContributor(connection, "localhost", NullLogger<RelationalDatabaseHealthContributor>.Instance)
        {
            ServiceName = "Example"
        };

        HealthCheckResult? result = await healthContributor.CheckHealthAsync(TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Down);
        result.Description.Should().Be("PostgreSQL health check failed");
        result.Details.Should().Contain("host", "localhost");
        result.Details.Should().Contain("service", "Example");
        result.Details.Should().ContainKey("error").WhoseValue.As<string>().Should().StartWith("NpgsqlException: ");
    }

    [Fact(Skip = "Integration test - Requires local PostgreSQL server")]
    public async Task PostgreSQL_Integration_Is_Connected_Returns_Up_Status()
    {
        DbConnection connection = new NpgsqlConnection("Server=localhost;User ID=steeltoe;Password=steeltoe");

        using var healthContributor = new RelationalDatabaseHealthContributor(connection, "localhost", NullLogger<RelationalDatabaseHealthContributor>.Instance)
        {
            ServiceName = "Example"
        };

        HealthCheckResult? result = await healthContributor.CheckHealthAsync(TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Up);
        result.Details.Should().Contain("host", "localhost");
        result.Details.Should().Contain("service", "Example");
        result.Details.Should().NotContainKey("error");
    }

    [Fact]
    public async Task MySQL_Not_Connected_Returns_Down_Status()
    {
        DbConnection connection = new MySqlConnection("Server=localhost;Port=9999;Connect Timeout=1");

        using var healthContributor = new RelationalDatabaseHealthContributor(connection, "localhost", NullLogger<RelationalDatabaseHealthContributor>.Instance)
        {
            ServiceName = "Example"
        };

        HealthCheckResult? result = await healthContributor.CheckHealthAsync(TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Down);
        result.Description.Should().Be("MySQL health check failed");
        result.Details.Should().Contain("host", "localhost");
        result.Details.Should().Contain("service", "Example");
        result.Details.Should().ContainKey("error").WhoseValue.As<string>().Should().StartWith("MySqlException: ");
    }

    [Fact(Skip = "Integration test - Requires local MySQL server")]
    public async Task MySQL_Integration_Is_Connected_Returns_Up_Status()
    {
        DbConnection connection = new MySqlConnection("Server=localhost;User ID=steeltoe;Password=steeltoe");

        using var healthContributor = new RelationalDatabaseHealthContributor(connection, "localhost", NullLogger<RelationalDatabaseHealthContributor>.Instance)
        {
            ServiceName = "Example"
        };

        HealthCheckResult? result = await healthContributor.CheckHealthAsync(TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Up);
        result.Details.Should().Contain("host", "localhost");
        result.Details.Should().Contain("service", "Example");
        result.Details.Should().NotContainKey("error");
    }

    [Fact]
    public async Task SQLServer_Not_Connected_Returns_Down_Status()
    {
        // Using a known host/port, so running this test doesn't take 15 seconds (the Connect Timeout only kicks in *after* establishing the socket connection).
        DbConnection connection = new SqlConnection("Server=tcp:www.microsoft.com,80;Connect Timeout=1;Connect Retry Count=0");

        using var healthContributor = new RelationalDatabaseHealthContributor(connection, "localhost", NullLogger<RelationalDatabaseHealthContributor>.Instance)
        {
            ServiceName = "Example"
        };

        HealthCheckResult? result = await healthContributor.CheckHealthAsync(TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Down);
        result.Description.Should().Be("SQL Server health check failed");
        result.Details.Should().Contain("host", "localhost");
        result.Details.Should().Contain("service", "Example");
        result.Details.Should().ContainKey("error").WhoseValue.As<string>().Should().StartWith("SqlException: Connection Timeout Expired.");
    }

    [Fact(Skip = "Integration test - Requires local SQL Server instance")]
    public async Task SQLServer_Integration_Is_Connected_Returns_Up_Status()
    {
        DbConnection connection = new SqlConnection(@"Server=(localdb)\mssqllocaldb");

        using var healthContributor = new RelationalDatabaseHealthContributor(connection, "localhost", NullLogger<RelationalDatabaseHealthContributor>.Instance)
        {
            ServiceName = "Example"
        };

        HealthCheckResult? result = await healthContributor.CheckHealthAsync(TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Up);
        result.Details.Should().Contain("host", "localhost");
        result.Details.Should().Contain("service", "Example");
        result.Details.Should().NotContainKey("error");
    }

    [Fact]
    public async Task Is_Connected_Returns_Up_Status()
    {
        var commandMock = new Mock<DbCommand>();
        commandMock.Setup(command => command.ExecuteScalarAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult<object?>(1));

        var connectionMock = new Mock<DbConnection>();
        connectionMock.Setup(connection => connection.Open());
        connectionMock.Protected().Setup<DbCommand>("CreateDbCommand").Returns(() => commandMock.Object);

        using var healthContributor =
            new RelationalDatabaseHealthContributor(connectionMock.Object, "localhost", NullLogger<RelationalDatabaseHealthContributor>.Instance)
            {
                ServiceName = "Example"
            };

        HealthCheckResult? result = await healthContributor.CheckHealthAsync(TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Up);
        result.Details.Should().Contain("host", "localhost");
        result.Details.Should().Contain("service", "Example");
        result.Details.Should().NotContainKey("error");
    }

    [Fact]
    public async Task Canceled_Throws()
    {
        var connectionMock = new Mock<DbConnection>();

        connectionMock.Setup(connection => connection.OpenAsync(It.IsAny<CancellationToken>())).Returns((CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return null!;
        });

        using var healthContributor =
            new RelationalDatabaseHealthContributor(connectionMock.Object, "localhost", NullLogger<RelationalDatabaseHealthContributor>.Instance);

        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        // ReSharper disable AccessToDisposedClosure
        Func<Task> action = async () => await healthContributor.CheckHealthAsync(source.Token);
        // ReSharper restore AccessToDisposedClosure

        await action.Should().ThrowExactlyAsync<OperationCanceledException>();
    }
}
