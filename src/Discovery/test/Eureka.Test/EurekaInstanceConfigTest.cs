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
        var configuration = new EurekaInstanceConfiguration();

        string thisHostName = configuration.GetHostName(false);
        string thisHostAddress = configuration.GetHostAddress(false);

        Assert.False(configuration.IsInstanceEnabledOnInit);
        Assert.Equal(EurekaInstanceConfiguration.DefaultNonSecurePort, configuration.NonSecurePort);
        Assert.Equal(EurekaInstanceConfiguration.DefaultSecurePort, configuration.SecurePort);
        Assert.True(configuration.IsNonSecurePortEnabled);
        Assert.False(configuration.SecurePortEnabled);
        Assert.Equal(EurekaInstanceConfiguration.DefaultLeaseRenewalIntervalInSeconds, configuration.LeaseRenewalIntervalInSeconds);
        Assert.Equal(EurekaInstanceConfiguration.DefaultLeaseExpirationDurationInSeconds, configuration.LeaseExpirationDurationInSeconds);
        Assert.Equal($"{thisHostName}:{configuration.SecurePort}", configuration.SecureVirtualHostName);
        Assert.Equal(thisHostAddress, configuration.IpAddress);
        Assert.Equal(EurekaInstanceConfiguration.DefaultAppName, configuration.AppName);
        Assert.Equal(EurekaInstanceConfiguration.DefaultStatusPageUrlPath, configuration.StatusPageUrlPath);
        Assert.Equal(EurekaInstanceConfiguration.DefaultHomePageUrlPath, configuration.HomePageUrlPath);
        Assert.Equal(EurekaInstanceConfiguration.DefaultHealthCheckUrlPath, configuration.HealthCheckUrlPath);
        Assert.NotNull(configuration.MetadataMap);
        Assert.Empty(configuration.MetadataMap);
        Assert.Equal(DataCenterName.MyOwn, configuration.DataCenterInfo.Name);
    }
}
