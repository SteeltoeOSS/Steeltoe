// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Steeltoe.Discovery.Consul.Util;

namespace Steeltoe.Discovery.Consul.Test.Util;

public sealed class ConsulServerUtilsTest
{
    [Fact]
    public void FindHost_ReturnsExpected()
    {
        var serviceEntry = new ServiceEntry
        {
            Service = new AgentService(),
            Node = new Node()
        };

        serviceEntry.Service.Address = "fc00:ec:cd::242:ac11:c";

        ConsulServerUtils.FindHost(serviceEntry).Should().Be("[fc00:ec:cd:0:0:242:ac11:c]");

        serviceEntry.Service.Address = null;
        serviceEntry.Node.Address = "fc00:ec:cd::242:ac11:c";

        ConsulServerUtils.FindHost(serviceEntry).Should().Be("[fc00:ec:cd:0:0:242:ac11:c]");
    }

    [Fact]
    public void FixIPv6Address_Fixes()
    {
        ConsulServerUtils.FixIPv6Address("fc00:ec:cd::242:ac11:c").Should().Be("[fc00:ec:cd:0:0:242:ac11:c]");
        ConsulServerUtils.FixIPv6Address("[fc00:ec:cd::242:ac11:c]").Should().Be("[fc00:ec:cd:0:0:242:ac11:c]");
        ConsulServerUtils.FixIPv6Address("192.168.0.1").Should().Be("192.168.0.1");
        ConsulServerUtils.FixIPv6Address("projects.spring.io").Should().Be("projects.spring.io");
        ConsulServerUtils.FixIPv6Address("veryLongHostName").Should().Be("veryLongHostName");
    }
}
