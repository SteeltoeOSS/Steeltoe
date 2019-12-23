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

using Steeltoe.Common;
using Steeltoe.Extensions.Configuration;
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
            var emptyConfig = TestHelpers.GetConfigurationFromDictionary(new Dictionary<string, string>());
            var opts = new CloudFoundryForwarderOptions(new ApplicationInstanceInfo(emptyConfig), new ServicesOptions(emptyConfig), emptyConfig);
            Assert.Equal(CloudFoundryForwarderOptions.DEFAULT_RATE, opts.RateMilli);
            Assert.True(opts.ValidateCertificates);
            Assert.Equal(CloudFoundryForwarderOptions.DEFAULT_TIMEOUT, opts.TimeoutSeconds);
            Assert.Null(opts.Endpoint);
            Assert.Null(opts.AccessToken);
            Assert.Null(opts.ApplicationId);
            Assert.Null(opts.InstanceId);
            Assert.Equal("-1", opts.InstanceIndex);
        }

        [Fact]
        public void Constructor_ThrowsForNulls()
        {
            var emptyConfig = TestHelpers.GetConfigurationFromDictionary(new Dictionary<string, string>());
            var appInfo = new ApplicationInstanceInfo(emptyConfig);
            var serviceInfo = new ServicesOptions(emptyConfig);
            var ex = Assert.Throws<ArgumentNullException>(() => new CloudFoundryForwarderOptions(null, serviceInfo, emptyConfig));
            Assert.Equal("appInfo", ex.ParamName);
            ex = Assert.Throws<ArgumentNullException>(() => new CloudFoundryForwarderOptions(appInfo, null, emptyConfig));
            Assert.Equal("serviceInfo", ex.ParamName);
            ex = Assert.Throws<ArgumentNullException>(() => new CloudFoundryForwarderOptions(appInfo, serviceInfo, null));
            Assert.Equal("config", ex.ParamName);
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
                ["management:metrics:exporter:cloudfoundry:instanceIndex"] = "1",
            };
            var config = TestHelpers.GetConfigurationFromDictionary(appsettings);

            var opts = new CloudFoundryForwarderOptions(new ApplicationInstanceInfo(config), new ServicesOptions(config), config);
            Assert.Equal(1000, opts.RateMilli);
            Assert.False(opts.ValidateCertificates);
            Assert.Equal(5, opts.TimeoutSeconds);
            Assert.Equal("https://foo.bar/foo", opts.Endpoint);
            Assert.Equal("token", opts.AccessToken);
            Assert.Equal("applicationId", opts.ApplicationId);
            Assert.Equal("instanceId", opts.InstanceId);
            Assert.Equal("1", opts.InstanceIndex);
        }
    }
}
