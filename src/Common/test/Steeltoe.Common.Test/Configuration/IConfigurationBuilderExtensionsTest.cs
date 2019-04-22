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
