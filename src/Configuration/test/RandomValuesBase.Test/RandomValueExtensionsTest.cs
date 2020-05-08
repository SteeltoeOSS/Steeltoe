// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
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

namespace Steeltoe.Extensions.Configuration.RandomValue.Test
{
    public class RandomValueExtensionsTest
    {
        [Fact]
        public void AddRandomValueSource_ThrowsIfConfigBuilderNull()
        {
            // Arrange
            IConfigurationBuilder configurationBuilder = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => RandomValueExtensions.AddRandomValueSource(configurationBuilder));
        }

        [Fact]
        public void AddRandomValueSource_ThrowsIfPrefixNull()
        {
            // Arrange
            string prefix = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentException>(() => RandomValueExtensions.AddRandomValueSource(new ConfigurationBuilder(), prefix));
        }

        [Fact]
        public void AddRandomValueSource_Ignores()
        {
            var builder = new ConfigurationBuilder()
                .AddRandomValueSource()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "foo:bar", "value" }
                });
            var config = builder.Build();
            var value = config["foo:bar"];
            Assert.Equal("value", value);
        }

        [Fact]
        public void AddRandomValueSource_String()
        {
            var builder = new ConfigurationBuilder()
                .AddRandomValueSource()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "foo:bar", "value" }
                });
            var config = builder.Build();
            var value = config["random:string"];
            Assert.NotNull(value);
        }

        [Fact]
        public void AddRandomValueSource_Uuid()
        {
            var builder = new ConfigurationBuilder()
                .AddRandomValueSource()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "foo:bar", "value" }
                });
            var config = builder.Build();
            var value = config["random:uuid"];
            Assert.NotNull(value);
        }

        [Fact]
        public void AddRandomValueSource_RandomInt()
        {
            var builder = new ConfigurationBuilder()
                .AddRandomValueSource()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "foo:bar", "value" }
                });
            var config = builder.Build();
            var value = config["random:int"];
            Assert.NotNull(value);
        }

        [Fact]
        public void AddRandomValueSource_RandomIntRange()
        {
            var builder = new ConfigurationBuilder()
                .AddRandomValueSource()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "foo:bar", "value" }
                });
            var config = builder.Build();
            var value = config["random:int[4,10]"];
            Assert.NotNull(value);
            var val = int.Parse(value);
            Assert.InRange(val, 4, 10);
        }

        [Fact]
        public void AddRandomValueSource_RandomIntMax()
        {
            var builder = new ConfigurationBuilder()
                .AddRandomValueSource()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "foo:bar", "value" }
                });
            var config = builder.Build();
            var value = config["random:int(10)"];
            Assert.NotNull(value);
            var val = int.Parse(value);
            Assert.InRange(val, 0, 10);
        }

        [Fact]
        public void AddRandomValueSource_RandomLong()
        {
            var builder = new ConfigurationBuilder()
                .AddRandomValueSource()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "foo:bar", "value" }
                });
            var config = builder.Build();
            var value = config["random:long"];
            Assert.NotNull(value);
        }

        [Fact]
        public void AddRandomValueSource_RandomLongRange()
        {
            var builder = new ConfigurationBuilder()
                .AddRandomValueSource()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "foo:bar", "value" }
                });
            var config = builder.Build();
            var value = config["random:long[4,10]"];
            Assert.NotNull(value);
            var val = int.Parse(value);
            Assert.InRange(val, 4, 10);
        }

        [Fact]
        public void AddRandomValueSource_RandomLongMax()
        {
            var builder = new ConfigurationBuilder()
                .AddRandomValueSource()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "foo:bar", "value" }
                });
            var config = builder.Build();
            var value = config["random:long(10)"];
            Assert.NotNull(value);
            var val = int.Parse(value);
            Assert.InRange(val, 0, 10);
        }
    }
}
