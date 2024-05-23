// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using MySqlConnector;
using Npgsql;
using Steeltoe.Common.HealthChecks;
using Xunit;

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

        HealthCheckResult? status = await healthContributor.CheckHealthAsync(CancellationToken.None);

        status.Should().NotBeNull();
        status!.Status.Should().Be(HealthStatus.Down);
        status.Description.Should().Be("PostgreSQL health check failed");
        status.Details.Should().Contain("host", "localhost");
        status.Details.Should().Contain("service", "Example");
        status.Details.Should().ContainKey("error").WhoseValue.As<string>().Should().Match("NpgsqlException: Failed to connect to *:9999");
        status.Details.Should().Contain("status", "DOWN");
    }

    [Fact(Skip = "Integration test - Requires local PostgreSQL server")]
    public async Task PostgreSQL_Integration_Is_Connected_Returns_Up_Status()
    {
        DbConnection connection = new NpgsqlConnection("Server=localhost;User ID=steeltoe;Password=steeltoe");

        using var healthContributor = new RelationalDatabaseHealthContributor(connection, "localhost", NullLogger<RelationalDatabaseHealthContributor>.Instance)
        {
            ServiceName = "Example"
        };

        HealthCheckResult? status = await healthContributor.CheckHealthAsync(CancellationToken.None);

        status.Should().NotBeNull();
        status!.Status.Should().Be(HealthStatus.Up);
        status.Details.Should().Contain("host", "localhost");
        status.Details.Should().Contain("service", "Example");
        status.Details.Should().NotContainKey("error");
        status.Details.Should().Contain("status", "UP");
    }

    [Fact]
    public async Task MySQL_Not_Connected_Returns_Down_Status()
    {
        DbConnection connection = new MySqlConnection("Server=localhost;Port=9999;Connect Timeout=1");

        using var healthContributor = new RelationalDatabaseHealthContributor(connection, "localhost", NullLogger<RelationalDatabaseHealthContributor>.Instance)
        {
            ServiceName = "Example"
        };

        HealthCheckResult? status = await healthContributor.CheckHealthAsync(CancellationToken.None);

        status.Should().NotBeNull();
        status!.Status.Should().Be(HealthStatus.Down);
        status.Description.Should().Be("MySQL health check failed");
        status.Details.Should().Contain("host", "localhost");
        status.Details.Should().Contain("service", "Example");
        status.Details.Should().ContainKey("error").WhoseValue.As<string>().Should().StartWith("MySqlException: ");
        status.Details.Should().Contain("status", "DOWN");
    }

    [Fact(Skip = "Integration test - Requires local MySQL server")]
    public async Task MySQL_Integration_Is_Connected_Returns_Up_Status()
    {
        DbConnection connection = new MySqlConnection("Server=localhost;User ID=steeltoe;Password=steeltoe");

        using var healthContributor = new RelationalDatabaseHealthContributor(connection, "localhost", NullLogger<RelationalDatabaseHealthContributor>.Instance)
        {
            ServiceName = "Example"
        };

        HealthCheckResult? status = await healthContributor.CheckHealthAsync(CancellationToken.None);

        status.Should().NotBeNull();
        status!.Status.Should().Be(HealthStatus.Up);
        status.Details.Should().Contain("host", "localhost");
        status.Details.Should().Contain("service", "Example");
        status.Details.Should().NotContainKey("error");
        status.Details.Should().Contain("status", "UP");
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

        HealthCheckResult? status = await healthContributor.CheckHealthAsync(CancellationToken.None);

        status.Should().NotBeNull();
        status!.Status.Should().Be(HealthStatus.Down);
        status.Description.Should().Be("SQL Server health check failed");
        status.Details.Should().Contain("host", "localhost");
        status.Details.Should().Contain("service", "Example");
        status.Details.Should().ContainKey("error").WhoseValue.As<string>().Should().StartWith("SqlException: Connection Timeout Expired.");
        status.Details.Should().Contain("status", "DOWN");
    }

    [Fact(Skip = "Integration test - Requires local SQL Server instance")]
    public async Task SQLServer_Integration_Is_Connected_Returns_Up_Status()
    {
        DbConnection connection = new SqlConnection("Server=(localdb)\\mssqllocaldb");

        using var healthContributor = new RelationalDatabaseHealthContributor(connection, "localhost", NullLogger<RelationalDatabaseHealthContributor>.Instance)
        {
            ServiceName = "Example"
        };

        HealthCheckResult? status = await healthContributor.CheckHealthAsync(CancellationToken.None);

        status.Should().NotBeNull();
        status!.Status.Should().Be(HealthStatus.Up);
        status.Details.Should().Contain("host", "localhost");
        status.Details.Should().Contain("service", "Example");
        status.Details.Should().NotContainKey("error");
        status.Details.Should().Contain("status", "UP");
    }

    [Fact]
    public async Task Is_Connected_Returns_Up_Status()
    {
        var commandMock = new Mock<DbCommand>();
        commandMock.Setup(command => command.ExecuteScalar()).Returns(1);

        var connectionMock = new Mock<DbConnection>();
        connectionMock.Setup(connection => connection.Open());
        connectionMock.Protected().Setup<DbCommand>("CreateDbCommand").Returns(() => commandMock.Object);

        using var healthContributor =
            new RelationalDatabaseHealthContributor(connectionMock.Object, "localhost", NullLogger<RelationalDatabaseHealthContributor>.Instance)
            {
                ServiceName = "Example"
            };

        HealthCheckResult? status = await healthContributor.CheckHealthAsync(CancellationToken.None);

        status.Should().NotBeNull();
        status!.Status.Should().Be(HealthStatus.Up);
        status.Details.Should().Contain("host", "localhost");
        status.Details.Should().Contain("service", "Example");
        status.Details.Should().NotContainKey("error");
        status.Details.Should().Contain("status", "UP");
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

        Func<Task> action = async () => await healthContributor.CheckHealthAsync(source.Token);

        await action.Should().ThrowExactlyAsync<OperationCanceledException>();
    }
}
