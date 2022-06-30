// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER
using System;
using k8s;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Extensions.Configuration.Kubernetes;

public static partial class KubernetesHostBuilderExtensions
{
    /// <summary>
    /// Add Kubernetes Configuration Providers for configmaps and secrets.
    /// </summary>
    /// <param name="applicationBuilder">Your <see cref="WebApplicationBuilder"/>.</param>
    /// <param name="kubernetesClientConfiguration">Customize the <see cref="KubernetesClientConfiguration"/>.</param>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/>.</param>
    public static WebApplicationBuilder AddKubernetesConfiguration(this WebApplicationBuilder applicationBuilder, Action<KubernetesClientConfiguration> kubernetesClientConfiguration = null, ILoggerFactory loggerFactory = null)
    {
        applicationBuilder.Configuration.AddKubernetes(kubernetesClientConfiguration, loggerFactory);
        applicationBuilder.Services.AddKubernetesConfigurationServices();
        return applicationBuilder;
    }
}
#endif
