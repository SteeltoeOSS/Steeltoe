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
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Endpoint.Trace.Test
{
    public class TraceOptionsTest : BaseTest
    {
        [Fact]
        public void Constructor_InitializesWithDefaults()
        {
            var opts = new TraceOptions();
            Assert.True(opts.Enabled);
            Assert.True(opts.Sensitive);
            Assert.Equal("httptrace", opts.Id);
            Assert.Equal(100, opts.Capacity);
            Assert.True(opts.AddTimeTaken);
            Assert.True(opts.AddRequestHeaders);
            Assert.True(opts.AddResponseHeaders);
            Assert.False(opts.AddPathInfo);
            Assert.False(opts.AddUserPrincipal);
            Assert.False(opts.AddParameters);
            Assert.False(opts.AddQueryString);
            Assert.False(opts.AddAuthType);
            Assert.False(opts.AddRemoteAddress);
            Assert.False(opts.AddSessionId);
        }

        [Fact]
        public void Contstructor_ThrowsIfConfigNull()
        {
            IConfiguration config = null;
            Assert.Throws<ArgumentNullException>(() => new TraceOptions(config));
        }

        [Fact]
        public void Contstructor_BindsConfigurationCorrectly()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "false",
                ["management:endpoints:sensitive"] = "false",
                ["management:endpoints:path"] = "/cloudfoundryapplication",
                ["management:endpoints:loggers:enabled"] = "false",
                ["management:endpoints:loggers:sensitive"] = "true",
                ["management:endpoints:trace:enabled"] = "true",
                ["management:endpoints:trace:sensitive"] = "true",
                ["management:endpoints:trace:capacity"] = "1000",
                ["management:endpoints:trace:addTimeTaken"] = "false",
                ["management:endpoints:trace:addRequestHeaders"] = "false",
                ["management:endpoints:trace:addResponseHeaders"] = "false",
                ["management:endpoints:trace:addPathInfo"] = "true",
                ["management:endpoints:trace:addUserPrincipal"] = "true",
                ["management:endpoints:trace:addParameters"] = "true",
                ["management:endpoints:trace:addQueryString"] = "true",
                ["management:endpoints:trace:addAuthType"] = "true",
                ["management:endpoints:trace:addRemoteAddress"] = "true",
                ["management:endpoints:trace:addSessionId"] = "true",
                ["management:endpoints:cloudfoundry:validatecertificates"] = "true",
                ["management:endpoints:cloudfoundry:enabled"] = "true"
            };
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var opts = new TraceOptions(config);
            CloudFoundryOptions cloudOpts = new CloudFoundryOptions(config);

            Assert.True(cloudOpts.Enabled);
            Assert.False(cloudOpts.Sensitive);
            Assert.Equal(string.Empty, cloudOpts.Id);
            Assert.Equal("/cloudfoundryapplication", cloudOpts.Path);
            Assert.True(cloudOpts.ValidateCertificates);

            Assert.True(opts.Enabled);
            Assert.True(opts.Sensitive);
            Assert.Equal("trace", opts.Id);
            Assert.Equal("/cloudfoundryapplication/trace", opts.Path);
            Assert.Equal(1000, opts.Capacity);
            Assert.False(opts.AddTimeTaken);
            Assert.False(opts.AddRequestHeaders);
            Assert.False(opts.AddResponseHeaders);
            Assert.True(opts.AddPathInfo);
            Assert.True(opts.AddUserPrincipal);
            Assert.True(opts.AddParameters);
            Assert.True(opts.AddQueryString);
            Assert.True(opts.AddAuthType);
            Assert.True(opts.AddRemoteAddress);
            Assert.True(opts.AddSessionId);
        }
    }
}
