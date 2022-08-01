// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using Microsoft.Extensions.Configuration;
using RichardSzalay.MockHttp;
using Steeltoe.Extensions.Configuration.Kubernetes;
using System.Net;
using Xunit;

namespace Steeltoe.Common.Kubernetes.Test;

public class StandardPodUtilitiesTest
{
    [Fact]
    public void ConstructorThrowsOnUll()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new StandardPodUtilities(null));
        Assert.Equal("kubernetesApplicationOptions", ex.ParamName);
    }

    [Fact]
    public async Task ReturnsNullOnFailureToGetCurrentPod()
    {
        var mockHttpMessageHandler = new MockHttpMessageHandler();
        mockHttpMessageHandler.Expect(HttpMethod.Get, "*").Respond(HttpStatusCode.NotFound);
        var appOptions = new KubernetesApplicationOptions(new ConfigurationBuilder().Build());
        using var client = new k8s.Kubernetes(new KubernetesClientConfiguration { Host = "http://localhost" }, httpClient: mockHttpMessageHandler.ToHttpClient());
        var utils = new StandardPodUtilities(appOptions, null, client);

        var getPodResult = await utils.GetCurrentPodAsync();

        Assert.Null(getPodResult);
    }

    [Fact]
    public async Task ReturnsPodOnSuccessGettingPod()
    {
        var hostname = "kubernetes1-7d7c8ff84f-76mkq";
        Environment.SetEnvironmentVariable("HOSTNAME", hostname);
        var podListRsp = "{\"kind\":\"PodList\",\"apiVersion\":\"v1\",\"metadata\":{\"selfLink\":\"/api/v1/namespaces/default/pods\",\"resourceVersion\":\"8541208\"},\"items\":[{\"metadata\":{\"name\":\"kubernetes1-7d7c8ff84f-76mkq\",\"generateName\":\"kubernetes1-7d7c8ff84f-\",\"namespace\":\"default\",\"selfLink\":\"/api/v1/namespaces/default/pods/kubernetes1-7d7c8ff84f-76mkq\",\"uid\":\"989ef5d3-c2bd-4179-a1ab-9925e15d202a\",\"resourceVersion\":\"8523457\",\"creationTimestamp\":\"2020-05-28T18:32:37Z\",\"labels\":{ \"app\":\"kubernetes1\"},\"annotations\":{\"buildID\":\"\"},\"ownerReferences\":[],\"containers\":[{\"name\":\"kubernetes1\",\"image\":\"kubernetes1:x01725c8eb04d86fc\",\"command\":[],\"ports\":[],\"resources\":{},\"volumeMounts\":[],\"terminationMessagePath\":\"/dev/termination-log\",\"terminationMessagePolicy\":\"File\",\"imagePullPolicy\":\"Never\"}],\"restartPolicy\":\"Always\",\"terminationGracePeriodSeconds\":0,\"dnsPolicy\":\"ClusterFirst\",\"serviceAccountName\":\"default\",\"serviceAccount\":\"default\",\"nodeName\":\"agentpool\",\"securityContext\":{},\"schedulerName\":\"default-scheduler\",\"tolerations\":[{\"key\":\"node.kubernetes.io/not-ready\",\"operator\":\"Exists\",\"effect\":\"NoExecute\",\"tolerationSeconds\":300},{\"key\":\"node.kubernetes.io/unreachable\",\"operator\":\"Exists\",\"effect\":\"NoExecute\",\"tolerationSeconds\":300}],\"priority\":0,\"enableServiceLinks\":true},\"spec\":{\"serviceAccountName\": \"default\", \"serviceAccount\": \"default\", \"nodeName\": \"agentpool\"},\"status\":{\"phase\":\"Running\",\"conditions\":[{\"type\":\"Initialized\",\"status\":\"True\",\"lastProbeTime\":null,\"lastTransitionTime\":\"2020-05-28T18:33:19Z\"},{\"type\":\"Ready\",\"status\":\"True\",\"lastProbeTime\":null,\"lastTransitionTime\":\"2020-05-28T18:33:20Z\"},{\"type\":\"ContainersReady\",\"status\":\"True\",\"lastProbeTime\":null,\"lastTransitionTime\":\"2020-05-28T18:33:20Z\"},{\"type\":\"PodScheduled\",\"status\":\"True\",\"lastProbeTime\":null,\"lastTransitionTime\":\"2020-05-28T18:32:37Z\"}],\"hostIP\":\"10.240.0.4\",\"podIP\":\"10.244.1.128\",\"podIPs\":[{\"ip\":\"10.244.1.128\"}],\"startTime\":\"2020-05-28T18:32:37Z\",\"initContainerStatuses\":[],\"containerStatuses\":[],\"qosClass\":\"BestEffort\"}}]}\n";
        var mockHttpMessageHandler = new MockHttpMessageHandler();
        mockHttpMessageHandler.Expect(HttpMethod.Get, "*").Respond(HttpStatusCode.OK, new StringContent(podListRsp));

        var appOptions = new KubernetesApplicationOptions(new ConfigurationBuilder().Build());
        using var client = new k8s.Kubernetes(new KubernetesClientConfiguration { Host = "http://localhost" }, httpClient: mockHttpMessageHandler.ToHttpClient());
        var utils = new StandardPodUtilities(appOptions, null, client);

        var getPodResult = await utils.GetCurrentPodAsync();

        Assert.NotNull(getPodResult);
        Assert.Equal("default", getPodResult.Metadata.NamespaceProperty);
        Assert.Equal(hostname, getPodResult.Metadata.Name);
        Assert.Equal("10.244.1.128", getPodResult.Status.PodIP);
        Assert.Equal("default", getPodResult.Spec.ServiceAccountName);
        Assert.Equal("agentpool", getPodResult.Spec.NodeName);
        Assert.Equal("10.240.0.4", getPodResult.Status.HostIP);
        Environment.SetEnvironmentVariable("HOSTNAME", null);
    }
}
