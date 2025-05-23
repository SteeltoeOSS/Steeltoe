// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Actuators.DbMigrations;

namespace Steeltoe.Management.Endpoint.Test.Actuators.DbMigrations;

public sealed class DbMigrationsEndpointTest(ITestOutputHelper testOutputHelper) : BaseTest
{
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    [Fact]
    public void DbMigrationsEndpoint_MigrationReflectionTargets_NotNull()
    {
        DatabaseMigrationScanner.GetDatabaseMethod.Should().NotBeNull();
        DatabaseMigrationScanner.GetMigrationsMethod.Should().NotBeNull();
        DatabaseMigrationScanner.DbContextType.Should().NotBeNull();
        DatabaseMigrationScanner.GetAppliedMigrationsMethod.Should().NotBeNull();
        DatabaseMigrationScanner.GetPendingMigrationsMethod.Should().NotBeNull();
    }

    [Fact]
    public void DatabaseMigrationScanner_GetDeclaredMigrations_InvokesRealExtensionMethodAndThrows()
    {
        var scanner = new DatabaseMigrationScanner();
        var dbContext = new MockDbContext();

        Action action = () => scanner.GetMigrations(dbContext);

        action.Should().ThrowExactly<TargetInvocationException>().WithInnerExceptionExactly<InvalidOperationException>()
            .WithMessage("*No database provider has been configured for this DbContext*");
    }

    [Fact]
    public async Task Invoke_WhenExistingDatabase_ReturnsExpected()
    {
        using var testContext = new SteeltoeTestContext(_testOutputHelper);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddScoped<MockDbContext>();
            services.AddSingleton<IDatabaseMigrationScanner, TestDatabaseMigrationScanner>();
            services.AddDbMigrationsActuator();
        };

        var sut = testContext.GetRequiredService<IDbMigrationsEndpointHandler>();
        Dictionary<string, DbMigrationsDescriptor> result = await sut.InvokeAsync(null, TestContext.Current.CancellationToken);

        const string contextName = nameof(MockDbContext);
        result.Should().ContainKey(contextName);
        result[contextName].AppliedMigrations.Should().Equal("applied");
        result[contextName].PendingMigrations.Should().Equal("pending");
    }

    [Fact]
    public async Task Invoke_NonExistingDatabase_ReturnsExpected()
    {
        using var testContext = new SteeltoeTestContext(_testOutputHelper);

        var migrationScanner = new TestDatabaseMigrationScanner
        {
            ThrowOnGetPendingMigrations = true
        };

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddScoped<MockDbContext>();
            services.AddSingleton<IDatabaseMigrationScanner>(migrationScanner);
            services.AddDbMigrationsActuator();
        };

        var handler = testContext.GetRequiredService<IDbMigrationsEndpointHandler>();
        Dictionary<string, DbMigrationsDescriptor> result = await handler.InvokeAsync(null, TestContext.Current.CancellationToken);

        const string contextName = nameof(MockDbContext);
        result.Should().ContainKey(contextName);
        result[contextName].AppliedMigrations.Should().BeEmpty();
        result[contextName].PendingMigrations.Should().Equal("migration");
    }

    [Fact]
    public async Task Invoke_NoDbContextRegistered_ReturnsExpected()
    {
        using var testContext = new SteeltoeTestContext(_testOutputHelper);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton<IDatabaseMigrationScanner, TestDatabaseMigrationScanner>();
            services.AddDbMigrationsActuator();
        };

        var handler = testContext.GetRequiredService<IDbMigrationsEndpointHandler>();
        Dictionary<string, DbMigrationsDescriptor> result = await handler.InvokeAsync(null, TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }
}
