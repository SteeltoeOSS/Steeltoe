﻿// Copyright 2017 the original author or authors.
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
using Oracle.ManagedDataAccess.Client;
using Steeltoe.CloudFoundry.Connector.Relational;
using Steeltoe.Common.HealthChecks;
using System;
using System.Data;
using Xunit;

namespace Steeltoe.CloudFoundry.ConnectorAutofac.Test
{
    public class OracleContainerBuilderExtensionsTest
    {
        [Fact]
        public void RegisterOracleConnection_Requires_Builder()
        {
            // arrange
            IConfiguration config = new ConfigurationBuilder().Build();

            // act & assert
            Assert.Throws<ArgumentNullException>(() => OracleContainerBuilderExtensions.RegisterOracleConnection(null, config));
        }

        [Fact]
        public void RegisterOracleConnection_Requires_Config()
        {
            // arrange
            ContainerBuilder cb = new ContainerBuilder();

            // act & assert
            Assert.Throws<ArgumentNullException>(() => OracleContainerBuilderExtensions.RegisterOracleConnection(cb, null));
        }

        [Fact]
        public void RegisterOracleConnection_AddsToContainer()
        {
            // arrange
            ContainerBuilder container = new ContainerBuilder();
            IConfiguration config = new ConfigurationBuilder().Build();

            // act
            _ = OracleContainerBuilderExtensions.RegisterOracleConnection(container, config);
            var services = container.Build();
            var dbConn = services.Resolve<IDbConnection>();

            // assert
            Assert.NotNull(dbConn);
            Assert.Equal(typeof(OracleConnection).FullName, dbConn.GetType().FullName);
        }

        [Fact]
        public void RegisterOracleConnection_AddsHealthContributorToContainer()
        {
            // arrange
            ContainerBuilder container = new ContainerBuilder();
            IConfiguration config = new ConfigurationBuilder().Build();

            // act
            _ = OracleContainerBuilderExtensions.RegisterOracleConnection(container, config);
            var services = container.Build();
            var healthContributor = services.Resolve<IHealthContributor>();

            // assert
            Assert.NotNull(healthContributor);
            Assert.IsType<RelationalHealthContributor>(healthContributor);
        }
    }
}
