// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public class EurekaClientConfigTest : AbstractBaseTest
{
    [Fact]
    public void DefaultConstructor_InitializedWithDefaults()
    {
        var config = new EurekaClientConfig();
        Assert.Equal(EurekaClientConfig.DefaultRegistryFetchIntervalSeconds, config.RegistryFetchIntervalSeconds);
        Assert.True(config.ShouldGZipContent);
        Assert.Equal(EurekaClientConfig.DefaultEurekaServerConnectTimeoutSeconds, config.EurekaServerConnectTimeoutSeconds);
        Assert.True(config.ShouldRegisterWithEureka);
        Assert.False(config.ShouldDisableDelta);
        Assert.True(config.ShouldFilterOnlyUpInstances);
        Assert.True(config.ShouldFetchRegistry);
        Assert.True(config.ShouldOnDemandUpdateStatusChange);
    }
}
