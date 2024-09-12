// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Steeltoe.Discovery.Eureka.Transport;

namespace Steeltoe.Discovery.Eureka.Test.Transport;

public sealed class JsonLeaseTest
{
    [Fact]
    public void Deserialize_GoodJson()
    {
        const string json = """
            {
              "renewalIntervalInSecs": 30,
              "durationInSecs": 90,
              "registrationTimestamp": 1457714988223,
              "lastRenewalTimestamp": 1457716158319,
              "evictionTimestamp": 0,
              "serviceUpTimestamp": 1457714988223
            }
            """;

        var leaseInfo = JsonSerializer.Deserialize<JsonLeaseInfo>(json);
        Assert.NotNull(leaseInfo);
        Assert.Equal(30, leaseInfo.RenewalIntervalInSeconds);
        Assert.Equal(90, leaseInfo.DurationInSeconds);
        Assert.Equal(1_457_714_988_223, leaseInfo.RegistrationTimestamp);
        Assert.Equal(1_457_716_158_319, leaseInfo.LastRenewalTimestamp);
        Assert.Equal(0, leaseInfo.EvictionTimestamp);
        Assert.Equal(1_457_714_988_223, leaseInfo.ServiceUpTimestamp);
    }
}
