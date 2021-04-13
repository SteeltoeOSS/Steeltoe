// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Extensions.Configuration.SpringBoot.Test
{
    public class SpringBootConfigurationBuilderExtensionsTest
    {
        [Fact]
        public void AddSpringBootEnv_ThrowsIfConfigBuilderNull()
        {
            // Arrange
            IConfigurationBuilder configurationBuilder = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => SpringBootConfigurationBuilderExtensions.AddSpringBootEnv(configurationBuilder));
        }

        [Fact]
        public void AddSpringBootEnv_AddKeys()
        {
            Environment.SetEnvironmentVariable("SPRING_APPLICATION_JSON", "{\"foo.bar\":\"value\"}");

            var builder = new ConfigurationBuilder()
                .AddSpringBootEnv();
            var config = builder.Build();
            var value = config["foo:bar"];
            Assert.Equal("value", value);
        }
    }
}
