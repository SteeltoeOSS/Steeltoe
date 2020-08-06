// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
        public void Constructors_InitializesProperties()
        {
            var memSource = new MemoryConfigurationSource();
            var sources = new List<IConfigurationSource>() { memSource };
            var factory = new LoggerFactory();

            var source = new PlaceholderResolverSource(sources, factory);
            Assert.Equal(factory, source._loggerFactory);
            Assert.NotNull(source._sources);
            Assert.Single(source._sources);
            Assert.NotSame(sources, source._sources);
            Assert.Contains(memSource, source._sources);
        }

        [Fact]
        public void Build_ReturnsProvider()
        {
            // Arrange
            var memSource = new MemoryConfigurationSource();
            IList<IConfigurationSource> sources = new List<IConfigurationSource>() { memSource };

            // Act and Assert
            var source = new PlaceholderResolverSource(sources, null);
            var provider = source.Build(new ConfigurationBuilder());
            Assert.IsType<PlaceholderResolverProvider>(provider);
        }
    }
}
