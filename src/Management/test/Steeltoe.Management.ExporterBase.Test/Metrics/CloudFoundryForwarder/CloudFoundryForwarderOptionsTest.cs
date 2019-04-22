// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
                ["management:metrics:exporter:cloudfoundry:endpoint"] = "http://foo.bar/foo",
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
            Assert.Equal("http://foo.bar/foo", opts.Endpoint);
            Assert.Equal("token", opts.AccessToken);
            Assert.Equal("applicationId", opts.ApplicationId);
            Assert.Equal("instanceId", opts.InstanceId);
            Assert.Equal("instanceIndex", opts.InstanceIndex);
        }
    }
}
