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

using System.Net;
using Xunit;

namespace Steeltoe.Common.Net.Test
{
    public class InetUtilsTest
    {
        [Fact]
        public void TestGetFirstNonLoopbackHostInfo()
        {
            var utils = new InetUtils(new InetOptions());
            Assert.NotNull(utils.FindFirstNonLoopbackHostInfo());
        }

        [Fact]
        public void TestGetFirstNonLoopbackAddress()
        {
            var utils = new InetUtils(new InetOptions());
            Assert.NotNull(utils.FindFirstNonLoopbackAddress());
        }

        [Fact]
        public void TestConvert()
        {
            var utils = new InetUtils(new InetOptions());
            Assert.NotNull(utils.ConvertAddress(Dns.GetHostEntry("localhost").AddressList[0]));
        }

        [Fact]
        public void TestHostInfo()
        {
            var utils = new InetUtils(new InetOptions());
            var info = utils.FindFirstNonLoopbackHostInfo();
            Assert.NotNull(info.IpAddress);
        }

        [Fact]
        public void TestIgnoreInterface()
        {
            var properties = new InetOptions()
            {
                IgnoredInterfaces = "docker0,veth.*"
            };

            var inetUtils = new InetUtils(properties);

            Assert.True(inetUtils.IgnoreInterface("docker0"));
            Assert.True(inetUtils.IgnoreInterface("vethAQI2QT"));
            Assert.False(inetUtils.IgnoreInterface("docker1"));
        }

        [Fact]
        public void TestDefaultIgnoreInterface()
        {
            var inetUtils = new InetUtils(new InetOptions());
            Assert.False(inetUtils.IgnoreInterface("docker0"));
        }

        [Fact]
        public void TestSiteLocalAddresses()
        {
            var properties = new InetOptions()
            {
                UseOnlySiteLocalInterfaces = true
            };

            var utils = new InetUtils(properties);
            Assert.True(utils.IsPreferredAddress(IPAddress.Parse("192.168.0.1")));
            Assert.False(utils.IsPreferredAddress(IPAddress.Parse("5.5.8.1")));
        }

        [Fact]
        public void TestPreferredNetworksRegex()
        {
            var properties = new InetOptions()
            {
                PreferredNetworks = "192.168.*,10.0.*"
            };

            var utils = new InetUtils(properties);
            Assert.True(utils.IsPreferredAddress(IPAddress.Parse("192.168.0.1")));
            Assert.False(utils.IsPreferredAddress(IPAddress.Parse("5.5.8.1")));
            Assert.True(utils.IsPreferredAddress(IPAddress.Parse("10.0.10.1")));
            Assert.False(utils.IsPreferredAddress(IPAddress.Parse("10.255.10.1")));
        }

        [Fact]
        public void TestPreferredNetworksSimple()
        {
            var properties = new InetOptions()
            {
                PreferredNetworks = "192,10.0"
            };

            var utils = new InetUtils(properties);
            Assert.True(utils.IsPreferredAddress(IPAddress.Parse("192.168.0.1")));
            Assert.False(utils.IsPreferredAddress(IPAddress.Parse("5.5.8.1")));
            Assert.False(utils.IsPreferredAddress(IPAddress.Parse("10.255.10.1")));
            Assert.True(utils.IsPreferredAddress(IPAddress.Parse("10.0.10.1")));
        }

        [Fact]
        public void TestPreferredNetworksListIsEmpty()
        {
            var properties = new InetOptions();

            var utils = new InetUtils(properties);
            Assert.True(utils.IsPreferredAddress(IPAddress.Parse("192.168.0.1")));
            Assert.True(utils.IsPreferredAddress(IPAddress.Parse("5.5.8.1")));
            Assert.True(utils.IsPreferredAddress(IPAddress.Parse("10.255.10.1")));
            Assert.True(utils.IsPreferredAddress(IPAddress.Parse("10.0.10.1")));
        }
    }
}
