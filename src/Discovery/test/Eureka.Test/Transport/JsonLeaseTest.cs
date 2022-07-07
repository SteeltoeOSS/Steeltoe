// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.Test;
using System.Text.Json;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Transport.Test;

public class JsonLeaseTest : AbstractBaseTest
{
    [Fact]
    public void Deserialize_GoodJson()
    {
        var json = @"
{   
    ""renewalIntervalInSecs"":30,
    ""durationInSecs"":90,
    ""registrationTimestamp"":1457714988223,
    ""lastRenewalTimestamp"":1457716158319,
    ""evictionTimestamp"":0,
    ""serviceUpTimestamp"":1457714988223
}";
        var leaseInfo = JsonSerializer.Deserialize<JsonLeaseInfo>(json);
        Assert.NotNull(leaseInfo);
        Assert.Equal(30, leaseInfo.RenewalIntervalInSecs);
        Assert.Equal(90, leaseInfo.DurationInSecs);
        Assert.Equal(1_457_714_988_223, leaseInfo.RegistrationTimestamp);
        Assert.Equal(1_457_716_158_319, leaseInfo.LastRenewalTimestamp);
        Assert.Equal(0, leaseInfo.EvictionTimestamp);
        Assert.Equal(1_457_714_988_223, leaseInfo.ServiceUpTimestamp);
    }
}
