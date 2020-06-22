﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Endpoint.Refresh.Test
{
    public class RefreshEndpointTest : BaseTest
    {
        [Fact]
        public void Constructor_ThrowsIfNulls()
        {
            IRefreshOptions options = null;
            IConfigurationRoot configuration = null;

            Assert.Throws<ArgumentNullException>(() => new RefreshEndpoint(options, configuration));

            options = new RefreshEndpointOptions();
            Assert.Throws<ArgumentNullException>(() => new RefreshEndpoint(options, configuration));
        }

        [Fact]
        public void DoInvoke_ReturnsExpected()
        {
            var opts = new RefreshEndpointOptions();
            var appsettings = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "false",
                ["management:endpoints:path"] = "/cloudfoundryapplication",
                ["management:endpoints:loggers:enabled"] = "false",
                ["management:endpoints:heapdump:enabled"] = "true",
                ["management:endpoints:cloudfoundry:validatecertificates"] = "true",
                ["management:endpoints:cloudfoundry:enabled"] = "true"
            };
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var ep = new RefreshEndpoint(opts, config);
            var result = ep.DoInvoke(config);
            Assert.NotNull(result);

            Assert.Contains("management:endpoints:loggers:enabled", result);
            Assert.Contains("management:endpoints:cloudfoundry:enabled", result);
        }
    }
}
