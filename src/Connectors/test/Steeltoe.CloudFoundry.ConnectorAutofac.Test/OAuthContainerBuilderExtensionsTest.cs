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
