// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Info;
using Xunit;

namespace Steeltoe.Management.Kubernetes.Test;

public class KubernetesInfoContributorTest
{
    [Fact]
    public async Task ReturnsPodInfoInsideCluster()
    {
        var builder = new InfoBuilder();
        var contributor = new KubernetesInfoContributor(new FakePodUtilities(FakePodUtilities.SamplePod));

        await contributor.ContributeAsync(builder, CancellationToken.None);
        var info = builder.Build()["kubernetes"] as Dictionary<string, object>;

        Assert.True(bool.Parse(info["inside"].ToString()));
        Assert.Equal("mynamespace", info["namespace"].ToString());
        Assert.Equal("mypod", info["podName"].ToString());
        Assert.Equal("mypodip", info["podIp"].ToString());
        Assert.Equal("myserviceaccount", info["serviceAccount"].ToString());
        Assert.Equal("mynode", info["nodeName"].ToString());
        Assert.Equal("myhostip", info["hostIp"].ToString());
    }

    [Fact]
    public async Task ReturnsNoPodInfoOutsideCluster()
    {
        var builder = new InfoBuilder();
        var contributor = new KubernetesInfoContributor(new FakePodUtilities(null));

        await contributor.ContributeAsync(builder, CancellationToken.None);
        var info = builder.Build()["kubernetes"] as Dictionary<string, object>;

        Assert.True(info.ContainsKey("inside"));
        Assert.False(bool.Parse(info["inside"].ToString()));
        Assert.False(info.ContainsKey("namespace"));
        Assert.False(info.ContainsKey("podName"));
        Assert.False(info.ContainsKey("podIp"));
        Assert.False(info.ContainsKey("serviceAccount"));
        Assert.False(info.ContainsKey("nodeName"));
        Assert.False(info.ContainsKey("hostIp"));
    }
}
