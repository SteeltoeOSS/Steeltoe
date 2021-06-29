﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Info.Contributor;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.Info;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Endpoint.Info.Test
{
    public class AppSettingsInfoContributorTest : BaseTest
    {
        private readonly Dictionary<string, string> appSettings = new Dictionary<string, string>()
        {
            ["info:application:name"] = "foobar",
            ["info:application:version"] = "1.0.0",
            ["info:application:date"] = "5/1/2008",
            ["info:application:time"] = "8:30:52 AM",
            ["info:NET:type"] = "Core",
            ["info:NET:version"] = "2.0.0"
        };

        [Fact]
        public void ConstributeWithConfigNull()
        {
            var contributor = new AppSettingsInfoContributor(null);
            var builder = new InfoBuilder();
            contributor.Contribute(builder);
            var result = builder.Build();
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void ContributeWithNullBUilderThrows()
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appSettings);
            var config = configurationBuilder.Build();
            var settings = new AppSettingsInfoContributor(config);

            Assert.Throws<ArgumentNullException>(() => settings.Contribute(null));
        }

        [Fact]
        public void ContributeAddsToBuilder()
        {
            appSettings.Add("info:NET:ASPNET:type", "Core");
            appSettings.Add("info:NET:ASPNET:version", "2.0.0");
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appSettings);
            var config = configurationBuilder.Build();
            var settings = new AppSettingsInfoContributor(config);

            var builder = new InfoBuilder();
            settings.Contribute(builder);

            var info = builder.Build();
            Assert.NotNull(info);
            Assert.Equal(2, info.Count);
            Assert.True(info.ContainsKey("application"));
            Assert.True(info.ContainsKey("NET"));

            var appNode = info["application"] as Dictionary<string, object>;
            Assert.NotNull(appNode);
            Assert.Equal("foobar", appNode["name"]);

            var netNode = info["NET"] as Dictionary<string, object>;
            Assert.NotNull(netNode);
            Assert.Equal("Core", netNode["type"]);

            Assert.NotNull(netNode["ASPNET"]);
        }
    }
}
