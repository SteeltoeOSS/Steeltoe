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

using Consul;
using Xunit;

namespace Steeltoe.Discovery.Consul.Util.Test
{
    public class ConsulServerUtilsTest
    {
        [Fact]
        public void FindHost_ReturnsExpected()
        {
            ServiceEntry hs = new ServiceEntry();
            hs.Service = new AgentService();
            hs.Node = new Node();
            hs.Service.Address = "fc00:ec:cd::242:ac11:c";

            string s1 = ConsulServerUtils.FindHost(hs);
            Assert.Equal("[fc00:ec:cd:0:0:242:ac11:c]", s1);

            hs.Service.Address = null;
            hs.Node.Address = "fc00:ec:cd::242:ac11:c";
            string s2 = ConsulServerUtils.FindHost(hs);
            Assert.Equal("[fc00:ec:cd:0:0:242:ac11:c]", s2);
        }

        [Fact]
        public void FixIpv6Address_Fixes()
        {
            string s1 = ConsulServerUtils.FixIPv6Address("fc00:ec:cd::242:ac11:c");
            Assert.Equal("[fc00:ec:cd:0:0:242:ac11:c]", s1);

            string s2 = ConsulServerUtils.FixIPv6Address("[fc00:ec:cd::242:ac11:c]");
            Assert.Equal("[fc00:ec:cd:0:0:242:ac11:c]", s2);

            string s3 = ConsulServerUtils.FixIPv6Address("192.168.0.1");
            Assert.Equal("192.168.0.1", s3);

            string s4 = ConsulServerUtils.FixIPv6Address("projects.spring.io");
            Assert.Equal("projects.spring.io", s4);

            string s5 = ConsulServerUtils.FixIPv6Address("veryLongHostName");
            Assert.Equal("veryLongHostName", s5);
        }
    }
}
