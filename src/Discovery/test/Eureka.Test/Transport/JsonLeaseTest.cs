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

        leaseInfo.Should().NotBeNull();
        leaseInfo.RenewalIntervalInSeconds.Should().Be(30);
        leaseInfo.DurationInSeconds.Should().Be(90);
        leaseInfo.RegistrationTimestamp.Should().Be(1_457_714_988_223);
        leaseInfo.LastRenewalTimestamp.Should().Be(1_457_716_158_319);
        leaseInfo.EvictionTimestamp.Should().Be(0);
        leaseInfo.ServiceUpTimestamp.Should().Be(1_457_714_988_223);
    }
}
