// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Extensions.Configuration.ConfigServer;
using System;
using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServerAutofac.Test
{
    public class ConfigServerConfigurationBuilderExtensionsAutofacTest
    {
        [Fact]
        public void AddConfigServer_ThrowsNulls()
        {
            ConfigurationBuilder builder = null;
            Assert.Throws<ArgumentNullException>(() => builder.AddConfigServer(null));
        }

        [Fact]
        public void AddConfigServer_WithEnvAndName()
        {
            var builder = new ConfigurationBuilder();
            builder.AddConfigServer("foo", "bar");

            ConfigServerConfigurationSource provider = null;
            foreach (var source in builder.Sources)
            {
                provider = source as ConfigServerConfigurationSource;
                if (provider != null)
                {
                    break;
                }
            }

            Assert.NotNull(provider);
            var settings = provider.DefaultSettings;
            Assert.NotNull(settings);
            Assert.Equal("foo", settings.Environment);
            Assert.Equal("bar", settings.Name);
        }
    }
}
