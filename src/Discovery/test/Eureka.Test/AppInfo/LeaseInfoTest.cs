// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Configuration;
using Steeltoe.Discovery.Eureka.Transport;
using Steeltoe.Discovery.Eureka.Util;

namespace Steeltoe.Discovery.Eureka.Test.AppInfo;

public sealed class LeaseInfoTest
{
    [Fact]
    public void FromJson_Correct()
    {
        var leaseInfo = new JsonLeaseInfo
        {
            RenewalIntervalInSeconds = 100,
            DurationInSeconds = 200,
            RegistrationTimestamp = 1_457_973_741_708,
            LastRenewalTimestamp = 1_457_973_741_708,
            EvictionTimestamp = 1_457_973_741_708,
            ServiceUpTimestamp = 1_457_973_741_708
        };

        LeaseInfo? result = LeaseInfo.FromJson(leaseInfo);
        Assert.NotNull(result);
        Assert.NotNull(result.RenewalInterval);
        Assert.Equal(100, result.RenewalInterval.Value.TotalSeconds);
        Assert.NotNull(result.Duration);
        Assert.Equal(200, result.Duration.Value.TotalSeconds);
        Assert.NotNull(result.RegistrationTimeUtc);
        Assert.Equal(1_457_973_741_708, DateTimeConversions.ToJavaMilliseconds(result.RegistrationTimeUtc.Value));
        Assert.NotNull(result.LastRenewalTimeUtc);
        Assert.Equal(1_457_973_741_708, DateTimeConversions.ToJavaMilliseconds(result.LastRenewalTimeUtc.Value));
        Assert.NotNull(result.EvictionTimeUtc);
        Assert.Equal(1_457_973_741_708, DateTimeConversions.ToJavaMilliseconds(result.EvictionTimeUtc.Value));
        Assert.NotNull(result.ServiceUpTimeUtc);
        Assert.Equal(1_457_973_741_708, DateTimeConversions.ToJavaMilliseconds(result.ServiceUpTimeUtc.Value));
    }

    [Fact]
    public void FromJson_LastRenewalTimestampLegacy_Correct()
    {
        var leaseInfo = new JsonLeaseInfo
        {
            LastRenewalTimestampLegacy = 1_457_973_741_708
        };

        LeaseInfo? result = LeaseInfo.FromJson(leaseInfo);
        Assert.NotNull(result);
        Assert.NotNull(result.LastRenewalTimeUtc);
        Assert.Equal(1_457_973_741_708, DateTimeConversions.ToJavaMilliseconds(result.LastRenewalTimeUtc.Value));
    }

    [Fact]
    public void FromConfiguration_Correct()
    {
        var instanceOptions = new EurekaInstanceOptions();
        LeaseInfo info = LeaseInfo.FromConfiguration(instanceOptions);
        Assert.NotNull(info.RenewalInterval);
        Assert.Equal(instanceOptions.LeaseRenewalIntervalInSeconds, info.RenewalInterval.Value.TotalSeconds);
        Assert.NotNull(info.Duration);
        Assert.Equal(instanceOptions.LeaseExpirationDurationInSeconds, info.Duration.Value.TotalSeconds);
    }
}
