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
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Steeltoe.Common.Configuration.Test
{
    public class ConfigurationValuesHelperTest
    {
        [Fact]
        public void GetString_NoResolveFromConfig()
        {
            var dict = new Dictionary<string, string>()
            {
                { "a:b", "astring" }
            };

            IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();

            var result = ConfigurationValuesHelper.GetString("a:b", config, null, null);
            Assert.Equal("astring", result);
        }

        [Fact]
        public void GetInt_ReturnsValue()
        {
            var dict = new Dictionary<string, string>()
            {
                { "a:b", "100" }
            };

            IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();

            var result = ConfigurationValuesHelper.GetInt("a:b", config, null, 500);
            Assert.Equal(100, result);
        }

        [Fact]
        public void GetDouble_ReturnsValue()
        {
            var dict = new Dictionary<string, string>()
            {
                { "a:b", "100.00" }
            };

            IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();

            var result = ConfigurationValuesHelper.GetDouble("a:b", config, null, 500.00);
            Assert.Equal(100.00, result);
        }

        [Fact]
        public void GetBoolean_ReturnsValue()
        {
            var dict = new Dictionary<string, string>()
            {
                { "a:b", "True" }
            };

            IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();

            var result = ConfigurationValuesHelper.GetBoolean("a:b", config, null, false);
            Assert.True(result);
        }

        [Fact]
        public void GetInt_NotFoundReturnsDefault()
        {
            var dict = new Dictionary<string, string>()
            {
                { "a:b", "astring" }
            };

            IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();

            var result = ConfigurationValuesHelper.GetInt("a:b:c", config, null, 100);
            Assert.Equal(100, result);
        }

        [Fact]
        public void GetDouble_NotFoundReturnsDefault()
        {
            var dict = new Dictionary<string, string>()
            {
                { "a:b", "astring" }
            };

            IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();

            var result = ConfigurationValuesHelper.GetDouble("a:b:c", config, null, 100.00);
            Assert.Equal(100.00, result);
        }

        [Fact]
        public void GetBoolean_NotFoundReturnsDefault()
        {
            var dict = new Dictionary<string, string>()
            {
                { "a:b", "astring" }
            };

            IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();

            var result = ConfigurationValuesHelper.GetBoolean("a:b:c", config, null, true);
            Assert.True(result);
        }

        [Fact]
        public void GetString_NotFoundReturnsDefault()
        {
            var dict = new Dictionary<string, string>()
            {
                { "a:b", "astring" }
            };

            IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();

            var result = ConfigurationValuesHelper.GetString("a:b:c", config, null, "foobar");
            Assert.Equal("foobar", result);
        }

        [Fact]
        public void GetString_ResolvesReference()
        {
            var dict = new Dictionary<string, string>()
            {
                { "a:b", "${a:b:c}" }
            };
            var dict2 = new Dictionary<string, string>()
            {
                { "a:b:c", "astring" }
            };

            IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
            IConfiguration resolve = new ConfigurationBuilder().AddInMemoryCollection(dict2).Build();

            var result = ConfigurationValuesHelper.GetString("a:b", config, resolve, "foobar");
            Assert.Equal("astring", result);
        }

        [Fact]
        public void GetString_ResolveNotFoundReturnsNotResolvedValue()
        {
            var dict = new Dictionary<string, string>()
            {
                { "a:b", "${a:b:c}" }
            };
            var dict2 = new Dictionary<string, string>()
            {
                { "a:b:d", "astring" }
            };

            IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
            IConfiguration resolve = new ConfigurationBuilder().AddInMemoryCollection(dict2).Build();

            var result = ConfigurationValuesHelper.GetString("a:b", config, resolve, null);
            Assert.Equal("${a:b:c}", result);
        }

        [Fact]
        public void GetString_ResolveNotFoundReturnsPlaceholderDefault()
        {
            var dict = new Dictionary<string, string>()
            {
                { "a:b", "${a:b:c?placeholderdefault}" }
            };
            var dict2 = new Dictionary<string, string>()
            {
                { "a:b:d", "astring" }
            };

            IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
            IConfiguration resolve = new ConfigurationBuilder().AddInMemoryCollection(dict2).Build();

            var result = ConfigurationValuesHelper.GetString("a:b", config, resolve, "foobar");
            Assert.Equal("placeholderdefault", result);
        }

        [Fact]
        public void GetSetting_GetsFromFirst()
        {
            var dict = new Dictionary<string, string>()
            {
                { "a:b", "setting1" }
            };
            var dict2 = new Dictionary<string, string>()
            {
                { "a:b", "setting2" }
            };

            IConfiguration config1 = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
            IConfiguration config2 = new ConfigurationBuilder().AddInMemoryCollection(dict2).Build();

            var result = ConfigurationValuesHelper.GetSetting("a:b", config1, config2, null, "foobar");
            Assert.Equal("setting1", result);
        }

        [Fact]
        public void GetSetting_GetsFromSecond()
        {
            var dict = new Dictionary<string, string>()
            {
                { "a:b:c", "setting1" }
            };
            var dict2 = new Dictionary<string, string>()
            {
                { "a:b", "setting2" }
            };

            IConfiguration config1 = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
            IConfiguration config2 = new ConfigurationBuilder().AddInMemoryCollection(dict2).Build();

            var result = ConfigurationValuesHelper.GetSetting("a:b", config1, config2, null, "foobar");
            Assert.Equal("setting2", result);
        }
    }
}
