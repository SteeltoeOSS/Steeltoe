// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public class EurekaDiscoveryManagerTest : AbstractBaseTest
{
    [Fact]
    public void Constructor_Initializes_Correctly()
    {
        var instOptions = new EurekaInstanceOptions();
        var clientOptions = new EurekaClientOptions() { EurekaServerRetryCount = 0 };
        var wrapInst = new TestOptionMonitorWrapper<EurekaInstanceOptions>(instOptions);
        var wrapClient = new TestOptionMonitorWrapper<EurekaClientOptions>(clientOptions);
        var appMgr = new EurekaApplicationInfoManager(wrapInst);
        var client = new EurekaDiscoveryClient(wrapClient, wrapInst, appMgr);

        var mgr = new EurekaDiscoveryManager(wrapClient, wrapInst, client);
        Assert.Equal(instOptions, mgr.InstanceConfig);
        Assert.Equal(clientOptions, mgr.ClientConfig);
        Assert.Equal(client, mgr.Client);
    }
}