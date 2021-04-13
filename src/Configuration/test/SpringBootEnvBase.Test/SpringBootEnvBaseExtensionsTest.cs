// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Extensions.Configuration.SpringBootEnv.Test
{
    public class SpringBootEnvBaseExtensionsTest
    {
        [Fact]
        public void AddSpringBootEnvSource_ThrowsIfConfigBuilderNull()
        {
            // Arrange
            IConfigurationBuilder configurationBuilder = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => SpringBootEnvExtensions.AddSpringBootEnvSource(configurationBuilder));
        }

        [Fact]
        public void AddSpringBootEnvSource_Ignores()
        {
            var builder = new ConfigurationBuilder()
                .AddSpringBootEnvSource()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "foo:bar", "value" }
                });
            var config = builder.Build();
            var value = config["foo:bar"];
            Assert.Equal("value", value);
        }
    }
}
