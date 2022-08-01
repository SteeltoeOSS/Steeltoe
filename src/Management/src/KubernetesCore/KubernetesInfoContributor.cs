// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Kubernetes;
using Steeltoe.Management.Info;

namespace Steeltoe.Management.Kubernetes;

public class KubernetesInfoContributor : IInfoContributor
{
    private readonly IPodUtilities _podUtilities;

    public KubernetesInfoContributor(IPodUtilities podUtilities)
    {
        _podUtilities = podUtilities;
    }

    public void Contribute(IInfoBuilder builder)
    {
        var current = _podUtilities.GetCurrentPodAsync().GetAwaiter().GetResult();
        var details = new Dictionary<string, object>();
        if (current != null)
        {
            details.Add("inside", true);
            details.Add("namespace", current.Metadata.NamespaceProperty);
            details.Add("podName", current.Metadata.Name);
            details.Add("podIp", current.Status.PodIP);
            details.Add("serviceAccount", current.Spec.ServiceAccountName);
            details.Add("nodeName", current.Spec.NodeName);
            details.Add("hostIp", current.Status.HostIP);
        }
        else
        {
            details.Add("inside", false);
        }

        builder.WithInfo("kubernetes", details);
    }
}
