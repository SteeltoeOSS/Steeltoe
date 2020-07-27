// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
            var container2 = new ContainerBuilder();
            Assert.Throws<ArgumentNullException>(() => container2.RegisterConfigServerClientOptions(null));
        }

        [Fact]
        public void RegisterConfigServerClientOptions_Registers_AndBindsConfiguration()
        {
            var container = new ContainerBuilder();
            var dict = new Dictionary<string, string>()
            {
                { "spring:cloud:config:uri", "https://foo.bar/foo" },
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
            Assert.Equal("https://foo.bar/foo", service.Value.Uri);
            Assert.Equal("env", service.Value.Env);
            Assert.Equal("label", service.Value.Label);
            Assert.Equal("name", service.Value.Name);
            Assert.Equal("username", service.Value.Username);
            Assert.Equal("password", service.Value.Password);
        }
    }
}
