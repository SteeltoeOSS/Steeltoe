// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.AppInfo;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public class EurekaInstanceConfigTest : AbstractBaseTest
{
    [Fact]
    public void DefaultConstructor_InitializedWithDefaults()
    {
        var config = new EurekaInstanceConfig();

        string thisHostName = config.GetHostName(false);
        string thisHostAddress = config.GetHostAddress(false);

        Assert.False(config.IsInstanceEnabledOnInit);
        Assert.Equal(EurekaInstanceConfig.DefaultNonSecurePort, config.NonSecurePort);
        Assert.Equal(EurekaInstanceConfig.DefaultSecurePort, config.SecurePort);
        Assert.True(config.IsNonSecurePortEnabled);
        Assert.False(config.SecurePortEnabled);
        Assert.Equal(EurekaInstanceConfig.DefaultLeaseRenewalIntervalInSeconds, config.LeaseRenewalIntervalInSeconds);
        Assert.Equal(EurekaInstanceConfig.DefaultLeaseExpirationDurationInSeconds, config.LeaseExpirationDurationInSeconds);
        Assert.Equal($"{thisHostName}:{config.SecurePort}", config.SecureVirtualHostName);
        Assert.Equal(thisHostAddress, config.IpAddress);
        Assert.Equal(EurekaInstanceConfig.DefaultAppName, config.AppName);
        Assert.Equal(EurekaInstanceConfig.DefaultStatusPageUrlPath, config.StatusPageUrlPath);
        Assert.Equal(EurekaInstanceConfig.DefaultHomePageUrlPath, config.HomePageUrlPath);
        Assert.Equal(EurekaInstanceConfig.DefaultHealthCheckUrlPath, config.HealthCheckUrlPath);
        Assert.NotNull(config.MetadataMap);
        Assert.Empty(config.MetadataMap);
        Assert.Equal(DataCenterName.MyOwn, config.DataCenterInfo.Name);
    }
}
