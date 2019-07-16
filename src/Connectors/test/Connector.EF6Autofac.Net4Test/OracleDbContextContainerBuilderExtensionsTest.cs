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

using Autofac;
using Microsoft.Extensions.Configuration;
using System;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.EF6Autofac.Test
{
    public class OracleDbContextContainerBuilderExtensionsTest
    {
        [Fact]
        public void RegisterOracleDbContext_Requires_Builder()
        {
            // arrange
            IConfiguration config = new ConfigurationBuilder().Build();

            // act & assert
            Assert.Throws<ArgumentNullException>(() => OracleDbContextContainerBuilderExtensions.RegisterDbContext<GoodOracleDbContextcs>(null, config));
        }

        [Fact]
        public void RegisterOracleDbContext_Requires_Config()
        {
            // arrange
            var cb = new ContainerBuilder();

            // act & assert
            Assert.Throws<ArgumentNullException>(() => OracleDbContextContainerBuilderExtensions.RegisterDbContext<GoodOracleDbContextcs>(cb, null));
        }

        [Fact]
        public void RegisterOracleDbContext_AddsToContainer()
        {
            // arrange
            var container = new ContainerBuilder();
            IConfiguration config = new ConfigurationBuilder().Build();

            // act
            _ = OracleDbContextContainerBuilderExtensions.RegisterDbContext<GoodOracleDbContextcs>(container, config);
            var services = container.Build();
            var dbConn = services.Resolve<GoodOracleDbContextcs>();

            // assert
            Assert.NotNull(dbConn);
            Assert.IsType<GoodOracleDbContextcs>(dbConn);
        }
    }
}
