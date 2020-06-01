// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.GemFire.Test
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
