// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common;
using Steeltoe.Common.Kubernetes;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Info;

namespace Steeltoe.Management.Kubernetes;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add an IInfoContributor that reports basic Kubernetes pod and host information.
    /// </summary>
    /// <param name="services">
    /// <see cref="IServiceCollection" />.
    /// </param>
    public static IServiceCollection AddKubernetesInfoContributor(this IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);

        services.AddKubernetesClient();
        services.TryAddSingleton<PodUtilities>();
        services.AddSingleton<IInfoContributor, KubernetesInfoContributor>();

        return services;
    }

    /// <summary>
    /// Add actuators that are useful when running in Kubernetes.
    /// </summary>
    /// <param name="services">
    /// <see cref="IServiceCollection" />.
    /// </param>
    public static IServiceCollection AddKubernetesActuators(this IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);

        services.AddKubernetesInfoContributor();
        services.AddAllActuators();

        return services;
    }
}
