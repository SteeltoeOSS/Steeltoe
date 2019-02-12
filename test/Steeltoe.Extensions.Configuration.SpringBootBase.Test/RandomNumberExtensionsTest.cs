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
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Steeltoe.Extensions.Configuration.RandomNumber.Test
{
    public class RandomNumberExtensionsTest
    {
        [Fact]
        public void AddRandomNumberGenerator_ThrowsIfConfigBuilderNull()
        {
            // Arrange
            IConfigurationBuilder configurationBuilder = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => RandomNumberExtensions.AddRandomNumberGenerator(configurationBuilder));
        }

        [Fact]
        public void AddRandomNumberGenerator_ThrowsIfPrefixNull()
        {
            // Arrange
            string prefix = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentException>(() => RandomNumberExtensions.AddRandomNumberGenerator(new ConfigurationBuilder(), prefix));
        }

        [Fact]
        public void AddRandomNumberGenerator_Ignores()
        {
            var builder = new ConfigurationBuilder()
                .AddRandomNumberGenerator()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "foo:bar", "value" }
                });
            var config = builder.Build();
            var value = config["foo:bar"];
            Assert.Equal("value", value);
        }

        [Fact]
        public void AddRandomNumberGenerator_String()
        {
            var builder = new ConfigurationBuilder()
                .AddRandomNumberGenerator()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "foo:bar", "value" }
                });
            var config = builder.Build();
            var value = config["random:string"];
            Assert.NotNull(value);
        }

        [Fact]
        public void AddRandomNumberGenerator_Uuid()
        {
            var builder = new ConfigurationBuilder()
                .AddRandomNumberGenerator()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "foo:bar", "value" }
                });
            var config = builder.Build();
            var value = config["random:uuid"];
            Assert.NotNull(value);
        }

        [Fact]
        public void AddRandomNumberGenerator_RandomInt()
        {
            var builder = new ConfigurationBuilder()
                .AddRandomNumberGenerator()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "foo:bar", "value" }
                });
            var config = builder.Build();
            var value = config["random:int"];
            Assert.NotNull(value);
        }

        [Fact]
        public void AddRandomNumberGenerator_RandomIntRange()
        {
            var builder = new ConfigurationBuilder()
                .AddRandomNumberGenerator()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "foo:bar", "value" }
                });
            var config = builder.Build();
            var value = config["random:int[4,10]"];
            Assert.NotNull(value);
            int val = int.Parse(value);
            Assert.InRange(val, 4, 10);
        }

        [Fact]
        public void AddRandomNumberGenerator_RandomIntMax()
        {
            var builder = new ConfigurationBuilder()
                .AddRandomNumberGenerator()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "foo:bar", "value" }
                });
            var config = builder.Build();
            var value = config["random:int(10)"];
            Assert.NotNull(value);
            int val = int.Parse(value);
            Assert.InRange(val, 0, 10);
        }

        [Fact]
        public void AddRandomNumberGenerator_RandomLong()
        {
            var builder = new ConfigurationBuilder()
                .AddRandomNumberGenerator()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "foo:bar", "value" }
                });
            var config = builder.Build();
            var value = config["random:long"];
            Assert.NotNull(value);
        }

        [Fact]
        public void AddRandomNumberGenerator_RandomLongRange()
        {
            var builder = new ConfigurationBuilder()
                .AddRandomNumberGenerator()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "foo:bar", "value" }
                });
            var config = builder.Build();
            var value = config["random:long[4,10]"];
            Assert.NotNull(value);
            int val = int.Parse(value);
            Assert.InRange(val, 4, 10);
        }

        [Fact]
        public void AddRandomNumberGenerator_RandomLongMax()
        {
            var builder = new ConfigurationBuilder()
                .AddRandomNumberGenerator()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "foo:bar", "value" }
                });
            var config = builder.Build();
            var value = config["random:long(10)"];
            Assert.NotNull(value);
            int val = int.Parse(value);
            Assert.InRange(val, 0, 10);
        }
    }
}
