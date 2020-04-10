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
using System.Linq;
using Xunit;

namespace Steeltoe.Connector.GemFire.Test
{
    public class GemFireConnectorOptionsTest
    {
        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            IConfiguration config = null;

            var ex = Assert.Throws<ArgumentNullException>(() => new GemFireConnectorOptions(config));
            Assert.Equal(nameof(config), ex.ParamName);
        }

        [Fact]
        public void Constructor_BindsValues_InPCCFormat()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["gemfire:client:locators:0"] = "localhost[10334]",
                ["gemfire:client:password"] = "password",
                ["gemfire:client:username"] = "developer"
            };

            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var sconfig = new GemFireConnectorOptions(config);
            var locators = sconfig.ParsedLocators();
            Assert.NotNull(locators);
            Assert.Equal("localhost", locators.First().Key);
            Assert.Equal(10334, locators.First().Value);
            Assert.Equal("password", sconfig.Password);
            Assert.Equal("developer", sconfig.Username);
        }

        [Fact]
        public void Constructor_BindsValues_InNormalFormat()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["gemfire:client:locators:0"] = "localhost:10334",
                ["gemfire:client:password"] = "password",
                ["gemfire:client:username"] = "developer"
            };

            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var sconfig = new GemFireConnectorOptions(config);
            var locators = sconfig.ParsedLocators();
            Assert.NotNull(locators);
            Assert.Equal("localhost", locators.First().Key);
            Assert.Equal(10334, locators.First().Value);
            Assert.Equal("password", sconfig.Password);
            Assert.Equal("developer", sconfig.Username);
        }

        [Fact]
        public void Constructor_Binds_OtherProperties()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["gemfire:client:properties:ping-interval"] = "99",
                ["gemfire:client:properties:someKey"] = "someValue",
                ["gemfire:client:properties:someOtherKey"] = "someOtherValue",
            };

            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var sconfig = new GemFireConnectorOptions(config);
            var props = sconfig.Properties;
            Assert.Equal("99", props["ping-interval"]);
            Assert.Equal("someValue", props["someKey"]);
            Assert.Equal("someOtherValue", props["someOtherKey"]);
        }
    }
}
