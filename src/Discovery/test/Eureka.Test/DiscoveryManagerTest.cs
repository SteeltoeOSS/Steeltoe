// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.TestResources;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class DiscoveryManagerTest : AbstractBaseTest
{
    [Fact]
    public void DiscoveryManager_IsSingleton()
    {
        Assert.Equal(EurekaApplicationInfoManager.SharedInstance, EurekaApplicationInfoManager.SharedInstance);
    }

    [Fact]
    public void DiscoveryManager_Uninitialized()
    {
        Assert.Null(EurekaDiscoveryManager.SharedInstance.Client);
        Assert.Null(EurekaDiscoveryManager.SharedInstance.ClientOptions);
        Assert.Null(EurekaDiscoveryManager.SharedInstance.InstanceOptions);
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

        EurekaDiscoveryManager.SharedInstance.Initialize(clientOptionsMonitor, instanceOptionsMonitor, NullLoggerFactory.Instance);

        Assert.NotNull(EurekaDiscoveryManager.SharedInstance.InstanceOptions);
        Assert.Equal(instanceOptions, EurekaDiscoveryManager.SharedInstance.InstanceOptions);
        Assert.NotNull(EurekaDiscoveryManager.SharedInstance.ClientOptions);
        Assert.Equal(clientOptions, EurekaDiscoveryManager.SharedInstance.ClientOptions);
        Assert.NotNull(EurekaDiscoveryManager.SharedInstance.Client);

        Assert.Equal(instanceOptions, EurekaApplicationInfoManager.SharedInstance.InstanceOptions);
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

        EurekaDiscoveryManager.SharedInstance.Initialize(clientOptionsMonitor, NullLoggerFactory.Instance);

        Assert.Null(EurekaDiscoveryManager.SharedInstance.InstanceOptions);
        Assert.NotNull(EurekaDiscoveryManager.SharedInstance.ClientOptions);
        Assert.Equal(clientOptions, EurekaDiscoveryManager.SharedInstance.ClientOptions);
        Assert.NotNull(EurekaDiscoveryManager.SharedInstance.Client);

        Assert.Null(EurekaApplicationInfoManager.SharedInstance.InstanceOptions);
    }
}
