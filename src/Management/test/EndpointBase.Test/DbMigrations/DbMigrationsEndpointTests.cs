// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Reflection;
using Xunit;

namespace Steeltoe.Management.Endpoint.DbMigrations.Test
{
    public class DbMigrationsEndpointTests : BaseTest
    {
        [Fact]
        public void DbMigrationsEndpoint_EfMigrationsReflectionTargets_NotNull()
        {
            DbMigrationsEndpoint._getDatabase.Should().NotBeNull();
            DbMigrationsEndpoint._getMigrations.Should().NotBeNull();
            DbMigrationsEndpoint._dbContextType.Should().NotBeNull();
            DbMigrationsEndpoint._getAppliedMigrations.Should().NotBeNull();
            DbMigrationsEndpoint._getPendingMigrations.Should().NotBeNull();
        }

        [Fact]
        public void DbMigrationsHelper_GetDeclaredMigrations_InvokesRealExtensionMethodAndThrows()
        {
            var sut = new DbMigrationsEndpoint.DbMigrationsEndpointHelper();

            sut.Invoking(s => s.GetMigrations(new MockDbContext()))
                .Should()
                .Throw<TargetInvocationException>()
                .WithInnerException<InvalidOperationException>()
                .WithMessage("*No database provider has been configured for this DbContext*");
        }

        [Fact]
        public void Invoke_WhenExistingDatabase_ReturnsExpected()
        {
            var context = new MockDbContext();
            var container = Substitute.For<IServiceProvider>();
            container.GetService(typeof(MockDbContext)).Returns(context);
            var helper = Substitute.For<DbMigrationsEndpoint.DbMigrationsEndpointHelper>();
            helper.ScanRootAssembly.Returns(typeof(MockDbContext).Assembly);
            helper.GetPendingMigrations(Arg.Any<object>()).Returns(new[] { "pending" });
            helper.GetAppliedMigrations(Arg.Any<object>()).Returns(new[] { "applied" });
            var options = new DbMigrationsEndpointOptions();
            var logger = GetLogger<DbMigrationsEndpoint>();

            var sut = new DbMigrationsEndpoint(options, container, helper, logger);
            var result = sut.Invoke();

            var contextName = nameof(MockDbContext);
            result.Should().ContainKey(contextName);
            result[contextName].AppliedMigrations.Should().Equal("applied");
            result[contextName].PendingMigrations.Should().Equal("pending");
        }

        [Fact]
        public void Invoke_NonExistingDatabase_ReturnsExpected()
        {
            var context = new MockDbContext();
            var container = Substitute.For<IServiceProvider>();
            container.GetService(typeof(MockDbContext)).Returns(context);
            var helper = Substitute.For<DbMigrationsEndpoint.DbMigrationsEndpointHelper>();
            helper.ScanRootAssembly.Returns(typeof(MockDbContext).Assembly);
            helper.GetPendingMigrations(Arg.Any<object>()).Throws(new SomeDbException("database doesn't exist"));
            helper.GetMigrations(Arg.Any<object>()).Returns(new[] { "migration" });
            var options = new DbMigrationsEndpointOptions();
            var logger = GetLogger<DbMigrationsEndpoint>();

            var sut = new DbMigrationsEndpoint(options, container, helper, logger);
            var result = sut.Invoke();

            var contextName = nameof(MockDbContext);
            result.Should().ContainKey(contextName);
            result[contextName].AppliedMigrations.Should().BeEmpty();
            result[contextName].PendingMigrations.Should().Equal("migration");
        }

        [Fact]
        public void Invoke_NonContainerRegistered_ReturnsExpected()
        {
            var container = Substitute.For<IServiceProvider>();
            var helper = Substitute.For<DbMigrationsEndpoint.DbMigrationsEndpointHelper>();
            helper.ScanRootAssembly.Returns(typeof(MockDbContext).Assembly);
            var options = new DbMigrationsEndpointOptions();
            var logger = GetLogger<DbMigrationsEndpoint>();

            var sut = new DbMigrationsEndpoint(options, container, helper, logger);
            var result = sut.Invoke();

            result.Should().BeEmpty();
        }
    }
}