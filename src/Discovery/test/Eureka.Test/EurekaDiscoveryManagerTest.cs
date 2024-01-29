// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class EurekaDiscoveryManagerTest : AbstractBaseTest
{
    [Fact]
    public void Constructor_Initializes_Correctly()
    {
        var instanceOptions = new EurekaInstanceOptions();

        var clientOptions = new EurekaClientOptions
        {
            EurekaServer =
            {
                RetryCount = 0
            }
        };

        var instanceOptionsMonitor = new TestOptionMonitorWrapper<EurekaInstanceOptions>(instanceOptions);
        var clientOptionsMonitor = new TestOptionMonitorWrapper<EurekaClientOptions>(clientOptions);

        var applicationInfoManager = new EurekaApplicationInfoManager(instanceOptionsMonitor);
        var discoveryClient = new EurekaDiscoveryClient(clientOptionsMonitor, instanceOptionsMonitor, applicationInfoManager);

        var discoveryManager = new EurekaDiscoveryManager(clientOptionsMonitor, instanceOptionsMonitor, discoveryClient);

        Assert.Equal(instanceOptions, discoveryManager.InstanceOptions);
        Assert.Equal(clientOptions, discoveryManager.ClientOptions);
        Assert.Equal(discoveryClient, discoveryManager.Client);
    }
}
