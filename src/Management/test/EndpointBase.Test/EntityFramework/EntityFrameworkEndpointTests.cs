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
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Steeltoe.Management.EndpointBase.DbMigrations;
using System;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.EntityFramework
{
    public class EntityFrameworkEndpointTests : BaseTest
    {
        [Fact]
        public void Invoke_WhenExistingDatabase_ReturnsExpected()
        {
            var context = new MockDbContext();
            var container = Substitute.For<IServiceProvider>();
            container.GetService(typeof(MockDbContext)).Returns(context);
            var helper = Substitute.For<EntityFrameworkEndpoint.EntityFrameworkEndpointHelper>();
            helper.GetPendingMigrations(Arg.Any<DbContext>()).Returns(new[] { "pending" });
            helper.GetAppliedMigrations(Arg.Any<DbContext>()).Returns(new[] { "applied" });
            var options = new EntityFrameworkEndpointOptions();
            options.ContextTypes.Add(typeof(MockDbContext));
            var logger = GetLogger<EntityFrameworkEndpoint>();

            var sut = new EntityFrameworkEndpoint(options, container, helper, logger);
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
            var helper = Substitute.For<EntityFrameworkEndpoint.EntityFrameworkEndpointHelper>();
            helper.GetPendingMigrations(Arg.Any<DbContext>()).Throws(new SomeDbException("database doesn't exist"));
            helper.GetMigrations(Arg.Any<DbContext>()).Returns(new[] { "migration" });
            var options = new EntityFrameworkEndpointOptions();
            options.ContextTypes.Add(typeof(MockDbContext));
            var logger = GetLogger<EntityFrameworkEndpoint>();

            var sut = new EntityFrameworkEndpoint(options, container, helper, logger);
            var result = sut.Invoke();

            var contextName = nameof(MockDbContext);
            result.Should().ContainKey(contextName);
            result[contextName].AppliedMigrations.Should().BeEmpty();
            result[contextName].PendingMigrations.Should().Equal("migration");
        }

        [Fact]
        public void Invoke_WithAuthodiscoverContext_ReturnsExpected()
        {
            var container = Substitute.For<IServiceProvider>();
            container.GetService(Arg.Any<Type>()).Returns(x => Activator.CreateInstance((Type)x[0]));
            var helper = Substitute.For<EntityFrameworkEndpoint.EntityFrameworkEndpointHelper>();
            helper.GetPendingMigrations(Arg.Any<DbContext>()).Returns(new[] { "pending" });
            helper.GetAppliedMigrations(Arg.Any<DbContext>()).Returns(new[] { "applied" });
            helper.ScanRootAssembly.Returns(typeof(MockDbContext).Assembly); // need to override as in test context entryassembly is not application code
            var options = new EntityFrameworkEndpointOptions();
            options.AutoDiscoverContexts = true;
            var logger = GetLogger<EntityFrameworkEndpoint>();

            var sut = new EntityFrameworkEndpoint(options, container, helper, logger);
            var result = sut.Invoke();

            var contextName = nameof(MockDbContext);
            result.Should().ContainKey(contextName);
        }
    }
}