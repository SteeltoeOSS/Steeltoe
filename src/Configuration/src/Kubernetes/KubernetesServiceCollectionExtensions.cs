// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Kubernetes;

namespace Steeltoe.Extensions.Configuration.Kubernetes;

public static class KubernetesServiceCollectionExtensions
{
    /// <summary>
    /// Adds KubernetesApplicationInstanceInfo and ensures startup loggers are replaced by runtime loggers.
    /// </summary>
    /// <param name="services">
    /// Your <see cref="IServiceCollection" />.
    /// </param>
    public static IServiceCollection AddKubernetesConfigurationServices(this IServiceCollection services)
    {
        return services.AddKubernetesApplicationInstanceInfo().AddHostedService<KubernetesHostedService>();
    }
}
