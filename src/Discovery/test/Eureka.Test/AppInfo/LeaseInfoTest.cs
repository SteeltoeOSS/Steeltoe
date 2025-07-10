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

        result.Should().NotBeNull();
        result.RenewalInterval.Should().NotBeNull();
        result.RenewalInterval.Value.TotalSeconds.Should().Be(100);
        result.Duration.Should().NotBeNull();
        result.Duration.Value.TotalSeconds.Should().Be(200);
        result.RegistrationTimeUtc.Should().NotBeNull();
        DateTimeConversions.ToJavaMilliseconds(result.RegistrationTimeUtc.Value).Should().Be(1_457_973_741_708);
        result.LastRenewalTimeUtc.Should().NotBeNull();
        DateTimeConversions.ToJavaMilliseconds(result.LastRenewalTimeUtc.Value).Should().Be(1_457_973_741_708);
        result.EvictionTimeUtc.Should().NotBeNull();
        DateTimeConversions.ToJavaMilliseconds(result.EvictionTimeUtc.Value).Should().Be(1_457_973_741_708);
        result.ServiceUpTimeUtc.Should().NotBeNull();
        DateTimeConversions.ToJavaMilliseconds(result.ServiceUpTimeUtc.Value).Should().Be(1_457_973_741_708);
    }

    [Fact]
    public void FromJson_LastRenewalTimestampLegacy_Correct()
    {
        var leaseInfo = new JsonLeaseInfo
        {
            LastRenewalTimestampLegacy = 1_457_973_741_708
        };

        LeaseInfo? result = LeaseInfo.FromJson(leaseInfo);

        result.Should().NotBeNull();
        result.LastRenewalTimeUtc.Should().NotBeNull();
        DateTimeConversions.ToJavaMilliseconds(result.LastRenewalTimeUtc.Value).Should().Be(1_457_973_741_708);
    }

    [Fact]
    public void FromConfiguration_Correct()
    {
        var instanceOptions = new EurekaInstanceOptions();
        LeaseInfo info = LeaseInfo.FromConfiguration(instanceOptions);

        info.RenewalInterval.Should().NotBeNull();
        info.RenewalInterval.Value.TotalSeconds.Should().Be(instanceOptions.LeaseRenewalIntervalInSeconds);
        info.Duration.Should().NotBeNull();
        info.Duration.Value.TotalSeconds.Should().Be(instanceOptions.LeaseExpirationDurationInSeconds);
    }
}
