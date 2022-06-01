// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using Microsoft.Extensions.Configuration;
using System.Threading;

namespace Steeltoe.Extensions.Configuration.Kubernetes;

internal class KubernetesSecretSource : IConfigurationSource
{
    private IKubernetes K8sClient { get; set; }

    private KubernetesConfigSourceSettings ConfigSettings { get; set; }

    private CancellationToken CancelToken { get; set; }

    internal KubernetesSecretSource(IKubernetes kubernetesClient, KubernetesConfigSourceSettings settings, CancellationToken cancellationToken = default)
    {
        K8sClient = kubernetesClient;
        ConfigSettings = settings;
        CancelToken = cancellationToken;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder) => new KubernetesSecretProvider(K8sClient, ConfigSettings, CancelToken);
}
