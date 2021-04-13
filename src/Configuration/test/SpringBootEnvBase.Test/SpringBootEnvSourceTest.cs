// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Steeltoe.Extensions.Configuration.SpringBootEnv.Test
{
    public class SpringBootEnvSourceTest
    {
        [Fact]
        public void Constructors__InitializesDefaults()
        {
            ILoggerFactory factory = new LoggerFactory();

            var source = new SpringBootEnvSource(factory);
            Assert.Equal(factory, source._loggerFactory);
        }

        [Fact]
        public void Build__ReturnsProvider()
        {
            // Arrange
            ILoggerFactory factory = new LoggerFactory();

            // Act and Assert
            var source = new SpringBootEnvSource();
            var provider = source.Build(new ConfigurationBuilder());
            Assert.IsType<SpringBootEnvProvider>(provider);
        }
    }
}
