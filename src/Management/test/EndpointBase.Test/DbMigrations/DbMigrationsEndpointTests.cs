// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Steeltoe.Management.EndpointBase.DbMigrations;
using System;
using System.Reflection;
using Steeltoe.Management.Endpoint.Test;
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