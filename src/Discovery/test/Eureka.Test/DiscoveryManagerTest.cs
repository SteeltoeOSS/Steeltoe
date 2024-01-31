// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.TestResources;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class DiscoveryManagerTest : AbstractBaseTest
{
    [Fact]
    public void DiscoveryManager_IsSingleton()
    {
        Assert.Equal(ApplicationInfoManager.Instance, ApplicationInfoManager.Instance);
    }

    [Fact]
    public void DiscoveryManager_Uninitialized()
    {
        Assert.Null(EurekaDiscoveryManager.Instance.Client);
        Assert.Null(EurekaDiscoveryManager.Instance.ClientOptions);
        Assert.Null(EurekaDiscoveryManager.Instance.InstanceOptions);
    }

    [Fact]
    public void Initialize_WithBothConfigs_InitializesAll()
    {
        var instanceOptions = new EurekaInstanceOptions();

        var clientOptions = new EurekaClientOptions
        {
            ShouldRegisterWithEureka = false,
            ShouldFetchRegistry = false
        };

        var instanceOptionsMonitor = new TestOptionsMonitor<EurekaInstanceOptions>(instanceOptions);
        var clientOptionsMonitor = new TestOptionsMonitor<EurekaClientOptions>(clientOptions);

        EurekaDiscoveryManager.Instance.Initialize(clientOptionsMonitor, instanceOptionsMonitor);

        Assert.NotNull(EurekaDiscoveryManager.Instance.InstanceOptions);
        Assert.Equal(instanceOptions, EurekaDiscoveryManager.Instance.InstanceOptions);
        Assert.NotNull(EurekaDiscoveryManager.Instance.ClientOptions);
        Assert.Equal(clientOptions, EurekaDiscoveryManager.Instance.ClientOptions);
        Assert.NotNull(EurekaDiscoveryManager.Instance.Client);

        Assert.Equal(instanceOptions, ApplicationInfoManager.Instance.InstanceOptions);
    }

    [Fact]
    public void Initialize_WithClientConfig_InitializesAll()
    {
        var clientOptions = new EurekaClientOptions
        {
            ShouldRegisterWithEureka = false,
            ShouldFetchRegistry = false
        };

        var clientOptionsMonitor = new TestOptionsMonitor<EurekaClientOptions>(clientOptions);

        EurekaDiscoveryManager.Instance.Initialize(clientOptionsMonitor);

        Assert.Null(EurekaDiscoveryManager.Instance.InstanceOptions);
        Assert.NotNull(EurekaDiscoveryManager.Instance.ClientOptions);
        Assert.Equal(clientOptions, EurekaDiscoveryManager.Instance.ClientOptions);
        Assert.NotNull(EurekaDiscoveryManager.Instance.Client);

        Assert.Null(ApplicationInfoManager.Instance.InstanceOptions);
    }
}
