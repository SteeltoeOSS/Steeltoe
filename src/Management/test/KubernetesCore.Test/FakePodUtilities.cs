// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s.Models;
using Steeltoe.Common.Kubernetes;
using System.Threading.Tasks;

namespace Steeltoe.Management.Kubernetes.Test;

internal class FakePodUtilities : IPodUtilities
{
    internal static V1Pod SamplePod { get; } = CreateSamplePod();

    private static V1Pod CreateSamplePod()
    {
        var metadata = new V1ObjectMeta { Name = "mypod" };
        metadata.SetNamespace("mynamespace");
        return new V1Pod
        {
            Metadata = metadata,
            Status = new V1PodStatus { PodIP = "mypodip", HostIP = "myhostip" },
            Spec = new V1PodSpec { ServiceAccountName = "myserviceaccount", NodeName = "mynode" }
        };
    }

    private readonly V1Pod fakePod;

    internal FakePodUtilities(V1Pod v1Pod)
    {
        fakePod = v1Pod;
    }

    public Task<V1Pod> GetCurrentPodAsync()
    {
        return Task.FromResult(fakePod);
    }
}