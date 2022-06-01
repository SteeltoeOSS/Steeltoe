// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using Microsoft.Extensions.Configuration;

namespace Steeltoe.Extensions.Configuration.Kubernetes;

internal class KubernetesConfigMapSource : IConfigurationSource
{
    private IKubernetes K8sClient { get; set; }

    private KubernetesConfigSourceSettings ConfigSettings { get; set; }

    internal KubernetesConfigMapSource(IKubernetes kubernetesClient, KubernetesConfigSourceSettings settings)
    {
        K8sClient = kubernetesClient;
        ConfigSettings = settings;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder) => new KubernetesConfigMapProvider(K8sClient, ConfigSettings);
}
