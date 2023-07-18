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

public class DbMigrationsEndpointTests : BaseTest
{
    private readonly ITestOutputHelper _output;

    public DbMigrationsEndpointTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void DbMigrationsEndpoint_EfMigrationsReflectionTargets_NotNull()
    {
        DbMigrationsEndpointHandler.GetDatabase.Should().NotBeNull();
        DbMigrationsEndpointHandler.GetMigrationsMethod.Should().NotBeNull();
        DbMigrationsEndpointHandler.DbContextType.Should().NotBeNull();
        DbMigrationsEndpointHandler.GetAppliedMigrationsMethod.Should().NotBeNull();
        DbMigrationsEndpointHandler.GetPendingMigrationsMethod.Should().NotBeNull();
    }

    [Fact]
    public void DbMigrationsHelper_GetDeclaredMigrations_InvokesRealExtensionMethodAndThrows()
    {
        var sut = new DbMigrationsEndpointHandler.DbMigrationsEndpointHelper();

        sut.Invoking(s => s.GetMigrations(new MockDbContext())).Should().Throw<TargetInvocationException>().WithInnerException<InvalidOperationException>()
            .WithMessage("*No database provider has been configured for this DbContext*");
    }

    [Fact]
    public async Task Invoke_WhenExistingDatabase_ReturnsExpected()
    {
        using var tc = new TestContext(_output);
        var helper = Substitute.For<DbMigrationsEndpointHandler.DbMigrationsEndpointHelper>();
        helper.ScanRootAssembly.Returns(typeof(MockDbContext).Assembly);

        helper.GetPendingMigrations(Arg.Any<object>()).Returns(new[]
        {
            "pending"
        });

        helper.GetAppliedMigrations(Arg.Any<object>()).Returns(new[]
        {
            "applied"
        });

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddScoped(_ => new MockDbContext());
            services.AddScoped(_ => helper);
            services.AddDbMigrationsActuatorServices();
        };

        var sut = tc.GetService<IDbMigrationsEndpointHandler>();
        Dictionary<string, DbMigrationsDescriptor> result = await sut.InvokeAsync(null, CancellationToken.None);

        const string contextName = nameof(MockDbContext);
        result.Should().ContainKey(contextName);
        result[contextName].AppliedMigrations.Should().Equal("applied");
        result[contextName].PendingMigrations.Should().Equal("pending");
    }

    [Fact]
    public async Task Invoke_NonExistingDatabase_ReturnsExpected()
    {
        using var tc = new TestContext(_output);
        var helper = Substitute.For<DbMigrationsEndpointHandler.DbMigrationsEndpointHelper>();
        helper.ScanRootAssembly.Returns(typeof(MockDbContext).Assembly);
        helper.GetPendingMigrations(Arg.Any<object>()).Throws(new SomeDbException("database doesn't exist"));

        helper.GetMigrations(Arg.Any<object>()).Returns(new[]
        {
            "migration"
        });

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddScoped(_ => new MockDbContext());
            services.AddScoped(_ => helper);
            services.AddDbMigrationsActuatorServices();
        };

        var sut = tc.GetService<IDbMigrationsEndpointHandler>();
        Dictionary<string, DbMigrationsDescriptor> result = await sut.InvokeAsync(null, CancellationToken.None);

        const string contextName = nameof(MockDbContext);
        result.Should().ContainKey(contextName);
        result[contextName].AppliedMigrations.Should().BeEmpty();
        result[contextName].PendingMigrations.Should().Equal("migration");
    }

    [Fact]
    public async Task Invoke_NonContainerRegistered_ReturnsExpected()
    {
        using var tc = new TestContext(_output);
        var helper = Substitute.For<DbMigrationsEndpointHandler.DbMigrationsEndpointHelper>();
        helper.ScanRootAssembly.Returns(typeof(MockDbContext).Assembly);

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddScoped(_ => helper);
            services.AddDbMigrationsActuatorServices();
        };

        var sut = tc.GetService<IDbMigrationsEndpointHandler>();
        Dictionary<string, DbMigrationsDescriptor> result = await sut.InvokeAsync(null, CancellationToken.None);

        result.Should().BeEmpty();
    }
}
