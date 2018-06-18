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
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using System.Linq;
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

            options = new RefreshOptions();
            Assert.Throws<ArgumentNullException>(() => new RefreshEndpoint(options, configuration));
        }

        [Fact]
        public void DoInvoke_ReturnsExpected()
        {
            var opts = new RefreshOptions();
            var appsettings = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "false",
                ["management:endpoints:sensitive"] = "false",
                ["management:endpoints:path"] = "/cloudfoundryapplication",
                ["management:endpoints:loggers:enabled"] = "false",
                ["management:endpoints:loggers:sensitive"] = "true",
                ["management:endpoints:heapdump:enabled"] = "true",
                ["management:endpoints:heapdump:sensitive"] = "true",
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
            Assert.Contains("management:endpoints:heapdump:sensitive", result);
            Assert.Contains("management:endpoints:cloudfoundry:enabled", result);
        }
    }
}
