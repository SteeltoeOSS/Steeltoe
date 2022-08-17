// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Discovery.Kubernetes.Discovery;

namespace Steeltoe.Discovery.Kubernetes;

public static class KubernetesDiscoveryClientFactory
{
    public static IDiscoveryClient CreateClient(IOptionsMonitor<KubernetesDiscoveryOptions> options, IKubernetes kubernetes)
    {
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(kubernetes);

        var isServicePortSecureResolver = new DefaultIsServicePortSecureResolver(options.CurrentValue);

        return new KubernetesDiscoveryClient(isServicePortSecureResolver, kubernetes, options);
    }
}
