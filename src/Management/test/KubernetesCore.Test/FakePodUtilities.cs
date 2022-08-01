// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s.Models;
using Steeltoe.Common.Kubernetes;

namespace Steeltoe.Management.Kubernetes.Test;

internal sealed class FakePodUtilities : IPodUtilities
{
    internal static V1Pod SamplePod { get; } = new ()
    {
        Metadata = new V1ObjectMeta { NamespaceProperty = "mynamespace", Name = "mypod" },
        Status = new V1PodStatus { PodIP = "mypodip", HostIP = "myhostip" },
        Spec = new V1PodSpec { ServiceAccountName = "myserviceaccount", NodeName = "mynode" }
    };

    private readonly V1Pod _fakePod;

    internal FakePodUtilities(V1Pod v1Pod)
    {
        _fakePod = v1Pod;
    }

    public Task<V1Pod> GetCurrentPodAsync()
    {
        return Task.FromResult(_fakePod);
    }
}
