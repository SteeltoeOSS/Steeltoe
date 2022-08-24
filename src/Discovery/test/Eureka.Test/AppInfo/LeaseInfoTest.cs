// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.Test;
using Steeltoe.Discovery.Eureka.Transport;
using Steeltoe.Discovery.Eureka.Util;
using Xunit;

namespace Steeltoe.Discovery.Eureka.AppInfo.Test;

public class LeaseInfoTest : AbstractBaseTest
{
    [Fact]
    public void Constructor_Defaults()
    {
        var info = new LeaseInfo();
        Assert.Equal(LeaseInfo.DefaultDurationInSecs, info.DurationInSecs);
        Assert.Equal(LeaseInfo.DefaultRenewalIntervalInSecs, info.RenewalIntervalInSecs);
    }

    [Fact]
    public void FromJson_Correct()
    {
        var leaseInfo = new JsonLeaseInfo
        {
            RenewalIntervalInSecs = 100,
            DurationInSecs = 200,
            RegistrationTimestamp = 1_457_973_741_708,
            LastRenewalTimestamp = 1_457_973_741_708,
            LastRenewalTimestampLegacy = 1_457_973_741_708,
            EvictionTimestamp = 1_457_973_741_708,
            ServiceUpTimestamp = 1_457_973_741_708
        };

        LeaseInfo result = LeaseInfo.FromJson(leaseInfo);
        Assert.Equal(100, result.RenewalIntervalInSecs);
        Assert.Equal(200, result.DurationInSecs);
        Assert.Equal(1_457_973_741_708, DateTimeConversions.ToJavaMillis(new DateTime(result.RegistrationTimestamp, DateTimeKind.Utc)));
        Assert.Equal(1_457_973_741_708, DateTimeConversions.ToJavaMillis(new DateTime(result.LastRenewalTimestamp, DateTimeKind.Utc)));
        Assert.Equal(1_457_973_741_708, DateTimeConversions.ToJavaMillis(new DateTime(result.LastRenewalTimestampLegacy, DateTimeKind.Utc)));
        Assert.Equal(1_457_973_741_708, DateTimeConversions.ToJavaMillis(new DateTime(result.EvictionTimestamp, DateTimeKind.Utc)));
        Assert.Equal(1_457_973_741_708, DateTimeConversions.ToJavaMillis(new DateTime(result.ServiceUpTimestamp, DateTimeKind.Utc)));
    }

    [Fact]
    public void FromConfig_Correct()
    {
        var configuration = new EurekaInstanceConfiguration();
        LeaseInfo info = LeaseInfo.FromConfig(configuration);
        Assert.Equal(configuration.LeaseRenewalIntervalInSeconds, info.RenewalIntervalInSecs);
        Assert.Equal(configuration.LeaseExpirationDurationInSeconds, info.DurationInSecs);
    }
}
