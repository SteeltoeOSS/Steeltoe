// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Info.Contributor;
using Steeltoe.Management.Info;

namespace Steeltoe.Management.Endpoint.Test.Info.Contributor;

public sealed class BuildInfoContributorTest
{
    [Fact]
    public void BuildAddsVersionInfo()
    {
        var contributor = new BuildInfoContributor();
        var builder = new InfoBuilder();

        contributor.ContributeAsync(builder, CancellationToken.None);
        IDictionary<string, object> results = builder.Build();

        Assert.True(results.ContainsKey("applicationVersionInfo"));
        Assert.NotNull(results["applicationVersionInfo"]);
        Assert.True(results.ContainsKey("steeltoeVersionInfo"));
        Assert.NotNull(results["steeltoeVersionInfo"]);
    }
}
