// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Common.Configuration.Test
{
    public class IConfigurationBuilderExtensionsTest
    {
        private Dictionary<string, string> configEntries = new Dictionary<string, string>()
                {
                    { "foo", "${bazoo}" },
                    { "bar", "${baz}" },
                    { "baz", "bar" }
                };

        [Fact]
        public void AddResolvedPlaceholders_ThrowsIfConfigBuilderNull()
        {
            // Arrange
            IConfigurationBuilder configurationBuilder = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => IConfigurationBuilderExtensions.AddResolvedPlaceholders(configurationBuilder));
            Assert.Contains(nameof(configurationBuilder), ex.Message);
        }

        [Fact]
        public void AddResolvedPlaceholders_AddsInMemorySourceToSourcesList()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();

            // Act and Assert
            configurationBuilder.AddResolvedPlaceholders();

            MemoryConfigurationSource typedSource = null;
            foreach (var source in configurationBuilder.Sources)
            {
                typedSource = source as MemoryConfigurationSource;
                if (typedSource != null)
                {
                    break;
                }
            }

            Assert.NotNull(typedSource);
        }

        [Fact]
        public void AddResolvedPlaceholders_OverridesResolvedValues()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(configEntries);

            // Act
            configurationBuilder.AddResolvedPlaceholders();
            var config = configurationBuilder.Build();

            // Assert
            Assert.Equal("bar", config["bar"]);
        }

        [Fact]
        public void AddResolvedPlaceholders_BlanksUnResolvedValues()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(configEntries);

            // Act
            configurationBuilder.AddResolvedPlaceholders();
            var config = configurationBuilder.Build();

            // Assert
            Assert.Equal(string.Empty, config["foo"]);
        }

        [Fact]
        public void AddResolvedPlaceholders_CanIgnoreUnResolvedValues()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(configEntries);

            // Act
            configurationBuilder.AddResolvedPlaceholders(false);
            var config = configurationBuilder.Build();

            // Assert
            Assert.Equal("${bazoo}", config["foo"]);
        }

        [Fact]
        public void AddResolvedPlaceholders_BlanksResolvedEmptyValues()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(configEntries);

            // Act
            configurationBuilder.AddResolvedPlaceholders();
            var config = configurationBuilder.Build();

            // Assert
            Assert.Equal(string.Empty, config["foo"]);
        }
    }
}
