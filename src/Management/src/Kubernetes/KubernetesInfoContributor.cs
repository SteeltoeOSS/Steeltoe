// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s.Models;
using Steeltoe.Common;
using Steeltoe.Common.Kubernetes;
using Steeltoe.Management.Info;

namespace Steeltoe.Management.Kubernetes;

internal sealed class KubernetesInfoContributor : IInfoContributor
{
    private readonly PodUtilities _podUtilities;

    public KubernetesInfoContributor(PodUtilities podUtilities)
    {
        ArgumentGuard.NotNull(podUtilities);

        _podUtilities = podUtilities;
    }

    public async Task ContributeAsync(IInfoBuilder builder, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(builder);

        V1Pod current = await _podUtilities.GetCurrentPodAsync(cancellationToken);
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
