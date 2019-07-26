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

using Xunit;

namespace Steeltoe.Discovery.Consul.Discovery.Test
{
    public class ConsulDiscoveryOptionsTest
    {
        [Fact]
        [Trait("Category", "SkipOnMacOS")]
        public void Constructor_InitsDefaults()
        {
            ConsulDiscoveryOptions opts = new ConsulDiscoveryOptions();
            Assert.True(opts.Register);
            Assert.True(opts.RegisterHealthCheck);
            Assert.Null(opts.DefaultQueryTag);
            Assert.Equal("zone", opts.DefaultZoneMetadataName);
            Assert.True(opts.Deregister);
            Assert.True(opts.Enabled);
            Assert.True(opts.FailFast);
            Assert.Equal("30m", opts.HealthCheckCriticalTimeout);
            Assert.Equal("10s", opts.HealthCheckInterval);
            Assert.Equal("/actuator/health", opts.HealthCheckPath);
            Assert.Equal("10s", opts.HealthCheckTimeout);
            Assert.False(opts.HealthCheckTlsSkipVerify);
            Assert.Null(opts.HealthCheckUrl);
            Assert.NotNull(opts.Heartbeat);
            Assert.NotNull(opts.HostName);
            Assert.Null(opts.InstanceGroup);
            Assert.Null(opts.InstanceZone);
            Assert.NotNull(opts.IpAddress); // TODO: this is null on MacOS
            Assert.False(opts.PreferIpAddress);
            Assert.False(opts.PreferAgentAddress);
            Assert.False(opts.QueryPassing);
            Assert.Equal("http", opts.Scheme);
            Assert.Null(opts.ServiceName);
            Assert.Null(opts.Tags);
        }
    }
}
