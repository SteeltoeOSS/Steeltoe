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
        var configuration = new EurekaClientConfiguration();
        Assert.Equal(EurekaClientConfiguration.DefaultRegistryFetchIntervalSeconds, configuration.RegistryFetchIntervalSeconds);
        Assert.True(configuration.ShouldGZipContent);
        Assert.Equal(EurekaClientConfiguration.DefaultEurekaServerConnectTimeoutSeconds, configuration.EurekaServerConnectTimeoutSeconds);
        Assert.True(configuration.ShouldRegisterWithEureka);
        Assert.False(configuration.ShouldDisableDelta);
        Assert.True(configuration.ShouldFilterOnlyUpInstances);
        Assert.True(configuration.ShouldFetchRegistry);
        Assert.True(configuration.ShouldOnDemandUpdateStatusChange);
    }
}
