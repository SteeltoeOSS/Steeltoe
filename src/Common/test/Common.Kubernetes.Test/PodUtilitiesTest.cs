// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;
using Steeltoe.Common.TestResources;
using Xunit;

namespace Steeltoe.Common.Kubernetes.Test;

public sealed class PodUtilitiesTest
{
    [Fact]
    public async Task ReturnsNullOnFailureToGetCurrentPod()
    {
        var mockHttpMessageHandler = new MockHttpMessageHandler();
        mockHttpMessageHandler.Expect(HttpMethod.Get, "*").Respond(HttpStatusCode.NotFound);
        using var delegatingHandler = new HttpClientDelegatingHandler(mockHttpMessageHandler.ToHttpClient());
        var appOptions = new KubernetesApplicationOptions(new ConfigurationBuilder().Build());

        using var client = new k8s.Kubernetes(new KubernetesClientConfiguration
        {
            Host = "http://localhost"
        }, delegatingHandler);

        var utilities = new PodUtilities(appOptions, client, NullLogger<PodUtilities>.Instance);

        V1Pod pod = await utilities.GetCurrentPodAsync(CancellationToken.None);

        Assert.Null(pod);
    }

    [Fact]
    public async Task ReturnsPodOnSuccessGettingPod()
    {
        const string hostname = "kubernetes1-7d7c8ff84f-76mkq";
        using var scope = new EnvironmentVariableScope("HOSTNAME", hostname);

        const string podListRsp =
            "{\"kind\":\"PodList\",\"apiVersion\":\"v1\",\"metadata\":{\"selfLink\":\"/api/v1/namespaces/default/pods\",\"resourceVersion\":\"8541208\"},\"items\":[{\"metadata\":{\"name\":\"kubernetes1-7d7c8ff84f-76mkq\",\"generateName\":\"kubernetes1-7d7c8ff84f-\",\"namespace\":\"default\",\"selfLink\":\"/api/v1/namespaces/default/pods/kubernetes1-7d7c8ff84f-76mkq\",\"uid\":\"989ef5d3-c2bd-4179-a1ab-9925e15d202a\",\"resourceVersion\":\"8523457\",\"creationTimestamp\":\"2020-05-28T18:32:37Z\",\"labels\":{ \"app\":\"kubernetes1\"},\"annotations\":{\"buildID\":\"\"},\"ownerReferences\":[],\"containers\":[{\"name\":\"kubernetes1\",\"image\":\"kubernetes1:x01725c8eb04d86fc\",\"command\":[],\"ports\":[],\"resources\":{},\"volumeMounts\":[],\"terminationMessagePath\":\"/dev/termination-log\",\"terminationMessagePolicy\":\"File\",\"imagePullPolicy\":\"Never\"}],\"restartPolicy\":\"Always\",\"terminationGracePeriodSeconds\":0,\"dnsPolicy\":\"ClusterFirst\",\"serviceAccountName\":\"default\",\"serviceAccount\":\"default\",\"nodeName\":\"agentpool\",\"securityContext\":{},\"schedulerName\":\"default-scheduler\",\"tolerations\":[{\"key\":\"node.kubernetes.io/not-ready\",\"operator\":\"Exists\",\"effect\":\"NoExecute\",\"tolerationSeconds\":300},{\"key\":\"node.kubernetes.io/unreachable\",\"operator\":\"Exists\",\"effect\":\"NoExecute\",\"tolerationSeconds\":300}],\"priority\":0,\"enableServiceLinks\":true},\"spec\":{\"serviceAccountName\": \"default\", \"serviceAccount\": \"default\", \"nodeName\": \"agentpool\"},\"status\":{\"phase\":\"Running\",\"conditions\":[{\"type\":\"Initialized\",\"status\":\"True\",\"lastProbeTime\":null,\"lastTransitionTime\":\"2020-05-28T18:33:19Z\"},{\"type\":\"Ready\",\"status\":\"True\",\"lastProbeTime\":null,\"lastTransitionTime\":\"2020-05-28T18:33:20Z\"},{\"type\":\"ContainersReady\",\"status\":\"True\",\"lastProbeTime\":null,\"lastTransitionTime\":\"2020-05-28T18:33:20Z\"},{\"type\":\"PodScheduled\",\"status\":\"True\",\"lastProbeTime\":null,\"lastTransitionTime\":\"2020-05-28T18:32:37Z\"}],\"hostIP\":\"10.240.0.4\",\"podIP\":\"10.244.1.128\",\"podIPs\":[{\"ip\":\"10.244.1.128\"}],\"startTime\":\"2020-05-28T18:32:37Z\",\"initContainerStatuses\":[],\"containerStatuses\":[],\"qosClass\":\"BestEffort\"}}]}\n";

        var mockHttpMessageHandler = new MockHttpMessageHandler();
        mockHttpMessageHandler.Expect(HttpMethod.Get, "*").Respond(HttpStatusCode.OK, new StringContent(podListRsp));
        using var delegatingHandler = new HttpClientDelegatingHandler(mockHttpMessageHandler.ToHttpClient());

        var appOptions = new KubernetesApplicationOptions(new ConfigurationBuilder().Build());

        using var client = new k8s.Kubernetes(new KubernetesClientConfiguration
        {
            Host = "http://localhost"
        }, delegatingHandler);

        var utilities = new PodUtilities(appOptions, client, NullLogger<PodUtilities>.Instance);

        V1Pod pod = await utilities.GetCurrentPodAsync(CancellationToken.None);

        Assert.NotNull(pod);
        Assert.Equal("default", pod.Metadata.NamespaceProperty);
        Assert.Equal(hostname, pod.Metadata.Name);
        Assert.Equal("10.244.1.128", pod.Status.PodIP);
        Assert.Equal("default", pod.Spec.ServiceAccountName);
        Assert.Equal("agentpool", pod.Spec.NodeName);
        Assert.Equal("10.240.0.4", pod.Status.HostIP);
    }
}
