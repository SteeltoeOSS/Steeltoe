// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddConfigServer("foo", "bar");

            ConfigServerConfigurationProvider provider = null;
            foreach (IConfigurationSource source in builder.Sources)
            {
                provider = source as ConfigServerConfigurationProvider;
                if (provider != null)
                {
                    break;
                }
            }

            Assert.NotNull(provider);
            var settings = provider.Settings;
            Assert.NotNull(settings);
            Assert.Equal("foo", settings.Environment);
            Assert.Equal("bar", settings.Name);
        }
    }
}
