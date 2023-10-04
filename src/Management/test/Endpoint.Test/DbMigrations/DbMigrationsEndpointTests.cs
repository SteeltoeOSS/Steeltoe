// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Steeltoe.Management.Endpoint.DbMigrations;
using Steeltoe.Management.Endpoint.Test.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.DbMigrations;

public sealed class DbMigrationsEndpointTests : BaseTest
{
    private readonly ITestOutputHelper _output;

    public DbMigrationsEndpointTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void DbMigrationsEndpoint_EfMigrationsReflectionTargets_NotNull()
    {
        DbMigrationsEndpointHandler.GetDatabaseMethod.Should().NotBeNull();
        DbMigrationsEndpointHandler.GetMigrationsMethod.Should().NotBeNull();
        DbMigrationsEndpointHandler.DbContextType.Should().NotBeNull();
        DbMigrationsEndpointHandler.GetAppliedMigrationsMethod.Should().NotBeNull();
        DbMigrationsEndpointHandler.GetPendingMigrationsMethod.Should().NotBeNull();
    }

    [Fact]
    public void DatabaseMigrationScanner_GetDeclaredMigrations_InvokesRealExtensionMethodAndThrows()
    {
        var sut = new DbMigrationsEndpointHandler.DatabaseMigrationScanner();

        sut.Invoking(scanner => scanner.GetMigrations(new MockDbContext())).Should().ThrowExactly<TargetInvocationException>()
            .WithInnerException<InvalidOperationException>().WithMessage("*No database provider has been configured for this DbContext*");
    }

    [Fact]
    public async Task Invoke_WhenExistingDatabase_ReturnsExpected()
    {
        using var testContext = new TestContext(_output);
        var scanner = Substitute.For<DbMigrationsEndpointHandler.DatabaseMigrationScanner>();
        scanner.ScanRootAssembly.Returns(typeof(MockDbContext).Assembly);

        scanner.GetPendingMigrations(Arg.Any<object>()).Returns(new[]
        {
            "pending"
        });

        scanner.GetAppliedMigrations(Arg.Any<object>()).Returns(new[]
        {
            "applied"
        });

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddScoped(_ => new MockDbContext());
            services.AddScoped(_ => scanner);
            services.AddDbMigrationsActuatorServices();
        };

        var sut = testContext.GetRequiredService<IDbMigrationsEndpointHandler>();
        Dictionary<string, DbMigrationsDescriptor> result = await sut.InvokeAsync(null, CancellationToken.None);

        const string contextName = nameof(MockDbContext);
        result.Should().ContainKey(contextName);
        result[contextName].AppliedMigrations.Should().Equal("applied");
        result[contextName].PendingMigrations.Should().Equal("pending");
    }

    [Fact]
    public async Task Invoke_NonExistingDatabase_ReturnsExpected()
    {
        using var testContext = new TestContext(_output);
        var scanner = Substitute.For<DbMigrationsEndpointHandler.DatabaseMigrationScanner>();
        scanner.ScanRootAssembly.Returns(typeof(MockDbContext).Assembly);
        scanner.GetPendingMigrations(Arg.Any<object>()).Throws(new SomeDbException("database doesn't exist"));

        scanner.GetMigrations(Arg.Any<object>()).Returns(new[]
        {
            "migration"
        });

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddScoped(_ => new MockDbContext());
            services.AddScoped(_ => scanner);
            services.AddDbMigrationsActuatorServices();
        };

        var sut = testContext.GetRequiredService<IDbMigrationsEndpointHandler>();
        Dictionary<string, DbMigrationsDescriptor> result = await sut.InvokeAsync(null, CancellationToken.None);

        const string contextName = nameof(MockDbContext);
        result.Should().ContainKey(contextName);
        result[contextName].AppliedMigrations.Should().BeEmpty();
        result[contextName].PendingMigrations.Should().Equal("migration");
    }

    [Fact]
    public async Task Invoke_NonContainerRegistered_ReturnsExpected()
    {
        using var testContext = new TestContext(_output);
        var scanner = Substitute.For<DbMigrationsEndpointHandler.DatabaseMigrationScanner>();
        scanner.ScanRootAssembly.Returns(typeof(MockDbContext).Assembly);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddScoped(_ => scanner);
            services.AddDbMigrationsActuatorServices();
        };

        var sut = testContext.GetRequiredService<IDbMigrationsEndpointHandler>();
        Dictionary<string, DbMigrationsDescriptor> result = await sut.InvokeAsync(null, CancellationToken.None);

        result.Should().BeEmpty();
    }
}
