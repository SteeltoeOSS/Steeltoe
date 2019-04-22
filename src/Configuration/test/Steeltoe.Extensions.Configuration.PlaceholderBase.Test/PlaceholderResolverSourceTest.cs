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

namespace Steeltoe.Extensions.Configuration.Placeholder.Test
{
    public class PlaceholderResolverSourceTest
    {
        [Fact]
        public void Constructor_ThrowsIfNulls()
        {
            // Arrange
            IList<IConfigurationSource> sources = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new PlaceholderResolverSource(sources));
        }

        [Fact]
        public void Constructors__InitializesProperties()
        {
            var memSource = new MemoryConfigurationSource();
            IList<IConfigurationSource> sources = new List<IConfigurationSource>() { memSource };
            ILoggerFactory factory = new LoggerFactory();

            var source = new PlaceholderResolverSource(sources, factory);
            Assert.Equal(factory, source._loggerFactory);
            Assert.NotNull(source._sources);
            Assert.Single(source._sources);
            Assert.NotSame(sources, source._sources);
            Assert.Contains(memSource, source._sources);
        }

        [Fact]
        public void Build__ReturnsProvider()
        {
            // Arrange
            var memSource = new MemoryConfigurationSource();
            IList<IConfigurationSource> sources = new List<IConfigurationSource>() { memSource };
            ILoggerFactory factory = new LoggerFactory();

            // Act and Assert
            var source = new PlaceholderResolverSource(sources, null);
            var provider = source.Build(new ConfigurationBuilder());
            Assert.IsType<PlaceholderResolverProvider>(provider);
        }
    }
}
