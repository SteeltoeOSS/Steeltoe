// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

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
    public void PostgreSQL_Not_Connected_Returns_Down_Status()
    {
        DbConnection connection = new NpgsqlConnection("Server=localhost;Port=9999;Timeout=1");

        using var healthContributor = new RelationalDatabaseHealthContributor(connection, "localhost", NullLogger<RelationalDatabaseHealthContributor>.Instance)
        {
            ServiceName = "Example"
        };

        HealthCheckResult status = healthContributor.Health();

        status.Status.Should().Be(HealthStatus.Down);
        status.Description.Should().Be("PostgreSQL health check failed");
        status.Details.Should().Contain("host", "localhost");
        status.Details.Should().Contain("service", "Example");
        status.Details.Should().Contain("error", "NpgsqlException: Failed to connect to [::1]:9999");
        status.Details.Should().Contain("status", "DOWN");
    }

    [Fact(Skip = "Integration test - Requires local PostgreSQL server")]
    public void PostgreSQL_Integration_Is_Connected_Returns_Up_Status()
    {
        DbConnection connection = new NpgsqlConnection("Server=localhost;User ID=steeltoe;Password=steeltoe");

        using var healthContributor = new RelationalDatabaseHealthContributor(connection, "localhost", NullLogger<RelationalDatabaseHealthContributor>.Instance)
        {
            ServiceName = "Example"
        };

        HealthCheckResult status = healthContributor.Health();

        status.Status.Should().Be(HealthStatus.Up);
        status.Details.Should().Contain("host", "localhost");
        status.Details.Should().Contain("service", "Example");
        status.Details.Should().NotContainKey("error");
        status.Details.Should().Contain("status", "UP");
    }

    [Fact]
    public void MySQL_Not_Connected_Returns_Down_Status()
    {
        DbConnection connection = new MySqlConnection("Server=localhost;Port=9999;Connect Timeout=1");

        using var healthContributor = new RelationalDatabaseHealthContributor(connection, "localhost", NullLogger<RelationalDatabaseHealthContributor>.Instance)
        {
            ServiceName = "Example"
        };

        HealthCheckResult status = healthContributor.Health();

        status.Status.Should().Be(HealthStatus.Down);
        status.Description.Should().Be("MySQL health check failed");
        status.Details.Should().Contain("host", "localhost");
        status.Details.Should().Contain("service", "Example");
        status.Details.Should().Contain("error", "MySqlException: Connect Timeout expired.");
        status.Details.Should().Contain("status", "DOWN");
    }

    [Fact(Skip = "Integration test - Requires local MySQL server")]
    public void MySQL_Integration_Is_Connected_Returns_Up_Status()
    {
        DbConnection connection = new MySqlConnection("Server=localhost;User ID=steeltoe;Password=steeltoe");

        using var healthContributor = new RelationalDatabaseHealthContributor(connection, "localhost", NullLogger<RelationalDatabaseHealthContributor>.Instance)
        {
            ServiceName = "Example"
        };

        HealthCheckResult status = healthContributor.Health();

        status.Status.Should().Be(HealthStatus.Up);
        status.Details.Should().Contain("host", "localhost");
        status.Details.Should().Contain("service", "Example");
        status.Details.Should().NotContainKey("error");
        status.Details.Should().Contain("status", "UP");
    }

    [Fact]
    public void SQLServer_Not_Connected_Returns_Down_Status()
    {
        // Using a known host/port, so running this test doesn't take 15 seconds (the Connect Timeout only kicks in *after* establishing the socket connection).
        DbConnection connection = new SqlConnection("Server=tcp:www.microsoft.com,80;Connect Timeout=1;Connect Retry Count=0");

        using var healthContributor = new RelationalDatabaseHealthContributor(connection, "localhost", NullLogger<RelationalDatabaseHealthContributor>.Instance)
        {
            ServiceName = "Example"
        };

        HealthCheckResult status = healthContributor.Health();

        status.Status.Should().Be(HealthStatus.Down);
        status.Description.Should().Be("SQL Server health check failed");
        status.Details.Should().Contain("host", "localhost");
        status.Details.Should().Contain("service", "Example");
        status.Details.Should().ContainKey("error").WhoseValue.As<string>().Should().StartWith("SqlException: Connection Timeout Expired.");
        status.Details.Should().Contain("status", "DOWN");
    }

    [Fact(Skip = "Integration test - Requires local SQL Server instance")]
    public void SQLServer_Integration_Is_Connected_Returns_Up_Status()
    {
        DbConnection connection = new SqlConnection("Server=(localdb)\\mssqllocaldb");

        using var healthContributor = new RelationalDatabaseHealthContributor(connection, "localhost", NullLogger<RelationalDatabaseHealthContributor>.Instance)
        {
            ServiceName = "Example"
        };

        HealthCheckResult status = healthContributor.Health();

        status.Status.Should().Be(HealthStatus.Up);
        status.Details.Should().Contain("host", "localhost");
        status.Details.Should().Contain("service", "Example");
        status.Details.Should().NotContainKey("error");
        status.Details.Should().Contain("status", "UP");
    }

    [Fact]
    public void Is_Connected_Returns_Up_Status()
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

        HealthCheckResult status = healthContributor.Health();

        status.Status.Should().Be(HealthStatus.Up);
        status.Details.Should().Contain("host", "localhost");
        status.Details.Should().Contain("service", "Example");
        status.Details.Should().NotContainKey("error");
        status.Details.Should().Contain("status", "UP");
    }
}
