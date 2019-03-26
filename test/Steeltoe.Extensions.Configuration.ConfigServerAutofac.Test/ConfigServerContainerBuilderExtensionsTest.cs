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
using Steeltoe.Common.Options.Autofac;
using Steeltoe.Extensions.Configuration.ConfigServer;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServerAutofac.Test
{
    public class ConfigServerContainerBuilderExtensionsTest
    {
        [Fact]
        public void RegisterConfigServerClientOptions_ThrowsNulls()
        {
            ContainerBuilder container = null;
            Assert.Throws<ArgumentNullException>(() => container.RegisterConfigServerClientOptions(null));
            ContainerBuilder container2 = new ContainerBuilder();
            Assert.Throws<ArgumentNullException>(() => container2.RegisterConfigServerClientOptions(null));
        }

        [Fact]
        public void RegisterConfigServerClientOptions_Registers_AndBindsConfiguration()
        {
            ContainerBuilder container = new ContainerBuilder();
            var dict = new Dictionary<string, string>()
            {
                { "spring:cloud:config:uri", "http://foo.bar/foo" },
                { "spring:cloud:config:env", "env" },
                { "spring:cloud:config:label", "label" },
                { "spring:cloud:config:name", "name" },
                { "spring:cloud:config:username", "username" },
                { "spring:cloud:config:password", "password" }
            };

            IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
            container.RegisterOptions();

            container.RegisterConfigServerClientOptions(config);
            var built = container.Build();

            var service = built.Resolve<IOptions<ConfigServerClientSettingsOptions>>();
            Assert.NotNull(service);
            Assert.NotNull(service.Value);
            Assert.Equal("http://foo.bar/foo", service.Value.Uri);
            Assert.Equal("env", service.Value.Env);
            Assert.Equal("label", service.Value.Label);
            Assert.Equal("name", service.Value.Name);
            Assert.Equal("username", service.Value.Username);
            Assert.Equal("password", service.Value.Password);
        }
    }
}
