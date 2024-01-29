// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
        Assert.Null(DiscoveryManager.Instance.Client);
        Assert.Null(DiscoveryManager.Instance.ClientOptions);
        Assert.Null(DiscoveryManager.Instance.InstanceOptions);
    }

    [Fact]
    public void Initialize_Throws_IfInstanceConfigNull()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            DiscoveryManager.Instance.Initialize(new EurekaClientOptions(), (EurekaInstanceOptions)null));

        Assert.Contains("instanceConfig", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Initialize_Throws_IfClientConfigNull()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => DiscoveryManager.Instance.Initialize(null, new EurekaInstanceOptions()));
        Assert.Contains("clientConfig", ex.Message, StringComparison.Ordinal);
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

        DiscoveryManager.Instance.Initialize(clientOptions, instanceOptions);

        Assert.NotNull(DiscoveryManager.Instance.InstanceOptions);
        Assert.Equal(instanceOptions, DiscoveryManager.Instance.InstanceOptions);
        Assert.NotNull(DiscoveryManager.Instance.ClientOptions);
        Assert.Equal(clientOptions, DiscoveryManager.Instance.ClientOptions);
        Assert.NotNull(DiscoveryManager.Instance.Client);

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

        DiscoveryManager.Instance.Initialize(clientOptions);

        Assert.Null(DiscoveryManager.Instance.InstanceOptions);
        Assert.NotNull(DiscoveryManager.Instance.ClientOptions);
        Assert.Equal(clientOptions, DiscoveryManager.Instance.ClientOptions);
        Assert.NotNull(DiscoveryManager.Instance.Client);

        Assert.Null(ApplicationInfoManager.Instance.InstanceOptions);
    }
}
