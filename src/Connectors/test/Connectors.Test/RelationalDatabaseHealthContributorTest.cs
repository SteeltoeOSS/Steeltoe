// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using MySqlConnector;
using Npgsql;
using NSubstitute;
using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Connectors.Test;

public sealed class RelationalDatabaseHealthContributorTest
{
    [Fact]
    public async Task PostgreSQL_Not_Connected_Returns_Down_Status()
    {
        var healthContributor = new RelationalDatabaseHealthContributor(() => new NpgsqlConnection("Server=localhost;Port=9999;Timeout=1"), "PostgreSQL",
            "localhost", NullLogger<RelationalDatabaseHealthContributor>.Instance)
        {
            ServiceName = "Example"
        };

        HealthCheckResult? result = await healthContributor.CheckHealthAsync(TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Down);
        result.Description.Should().Be("PostgreSQL health check failed");
        result.Details.Should().Contain("host", "localhost");
        result.Details.Should().Contain("service", "Example");

        string errorMessage = result.Details.Should().ContainKey("error").WhoseValue.As<string>();

        errorMessage.Should().Match(error =>
            error.StartsWith("NpgsqlException: Failed to connect", StringComparison.Ordinal) ||
            error.StartsWith("TimeoutException: ", StringComparison.Ordinal));
    }

    [Fact(Skip = "Integration test - Requires local PostgreSQL server")]
    public async Task PostgreSQL_Integration_Is_Connected_Returns_Up_Status()
    {
        var healthContributor = new RelationalDatabaseHealthContributor(() => new NpgsqlConnection("Server=localhost;User ID=steeltoe;Password=steeltoe"),
            "PostgreSQL", "localhost", NullLogger<RelationalDatabaseHealthContributor>.Instance)
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
        var healthContributor = new RelationalDatabaseHealthContributor(() => new MySqlConnection("Server=localhost;Port=9999;Connect Timeout=1"), "MySQL",
            "localhost", NullLogger<RelationalDatabaseHealthContributor>.Instance)
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
        var healthContributor = new RelationalDatabaseHealthContributor(() => new MySqlConnection("Server=localhost;User ID=steeltoe;Password=steeltoe"),
            "MySQL", "localhost", NullLogger<RelationalDatabaseHealthContributor>.Instance)
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
        var healthContributor = new RelationalDatabaseHealthContributor(
            () => new SqlConnection("Server=tcp:www.microsoft.com,80;Connect Timeout=1;Connect Retry Count=0"), "SQL Server", "localhost",
            NullLogger<RelationalDatabaseHealthContributor>.Instance)
        {
            ServiceName = "Example"
        };

        HealthCheckResult? result = await healthContributor.CheckHealthAsync(TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Down);
        result.Description.Should().Be("SQL Server health check failed");
        result.Details.Should().Contain("host", "localhost");
        result.Details.Should().Contain("service", "Example");

        result.Details.Should().ContainKey("error").WhoseValue.As<string>().Should().Match(exception =>
            exception.StartsWith("SqlException: Connection Timeout Expired.", StringComparison.Ordinal) ||
            exception.StartsWith("SqlException: A network-related or instance-specific error", StringComparison.Ordinal));
    }

    [Fact(Skip = "Integration test - Requires local SQL Server instance")]
    public async Task SQLServer_Integration_Is_Connected_Returns_Up_Status()
    {
        var healthContributor = new RelationalDatabaseHealthContributor(() => new SqlConnection(@"Server=(localdb)\mssqllocaldb"), "SQL Server", "localhost",
            NullLogger<RelationalDatabaseHealthContributor>.Instance)
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
        var command = Substitute.For<DbCommand>();
        command.ExecuteScalarAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<object?>(1));

        await using var connection = new FakeDbConnection(command);

        var healthContributor =
            // ReSharper disable once AccessToDisposedClosure
            new RelationalDatabaseHealthContributor(() => connection, "SQL Server", "localhost", NullLogger<RelationalDatabaseHealthContributor>.Instance)
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
        var connection = Substitute.For<DbConnection>();

        connection.OpenAsync(Arg.Any<CancellationToken>()).Returns(info =>
        {
            info.Arg<CancellationToken>().ThrowIfCancellationRequested();
            return Task.CompletedTask;
        });

        var healthContributor =
            new RelationalDatabaseHealthContributor(() => connection, "SQL Server", "localhost", NullLogger<RelationalDatabaseHealthContributor>.Instance);

        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        // ReSharper disable AccessToDisposedClosure
        Func<Task> action = async () => await healthContributor.CheckHealthAsync(source.Token);
        // ReSharper restore AccessToDisposedClosure

        await action.Should().ThrowExactlyAsync<OperationCanceledException>();
    }

    private sealed class FakeDbConnection(DbCommand command) : DbConnection
    {
        [AllowNull]
        public override string ConnectionString
        {
            get;
            set => field = value ?? string.Empty;
        } = string.Empty;

        public override string ServerVersion => string.Empty;
        public override string DataSource => string.Empty;
        public override string Database => string.Empty;
        public override ConnectionState State => ConnectionState.Open;

        public override void ChangeDatabase(string databaseName)
        {
            throw new NotSupportedException();
        }

        public override void Open()
        {
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            throw new NotSupportedException();
        }

        protected override DbCommand CreateDbCommand()
        {
            return command;
        }

        public override void Close()
        {
        }
    }
}
