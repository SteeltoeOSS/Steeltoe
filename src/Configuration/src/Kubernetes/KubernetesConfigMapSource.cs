// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

namespace Steeltoe.Configuration.Kubernetes;

internal sealed class KubernetesConfigMapSource : IConfigurationSource
{
    private readonly IKubernetes _kubernetesClient;
    private readonly KubernetesConfigSourceSettings _settings;

    public KubernetesConfigMapSource(IKubernetes kubernetesClient, KubernetesConfigSourceSettings settings)
    {
        ArgumentGuard.NotNull(kubernetesClient);
        ArgumentGuard.NotNull(settings);

        _kubernetesClient = kubernetesClient;
        _settings = settings;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new KubernetesConfigMapProvider(_kubernetesClient, _settings, CancellationToken.None);
    }
}
