// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using Microsoft.Extensions.Configuration;

namespace Steeltoe.Extensions.Configuration.Kubernetes;

internal sealed class KubernetesSecretSource : IConfigurationSource
{
    private IKubernetes KubernetesClient { get; }

    private KubernetesConfigSourceSettings ConfigurationSettings { get; }

    private CancellationToken CancelToken { get; }

    internal KubernetesSecretSource(IKubernetes kubernetesClient, KubernetesConfigSourceSettings settings, CancellationToken cancellationToken = default)
    {
        KubernetesClient = kubernetesClient;
        ConfigurationSettings = settings;
        CancelToken = cancellationToken;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new KubernetesSecretProvider(KubernetesClient, ConfigurationSettings, CancelToken);
    }
}
