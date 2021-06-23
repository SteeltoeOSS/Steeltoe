// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.CommandLine;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Steeltoe.Extensions.Configuration.SpringBoot.Test
{
    public class SpringBootCmdSourceTest
    {
        [Fact]
        public void Constructors__InitializesDefaults()
        {
            var config = new ConfigurationBuilder()
                            .AddCommandLine(new string[] { })
                            .Build();

            var source = new SpringBootCmdSource(config);
            Assert.Equal(config, source._config);
        }

        [Fact]
        public void Build__ReturnsProvider()
        {
            // Arrange
            var config = new ConfigurationBuilder()
                            .AddCommandLine(new string[] { })
                            .Build();

            // Act and Assert
            var source = new SpringBootCmdSource(config);
            var provider = source.Build(new ConfigurationBuilder());
            Assert.IsType<SpringBootCmdProvider>(provider);
        }
    }
}
