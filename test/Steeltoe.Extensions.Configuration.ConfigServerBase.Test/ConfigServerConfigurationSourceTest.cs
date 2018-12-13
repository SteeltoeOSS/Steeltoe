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
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServer.Test
{
    public class ConfigServerConfigurationSourceTest
    {
        [Fact]
        public void Constructors__ThrowsIfNulls()
        {
            // Arrange
            ConfigServerClientSettings settings = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new ConfigServerConfigurationSource((IConfiguration)null));
            ex = Assert.Throws<ArgumentNullException>(() => new ConfigServerConfigurationSource(settings, (IConfiguration)null, null));
            ex = Assert.Throws<ArgumentNullException>(() => new ConfigServerConfigurationSource((IList<IConfigurationSource>)null, null));
            ex = Assert.Throws<ArgumentNullException>(() => new ConfigServerConfigurationSource(settings, new List<IConfigurationSource>(), null));
        }

        [Fact]
        public void Constructors__InitializesProperties()
        {
            ConfigServerClientSettings settings = new ConfigServerClientSettings();
            var memSource = new MemoryConfigurationSource();
            IList<IConfigurationSource> sources = new List<IConfigurationSource>() { memSource };
            ILoggerFactory factory = new LoggerFactory();

            var source = new ConfigServerConfigurationSource(settings, sources, new Dictionary<string, object>() { { "foo", "bar" } }, factory);
            Assert.Equal(settings, source.DefaultSettings);
            Assert.Equal(factory, source.LogFactory);
            Assert.Null(source.Configuration);
            Assert.NotSame(sources, source._sources);
            Assert.Single(source._sources);
            Assert.Equal(memSource, source._sources[0]);
            Assert.Single(source._properties);
            Assert.Equal("bar", source._properties["foo"]);

            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
            source = new ConfigServerConfigurationSource(settings, config, factory);
            Assert.Equal(settings, source.DefaultSettings);
            Assert.Equal(factory, source.LogFactory);
            Assert.NotNull(source.Configuration);
            var root = source.Configuration as IConfigurationRoot;
            Assert.NotNull(root);
            Assert.Same(config, root);
        }

        [Fact]
        public void Build__ReturnsProvider()
        {
            // Arrange
            ConfigServerClientSettings settings = new ConfigServerClientSettings();
            var memSource = new MemoryConfigurationSource();
            IList<IConfigurationSource> sources = new List<IConfigurationSource>() { memSource };
            ILoggerFactory factory = new LoggerFactory();

            // Act and Assert
            var source = new ConfigServerConfigurationSource(settings, sources, null);
            var provider = source.Build(new ConfigurationBuilder());
            Assert.IsType<ConfigServerConfigurationProvider>(provider);
        }
    }
}
