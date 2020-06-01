// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Xunit;

#pragma warning disable CS0618 // Type or member is obsolete
namespace Steeltoe.Management.Endpoint.Test
{
    public class ManagementOptionsTest : BaseTest
    {
        [Fact]
        public void InitializedWithDefaults()
        {
            ManagementOptions opts = ManagementOptions.GetInstance();
            Assert.False(opts.Enabled.HasValue);
            Assert.Equal("/", opts.Path);
        }

        [Fact]
        public void ThrowsIfConfigNull()
        {
            IConfiguration config = null;
            Assert.Throws<ArgumentNullException>(() => ManagementOptions.GetInstance(config));
        }

        [Fact]
        public void BindsConfigurationCorrectly()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "false",
                ["management:endpoints:path"] = "/management",
                ["management:endpoints:info:enabled"] = "true",
                ["management:endpoints:info:id"] = "/infomanagement"
            };
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            ManagementOptions opts = ManagementOptions.GetInstance(config);
            Assert.False(opts.Enabled);
            Assert.Equal("/management", opts.Path);
        }
    }
}

#pragma warning restore CS0618 // Type or member is obsolete