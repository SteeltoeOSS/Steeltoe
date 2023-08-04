// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Health.Contributor;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Health.Contributor;

public sealed class DiskSpaceContributorTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        IOptionsMonitor<DiskSpaceContributorOptions> optionsMonitor = GetOptionsMonitorFromSettings<DiskSpaceContributorOptions>();
        var contributor = new DiskSpaceContributor(optionsMonitor);
        Assert.Equal("diskSpace", contributor.Id);
    }

    [Fact]
    public void Health_InitializedWithDefaults_ReturnsExpected()
    {
        IOptionsMonitor<DiskSpaceContributorOptions> optionsMonitor = GetOptionsMonitorFromSettings<DiskSpaceContributorOptions>();
        var contributor = new DiskSpaceContributor(optionsMonitor);
        Assert.Equal("diskSpace", contributor.Id);
        HealthCheckResult? result = contributor.Health();
        Assert.NotNull(result);
        Assert.Equal(HealthStatus.Up, result.Status);
        Assert.NotNull(result.Details);
        Assert.True(result.Details.ContainsKey("total"));
        Assert.True(result.Details.ContainsKey("free"));
        Assert.True(result.Details.ContainsKey("threshold"));
        Assert.True(result.Details.ContainsKey("status"));
    }
}
