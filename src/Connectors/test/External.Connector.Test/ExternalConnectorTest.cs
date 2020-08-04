// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector;
using Steeltoe.Connector.Services;
using System;
using System.Linq;
using Xunit;

namespace External.Connector.Test
{
    public class ExternalConnectorTest
    {
        [Fact]
        public void CustomCreatorIsRetrieved()
        {
            // arrange
            var config = new ConfigurationBuilder().Build();

            // act
            var creator = ServiceInfoCreatorFactory.GetServiceInfoCreator(config);

            // assert
            Assert.IsType<TestServiceInfoCreator>(creator);
            Assert.Single(creator.ServiceInfos);
        }

        [Fact]
        public void CustomCreatorCanBePresentAndDisabled()
        {
            // arrange
            var config = new ConfigurationBuilder().Build();
            Environment.SetEnvironmentVariable("TestServiceInfoCreator", "false");

            // act
            var creator = ServiceInfoCreatorFactory.GetServiceInfoCreator(config);

            // assert
            Assert.IsType<ServiceInfoCreator>(creator);
            Assert.Equal(13, creator.Factories.Count);
            Environment.SetEnvironmentVariable("TestServiceInfoCreator", null);
        }

        [Fact]
        public void CustomCreatorCanUseOwnServiceInfos()
        {
            // arrange
            var config = new ConfigurationBuilder().Build();

            // act
            var serviceInfos = config.GetServiceInfos<DB2ServiceInfo>();

            // assert
            Assert.Single(serviceInfos);
            var serviceInfo = serviceInfos.First();
            Assert.Equal("test", serviceInfo.Scheme);
            Assert.Equal("test", serviceInfo.Host);
            Assert.Equal("test", serviceInfo.Path);
        }
    }
}
