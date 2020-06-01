// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Steeltoe.CloudFoundry.Connector.MongoDb;
using Steeltoe.Common.HealthChecks;
using System;
using Xunit;

namespace Steeltoe.CloudFoundry.ConnectorAutofac.Test
{
    public class MongoDbContainerBuilderExtensionsTest
    {
        [Fact]
        public void RegisterMongoDb_Requires_Builder()
        {
            // arrange
            IConfiguration config = new ConfigurationBuilder().Build();

            // act & assert
            Assert.Throws<ArgumentNullException>(() => MongoDbContainerBuilderExtensions.RegisterMongoDbConnection(null, config));
        }

        [Fact]
        public void RegisterMongoDb_Requires_Config()
        {
            // arrange
            ContainerBuilder cb = new ContainerBuilder();

            // act & assert
            Assert.Throws<ArgumentNullException>(() => MongoDbContainerBuilderExtensions.RegisterMongoDbConnection(cb, null));
        }

        [Fact]
        public void RegisterMongoDb_AddsTypesToContainer()
        {
            // arrange
            ContainerBuilder container = new ContainerBuilder();
            IConfiguration config = new ConfigurationBuilder().Build();

            // act
            var regBuilder = container.RegisterMongoDbConnection(config);
            var services = container.Build();
            var mongoClient = services.Resolve<MongoClient>();
            var iMongoClient = services.Resolve<IMongoClient>();
            var mongoUrl = services.Resolve<MongoUrl>();

            // assert
            Assert.NotNull(mongoClient);
            Assert.NotNull(iMongoClient);
            Assert.NotNull(mongoUrl);
            Assert.Equal(typeof(MongoClient).FullName, mongoClient.GetType().FullName);
            Assert.Equal(typeof(MongoClient).FullName, iMongoClient.GetType().FullName);
            Assert.Equal(typeof(MongoUrl).FullName, mongoUrl.GetType().FullName);
        }

        [Fact]
        public void RegisterMongoClient_AddsHealthContributorToContainer()
        {
            // arrange
            ContainerBuilder container = new ContainerBuilder();
            IConfiguration config = new ConfigurationBuilder().Build();

            // act
            container.RegisterMongoDbConnection(config);
            var services = container.Build();
            var healthContributor = services.Resolve<IHealthContributor>();

            // assert
            Assert.NotNull(healthContributor);
            Assert.IsType<MongoDbHealthContributor>(healthContributor);
        }
    }
}
