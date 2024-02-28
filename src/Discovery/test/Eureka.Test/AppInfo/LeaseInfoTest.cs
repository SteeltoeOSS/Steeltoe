// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Transport;
using Steeltoe.Discovery.Eureka.Util;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test.AppInfo;

public sealed class LeaseInfoTest : AbstractBaseTest
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

        LeaseInfo result = LeaseInfo.FromJson(leaseInfo);
        Assert.Equal(100, result.RenewalInterval.TotalSeconds);
        Assert.Equal(200, result.Duration.TotalSeconds);
        Assert.Equal(1_457_973_741_708, DateTimeConversions.ToJavaMilliseconds(result.RegistrationTimeUtc));
        Assert.Equal(1_457_973_741_708, DateTimeConversions.ToJavaMilliseconds(result.LastRenewalTimeUtc));
        Assert.Equal(1_457_973_741_708, DateTimeConversions.ToJavaMilliseconds(result.EvictionTimeUtc));
        Assert.Equal(1_457_973_741_708, DateTimeConversions.ToJavaMilliseconds(result.ServiceUpTimeUtc));
    }

    [Fact]
    public void FromJson_LastRenewalTimestampLegacy_Correct()
    {
        var leaseInfo = new JsonLeaseInfo
        {
            RenewalIntervalInSeconds = 100,
            DurationInSeconds = 200,
            RegistrationTimestamp = 1_457_973_741_708,
            LastRenewalTimestampLegacy = 1_457_973_741_708,
            EvictionTimestamp = 1_457_973_741_708,
            ServiceUpTimestamp = 1_457_973_741_708
        };

        LeaseInfo result = LeaseInfo.FromJson(leaseInfo);
        Assert.Equal(100, result.RenewalInterval.TotalSeconds);
        Assert.Equal(200, result.Duration.TotalSeconds);
        Assert.Equal(1_457_973_741_708, DateTimeConversions.ToJavaMilliseconds(result.RegistrationTimeUtc));
        Assert.Equal(1_457_973_741_708, DateTimeConversions.ToJavaMilliseconds(result.LastRenewalTimeUtc));
        Assert.Equal(1_457_973_741_708, DateTimeConversions.ToJavaMilliseconds(result.EvictionTimeUtc));
        Assert.Equal(1_457_973_741_708, DateTimeConversions.ToJavaMilliseconds(result.ServiceUpTimeUtc));
    }

    [Fact]
    public void FromConfig_Correct()
    {
        var instanceOptions = new EurekaInstanceOptions();
        LeaseInfo info = LeaseInfo.FromConfiguration(instanceOptions);
        Assert.Equal(instanceOptions.LeaseRenewalIntervalInSeconds, info.RenewalInterval.TotalSeconds);
        Assert.Equal(instanceOptions.LeaseExpirationDurationInSeconds, info.Duration.TotalSeconds);
    }
}
