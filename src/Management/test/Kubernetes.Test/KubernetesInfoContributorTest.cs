// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Steeltoe.Common.Kubernetes;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Info;
using Xunit;

namespace Steeltoe.Management.Kubernetes.Test;

public sealed class KubernetesInfoContributorTest
{
    [Fact]
    public async Task ReturnsPodInfoInsideCluster()
    {
        var pod = new V1Pod
        {
            Metadata = new V1ObjectMeta
            {
                NamespaceProperty = "mynamespace",
                Name = "mypod"
            },
            Status = new V1PodStatus
            {
                PodIP = "mypodip",
                HostIP = "myhostip"
            },
            Spec = new V1PodSpec
            {
                ServiceAccountName = "myserviceaccount",
                NodeName = "mynode"
            }
        };

        using var scope = new EnvironmentVariableScope("HOSTNAME", "mypod");
        IKubernetes kubernetes = CreateKubernetesMock(pod);
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();
        var podUtilities = new PodUtilities(new KubernetesApplicationOptions(configurationRoot), kubernetes, NullLogger<PodUtilities>.Instance);
        var contributor = new KubernetesInfoContributor(podUtilities);

        var builder = new InfoBuilder();
        await contributor.ContributeAsync(builder, CancellationToken.None);
        IDictionary<string, object> infos = builder.Build();

        var info = (Dictionary<string, object>)infos["kubernetes"];
        info.Should().NotBeNull();
        info.Should().ContainKey("inside").WhoseValue.As<bool>().Should().BeTrue();
        info.Should().ContainKey("namespace").WhoseValue.As<string>().Should().Be("mynamespace");
        info.Should().ContainKey("podName").WhoseValue.As<string>().Should().Be("mypod");
        info.Should().ContainKey("podIp").WhoseValue.As<string>().Should().Be("mypodip");
        info.Should().ContainKey("serviceAccount").WhoseValue.As<string>().Should().Be("myserviceaccount");
        info.Should().ContainKey("nodeName").WhoseValue.As<string>().Should().Be("mynode");
        info.Should().ContainKey("hostIp").WhoseValue.As<string>().Should().Be("myhostip");
    }

    [Fact]
    public async Task ReturnsNoPodInfoOutsideCluster()
    {
        IKubernetes kubernetes = CreateKubernetesMock(null);
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();
        var podUtilities = new PodUtilities(new KubernetesApplicationOptions(configurationRoot), kubernetes, NullLogger<PodUtilities>.Instance);
        var contributor = new KubernetesInfoContributor(podUtilities);

        var builder = new InfoBuilder();
        await contributor.ContributeAsync(builder, CancellationToken.None);
        IDictionary<string, object> infos = builder.Build();

        var info = (Dictionary<string, object>)infos["kubernetes"];
        info.Should().NotBeNull();
        info.Keys.Should().HaveCount(1);
        info.Should().ContainKey("inside").WhoseValue.As<bool>().Should().BeFalse();
    }

    private static IKubernetes CreateKubernetesMock(V1Pod? pod)
    {
        var response = new HttpOperationResponse<V1PodList>
        {
            Body = pod != null
                ? new V1PodList(new List<V1Pod>
                {
                    pod
                })
                : new V1PodList()
        };

        var coreMock = new Mock<ICoreV1Operations>();

        coreMock.Setup(operations => operations.ListNamespacedPodWithHttpMessagesAsync(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<bool?>(),
            It.IsAny<bool?>(), It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var kubernetesMock = new Mock<IKubernetes>();
        kubernetesMock.Setup(kubernetes => kubernetes.CoreV1).Returns(coreMock.Object);
        return kubernetesMock.Object;
    }
}
