// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.Kubernetes;

namespace Steeltoe.Extensions.Configuration.Kubernetes;

public static class KubernetesConfigurationBuilderExtensions
{
    /// <summary>
    /// Add configuration providers for ConfigMaps and Secrets.
    /// </summary>
    /// <param name="configurationBuilder">
    /// <see cref="IConfigurationBuilder" />.
    /// </param>
    /// <param name="kubernetesClientConfiguration">
    /// Kubernetes client configuration customization.
    /// </param>
    /// <param name="loggerFactory">
    /// <see cref="ILoggerFactory" /> for logging within configuration providers.
    /// </param>
    public static IConfigurationBuilder AddKubernetes(this IConfigurationBuilder configurationBuilder,
        Action<KubernetesClientConfiguration> kubernetesClientConfiguration = null, ILoggerFactory loggerFactory = null)
    {
        ArgumentGuard.NotNull(configurationBuilder);

        ILogger logger = loggerFactory?.CreateLogger("Steeltoe.Extensions.Configuration.Kubernetes");

        var appInfo = new KubernetesApplicationOptions(configurationBuilder.Build());

        if (appInfo.Enabled && (appInfo.Config.Enabled || appInfo.Secrets.Enabled))
        {
            logger?.LogTrace("Steeltoe Kubernetes is enabled");

            string lowercaseAppName = appInfo.Name.ToLowerInvariant();
            string lowercaseAppEnvName = (appInfo.Name + appInfo.NameEnvironmentSeparator + appInfo.EnvironmentName).ToLowerInvariant();

            IKubernetes kubernetesClient = KubernetesClientHelpers.GetKubernetesClient(appInfo, kubernetesClientConfiguration, logger);

            if (appInfo.Config.Enabled)
            {
                configurationBuilder
                    .Add(new KubernetesConfigMapSource(kubernetesClient,
                        new KubernetesConfigSourceSettings(appInfo.NameSpace, lowercaseAppName, appInfo.Reload, loggerFactory))).Add(
                        new KubernetesConfigMapSource(kubernetesClient,
                            new KubernetesConfigSourceSettings(appInfo.NameSpace, lowercaseAppEnvName, appInfo.Reload, loggerFactory)));

                foreach (NamespacedResource configMap in appInfo.Config.Sources)
                {
                    configurationBuilder.Add(new KubernetesConfigMapSource(kubernetesClient,
                        new KubernetesConfigSourceSettings(configMap.Namespace, configMap.Name, appInfo.Reload, loggerFactory)));
                }
            }

            if (appInfo.Secrets.Enabled)
            {
                configurationBuilder
                    .Add(new KubernetesSecretSource(kubernetesClient,
                        new KubernetesConfigSourceSettings(appInfo.NameSpace, lowercaseAppName, appInfo.Reload, loggerFactory))).Add(
                        new KubernetesSecretSource(kubernetesClient,
                            new KubernetesConfigSourceSettings(appInfo.NameSpace, lowercaseAppEnvName, appInfo.Reload, loggerFactory)));

                foreach (NamespacedResource secret in appInfo.Secrets.Sources)
                {
                    configurationBuilder.Add(new KubernetesSecretSource(kubernetesClient,
                        new KubernetesConfigSourceSettings(secret.Namespace, secret.Name, appInfo.Reload, loggerFactory)));
                }
            }
        }

        return configurationBuilder;
    }
}
