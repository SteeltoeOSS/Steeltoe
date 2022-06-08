// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.Test;
using Steeltoe.Discovery.Eureka.Transport;
using Steeltoe.Discovery.Eureka.Util;
using System;
using Xunit;

namespace Steeltoe.Discovery.Eureka.AppInfo.Test;

public class LeaseInfoTest : AbstractBaseTest
{
    [Fact]
    public void Constructor_Defaults()
    {
        var info = new LeaseInfo();
        Assert.Equal(LeaseInfo.Default_DurationInSecs, info.DurationInSecs);
        Assert.Equal(LeaseInfo.Default_RenewalIntervalInSecs, info.RenewalIntervalInSecs);
    }

    [Fact]
    public void FromJson_Correct()
    {
        var jinfo = new JsonLeaseInfo
        {
            RenewalIntervalInSecs = 100,
            DurationInSecs = 200,
            RegistrationTimestamp = 1457973741708,
            LastRenewalTimestamp = 1457973741708,
            LastRenewalTimestampLegacy = 1457973741708,
            EvictionTimestamp = 1457973741708,
            ServiceUpTimestamp = 1457973741708
        };

        var result = LeaseInfo.FromJson(jinfo);
        Assert.Equal(100, result.RenewalIntervalInSecs);
        Assert.Equal(200, result.DurationInSecs);
        Assert.Equal(1457973741708, DateTimeConversions.ToJavaMillis(new DateTime(result.RegistrationTimestamp, DateTimeKind.Utc)));
        Assert.Equal(1457973741708, DateTimeConversions.ToJavaMillis(new DateTime(result.LastRenewalTimestamp, DateTimeKind.Utc)));
        Assert.Equal(1457973741708, DateTimeConversions.ToJavaMillis(new DateTime(result.LastRenewalTimestampLegacy, DateTimeKind.Utc)));
        Assert.Equal(1457973741708, DateTimeConversions.ToJavaMillis(new DateTime(result.EvictionTimestamp, DateTimeKind.Utc)));
        Assert.Equal(1457973741708, DateTimeConversions.ToJavaMillis(new DateTime(result.ServiceUpTimestamp, DateTimeKind.Utc)));
    }

    [Fact]
    public void ToJson_Correct()
    {
        var jinfo = new JsonLeaseInfo
        {
            RenewalIntervalInSecs = 100,
            DurationInSecs = 200,
            RegistrationTimestamp = 1457973741708,
            LastRenewalTimestamp = 1457973741708,
            LastRenewalTimestampLegacy = 1457973741708,
            EvictionTimestamp = 1457973741708,
            ServiceUpTimestamp = 1457973741708
        };

        var result = LeaseInfo.FromJson(jinfo);

        jinfo = result.ToJson();

        Assert.Equal(100, result.RenewalIntervalInSecs);
        Assert.Equal(200, result.DurationInSecs);
        Assert.Equal(1457973741708, DateTimeConversions.ToJavaMillis(new DateTime(result.RegistrationTimestamp, DateTimeKind.Utc)));
        Assert.Equal(1457973741708, DateTimeConversions.ToJavaMillis(new DateTime(result.LastRenewalTimestamp, DateTimeKind.Utc)));
        Assert.Equal(1457973741708, DateTimeConversions.ToJavaMillis(new DateTime(result.LastRenewalTimestampLegacy, DateTimeKind.Utc)));
        Assert.Equal(1457973741708, DateTimeConversions.ToJavaMillis(new DateTime(result.EvictionTimestamp, DateTimeKind.Utc)));
        Assert.Equal(1457973741708, DateTimeConversions.ToJavaMillis(new DateTime(result.ServiceUpTimestamp, DateTimeKind.Utc)));
    }

    [Fact]
    public void FromConfig_Correct()
    {
        var config = new EurekaInstanceConfig();
        var info = LeaseInfo.FromConfig(config);
        Assert.Equal(config.LeaseRenewalIntervalInSeconds, info.RenewalIntervalInSecs);
        Assert.Equal(config.LeaseExpirationDurationInSeconds, info.DurationInSecs);
    }
}
