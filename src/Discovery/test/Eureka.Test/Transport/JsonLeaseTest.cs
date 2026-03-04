// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Eureka.Transport;

namespace Steeltoe.Discovery.Eureka.Test.Transport;

public sealed class JsonLeaseTest
{
    [Fact]
    public void Serialize()
    {
        var leaseInfo = new JsonLeaseInfo
        {
            RenewalIntervalInSeconds = 30,
            DurationInSeconds = 90,
            RegistrationTimestamp = 1_457_714_988_223,
            LastRenewalTimestamp = 1_457_716_158_319,
            EvictionTimestamp = 1_457_715_134_123,
            ServiceUpTimestamp = 1_457_714_988_223
        };

        string result = JsonSerializer.Serialize(leaseInfo, EurekaClient.RequestSerializerOptions);

        result.Should().BeJson("""
            {
              "renewalIntervalInSecs": 30,
              "durationInSecs": 90,
              "registrationTimestamp": "1457714988223",
              "lastRenewalTimestamp": "1457716158319",
              "evictionTimestamp": "1457715134123",
              "serviceUpTimestamp": "1457714988223"
            }
            """);
    }

    [Fact]
    public void Deserialize()
    {
        const string json = """
            {
              "renewalIntervalInSecs": 30,
              "durationInSecs": 90,
              "registrationTimestamp": 1457714988223,
              "lastRenewalTimestamp": 1457716158319,
              "evictionTimestamp": 1457715134123,
              "serviceUpTimestamp": 1457714988223
            }
            """;

        var result = JsonSerializer.Deserialize<JsonLeaseInfo>(json, EurekaClient.ResponseSerializerOptions);

        result.Should().NotBeNull();
        result.RenewalIntervalInSeconds.Should().Be(30);
        result.DurationInSeconds.Should().Be(90);
        result.RegistrationTimestamp.Should().Be(1_457_714_988_223);
        result.LastRenewalTimestamp.Should().Be(1_457_716_158_319);
        result.EvictionTimestamp.Should().Be(1_457_715_134_123);
        result.ServiceUpTimestamp.Should().Be(1_457_714_988_223);
    }
}
