// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Exporter.Metrics.CloudFoundryForwarder.Test
{
    public class CloudFoundryForwarderOptionsTest : BaseTest
    {
        [Fact]
        public void Constructor_InitializesWithDefaults()
        {
            var opts = new CloudFoundryForwarderOptions();
            Assert.Equal(CloudFoundryForwarderOptions.DEFAULT_RATE, opts.RateMilli);
            Assert.True(opts.ValidateCertificates);
            Assert.Equal(CloudFoundryForwarderOptions.DEFAULT_TIMEOUT, opts.TimeoutSeconds);
            Assert.Null(opts.Endpoint);
            Assert.Null(opts.AccessToken);
            Assert.Null(opts.ApplicationId);
            Assert.Null(opts.InstanceId);
            Assert.Null(opts.InstanceIndex);
        }

        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            IConfiguration config = null;
            Assert.Throws<ArgumentNullException>(() => new CloudFoundryForwarderOptions(config));
        }

        [Fact]
        public void Constructor_BindsConfigurationCorrectly()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["management:metrics:exporter:cloudfoundry:endpoint"] = "https://foo.bar/foo",
                ["management:metrics:exporter:cloudfoundry:accessToken"] = "token",
                ["management:metrics:exporter:cloudfoundry:rateMilli"] = "1000",
                ["management:metrics:exporter:cloudfoundry:validateCertificates"] = "false",
                ["management:metrics:exporter:cloudfoundry:timeoutSeconds"] = "5",
                ["management:metrics:exporter:cloudfoundry:applicationId"] = "applicationId",
                ["management:metrics:exporter:cloudfoundry:instanceId"] = "instanceId",
                ["management:metrics:exporter:cloudfoundry:instanceIndex"] = "instanceIndex",
            };
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var opts = new CloudFoundryForwarderOptions(config);
            Assert.Equal(1000, opts.RateMilli);
            Assert.False(opts.ValidateCertificates);
            Assert.Equal(5, opts.TimeoutSeconds);
            Assert.Equal("https://foo.bar/foo", opts.Endpoint);
            Assert.Equal("token", opts.AccessToken);
            Assert.Equal("applicationId", opts.ApplicationId);
            Assert.Equal("instanceId", opts.InstanceId);
            Assert.Equal("instanceIndex", opts.InstanceIndex);
        }
    }
}
