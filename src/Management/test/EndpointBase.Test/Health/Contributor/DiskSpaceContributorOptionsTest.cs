﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Endpoint.Health.Contributor.Test
{
    public class DiskSpaceContributorOptionsTest : BaseTest
    {
        [Fact]
        public void Constructor_InitializesWithDefaults()
        {
            var opts = new DiskSpaceContributorOptions();
            Assert.Equal(".", opts.Path);
            Assert.Equal(10 * 1024 * 1024, opts.Threshold);
        }

        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            IConfiguration config = null;
            Assert.Throws<ArgumentNullException>(() => new DiskSpaceContributorOptions(config));
        }

        [Fact]
        public void Constructor_BindsConfigurationCorrectly()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["management:endpoints:health:enabled"] = "true",
                ["management:endpoints:health:diskspace:path"] = "foobar",
                ["management:endpoints:health:diskspace:threshold"] = "5"
            };
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var opts = new DiskSpaceContributorOptions(config);
            Assert.Equal("foobar", opts.Path);
            Assert.Equal(5, opts.Threshold);
        }
    }
}
