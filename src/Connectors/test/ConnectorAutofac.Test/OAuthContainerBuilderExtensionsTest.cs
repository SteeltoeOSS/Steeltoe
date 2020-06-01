// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.CloudFoundry.Connector;
using Steeltoe.CloudFoundry.Connector.OAuth;
using System;
using Xunit;

namespace Steeltoe.CloudFoundry.ConnectorAutofac.Test
{
    public class OAuthContainerBuilderExtensionsTest
    {
        [Fact]
        public void RegisterOAuthServiceOptions_Requires_Builder()
        {
            // arrange
            IConfiguration config = new ConfigurationBuilder().Build();

            // act & assert
            Assert.Throws<ArgumentNullException>(() => OAuthContainerBuilderExtensions.RegisterOAuthServiceOptions(null, config));
        }

        [Fact]
        public void RegisterOAuthServiceOptions_Requires_Config()
        {
            // arrange
            ContainerBuilder cb = new ContainerBuilder();

            // act & assert
            Assert.Throws<ArgumentNullException>(() => OAuthContainerBuilderExtensions.RegisterOAuthServiceOptions(cb, null));
        }

        [Fact]
        public void RegisterOAuthServiceOptions_AddsToContainer()
        {
            // arrange
            ContainerBuilder container = new ContainerBuilder();
            IConfiguration config = new ConfigurationBuilder().Build();

            // act
            var regBuilder = OAuthContainerBuilderExtensions.RegisterOAuthServiceOptions(container, config);
            var services = container.Build();
            var options = services.Resolve<IOptions<OAuthServiceOptions>>();

            // assert
            Assert.NotNull(options);
            Assert.IsType<ConnectorIOptions<OAuthServiceOptions>>(options);
        }
    }
}
