// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Xunit;

namespace Steeltoe.Consul.Util.Test
{
    public class ConsulServerUtilsTest
    {
        [Fact]
        public void FindHost_ReturnsExpected()
        {
            var hs = new ServiceEntry
            {
                Service = new AgentService(),
                Node = new Node()
            };
            hs.Service.Address = "fc00:ec:cd::242:ac11:c";

            var s1 = ConsulServerUtils.FindHost(hs);
            Assert.Equal("[fc00:ec:cd:0:0:242:ac11:c]", s1);

            hs.Service.Address = null;
            hs.Node.Address = "fc00:ec:cd::242:ac11:c";
            var s2 = ConsulServerUtils.FindHost(hs);
            Assert.Equal("[fc00:ec:cd:0:0:242:ac11:c]", s2);
        }

        [Fact]
        public void FixIpv6Address_Fixes()
        {
            var s1 = ConsulServerUtils.FixIPv6Address("fc00:ec:cd::242:ac11:c");
            Assert.Equal("[fc00:ec:cd:0:0:242:ac11:c]", s1);

            var s2 = ConsulServerUtils.FixIPv6Address("[fc00:ec:cd::242:ac11:c]");
            Assert.Equal("[fc00:ec:cd:0:0:242:ac11:c]", s2);

            var s3 = ConsulServerUtils.FixIPv6Address("192.168.0.1");
            Assert.Equal("192.168.0.1", s3);

            var s4 = ConsulServerUtils.FixIPv6Address("projects.spring.io");
            Assert.Equal("projects.spring.io", s4);

            var s5 = ConsulServerUtils.FixIPv6Address("veryLongHostName");
            Assert.Equal("veryLongHostName", s5);
        }
    }
}
