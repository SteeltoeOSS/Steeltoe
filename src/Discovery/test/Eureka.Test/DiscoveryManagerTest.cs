// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public class DiscoveryManagerTest : AbstractBaseTest
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
        Assert.Null(DiscoveryManager.Instance.ClientConfiguration);
        Assert.Null(DiscoveryManager.Instance.InstanceConfig);
    }

    [Fact]
    public void Initialize_Throws_IfInstanceConfigNull()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            DiscoveryManager.Instance.Initialize(new EurekaClientConfiguration(), (EurekaInstanceConfiguration)null));

        Assert.Contains("instanceConfig", ex.Message);
    }

    [Fact]
    public void Initialize_Throws_IfClientConfigNull()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => DiscoveryManager.Instance.Initialize(null, new EurekaInstanceConfiguration()));
        Assert.Contains("clientConfig", ex.Message);
    }

    [Fact]
    public void Initialize_WithBothConfigs_InitializesAll()
    {
        var instanceConfig = new EurekaInstanceConfiguration();

        var clientConfig = new EurekaClientConfiguration
        {
            ShouldRegisterWithEureka = false,
            ShouldFetchRegistry = false
        };

        DiscoveryManager.Instance.Initialize(clientConfig, instanceConfig);

        Assert.NotNull(DiscoveryManager.Instance.InstanceConfig);
        Assert.Equal(instanceConfig, DiscoveryManager.Instance.InstanceConfig);
        Assert.NotNull(DiscoveryManager.Instance.ClientConfiguration);
        Assert.Equal(clientConfig, DiscoveryManager.Instance.ClientConfiguration);
        Assert.NotNull(DiscoveryManager.Instance.Client);

        Assert.Equal(instanceConfig, ApplicationInfoManager.Instance.InstanceConfig);
    }

    [Fact]
    public void Initialize_WithClientConfig_InitializesAll()
    {
        var clientConfig = new EurekaClientConfiguration
        {
            ShouldRegisterWithEureka = false,
            ShouldFetchRegistry = false
        };

        DiscoveryManager.Instance.Initialize(clientConfig);

        Assert.Null(DiscoveryManager.Instance.InstanceConfig);
        Assert.NotNull(DiscoveryManager.Instance.ClientConfiguration);
        Assert.Equal(clientConfig, DiscoveryManager.Instance.ClientConfiguration);
        Assert.NotNull(DiscoveryManager.Instance.Client);

        Assert.Null(ApplicationInfoManager.Instance.InstanceConfig);
    }
}
