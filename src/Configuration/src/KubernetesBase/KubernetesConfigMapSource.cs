// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using Microsoft.Extensions.Configuration;

namespace Steeltoe.Extensions.Configuration.Kubernetes;

internal sealed class KubernetesConfigMapSource : IConfigurationSource
{
    private IKubernetes KubernetesClient { get; }

    private KubernetesConfigSourceSettings ConfigSettings { get; }

    internal KubernetesConfigMapSource(IKubernetes kubernetesClient, KubernetesConfigSourceSettings settings)
    {
        KubernetesClient = kubernetesClient;
        ConfigSettings = settings;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new KubernetesConfigMapProvider(KubernetesClient, ConfigSettings);
    }
}
