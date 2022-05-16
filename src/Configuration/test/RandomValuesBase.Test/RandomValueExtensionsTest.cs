// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Extensions.Configuration.RandomValue.Test
{
    public class RandomValueExtensionsTest
    {
        [Fact]
        public void AddRandomValueSource_ThrowsIfConfigBuilderNull()
        {
            IConfigurationBuilder configurationBuilder = null;

            var ex = Assert.Throws<ArgumentNullException>(() => configurationBuilder.AddRandomValueSource());
        }

        [Fact]
        public void AddRandomValueSource_ThrowsIfPrefixNull()
        {
            string prefix = null;

            var ex = Assert.Throws<ArgumentException>(() => new ConfigurationBuilder().AddRandomValueSource(prefix));
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
