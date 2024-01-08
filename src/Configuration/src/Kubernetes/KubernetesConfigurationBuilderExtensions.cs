// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common;
using Steeltoe.Common.Kubernetes;

namespace Steeltoe.Configuration.Kubernetes;

public static class KubernetesConfigurationBuilderExtensions
{
    /// <summary>
    /// Adds configuration providers for Kubernetes ConfigMaps and Secrets.
    /// </summary>
    /// <param name="configurationBuilder">
    /// The configuration builder.
    /// </param>
    public static IConfigurationBuilder AddKubernetes(this IConfigurationBuilder configurationBuilder)
    {
        return AddKubernetes(configurationBuilder, null, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds configuration providers for Kubernetes ConfigMaps and Secrets.
    /// </summary>
    /// <param name="configurationBuilder">
    /// The configuration builder.
    /// </param>
    /// <param name="configureClient">
    /// Enables customization of the <see cref="KubernetesClientConfiguration" />.
    /// </param>
    public static IConfigurationBuilder AddKubernetes(this IConfigurationBuilder configurationBuilder, Action<KubernetesClientConfiguration> configureClient)
    {
        return AddKubernetes(configurationBuilder, configureClient, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds configuration providers for Kubernetes ConfigMaps and Secrets.
    /// </summary>
    /// <param name="configurationBuilder">
    /// The configuration builder.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public static IConfigurationBuilder AddKubernetes(this IConfigurationBuilder configurationBuilder, ILoggerFactory loggerFactory)
    {
        return AddKubernetes(configurationBuilder, null, loggerFactory);
    }

    /// <summary>
    /// Adds configuration providers for Kubernetes ConfigMaps and Secrets.
    /// </summary>
    /// <param name="configurationBuilder">
    /// The configuration builder.
    /// </param>
    /// <param name="configureClient">
    /// Enables customization of the <see cref="KubernetesClientConfiguration" />.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public static IConfigurationBuilder AddKubernetes(this IConfigurationBuilder configurationBuilder, Action<KubernetesClientConfiguration>? configureClient,
        ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(configurationBuilder);
        ArgumentGuard.NotNull(loggerFactory);

        ILogger logger = loggerFactory.CreateLogger(typeof(KubernetesConfigurationBuilderExtensions).Namespace!);

        var appInfo = new KubernetesApplicationOptions(configurationBuilder.Build());

        if (appInfo.Enabled && (appInfo.Config.Enabled || appInfo.Secrets.Enabled))
        {
            logger.LogTrace("Steeltoe Kubernetes is enabled");

#pragma warning disable S4040 // Strings should be normalized to uppercase
            string lowercaseAppName = appInfo.Name.ToLowerInvariant();
            string lowercaseAppEnvName = $"{appInfo.Name}{appInfo.NameEnvironmentSeparator}{appInfo.EnvironmentName}".ToLowerInvariant();
#pragma warning restore S4040 // Strings should be normalized to uppercase

            IKubernetes kubernetesClient = KubernetesClientHelpers.GetKubernetesClient(appInfo, configureClient, logger);

            var appSettings = new KubernetesConfigSourceSettings(appInfo.NameSpace, lowercaseAppName, appInfo.Reload, loggerFactory);
            var appEnvSettings = new KubernetesConfigSourceSettings(appInfo.NameSpace, lowercaseAppEnvName, appInfo.Reload, loggerFactory);

            if (appInfo.Config.Enabled)
            {
                configurationBuilder.Add(new KubernetesConfigMapSource(kubernetesClient, appSettings));
                configurationBuilder.Add(new KubernetesConfigMapSource(kubernetesClient, appEnvSettings));

                foreach (NamespacedResource configMap in appInfo.Config.Sources)
                {
                    var configMapSettings = new KubernetesConfigSourceSettings(configMap.Namespace, configMap.Name, appInfo.Reload, loggerFactory);
                    configurationBuilder.Add(new KubernetesConfigMapSource(kubernetesClient, configMapSettings));
                }
            }

            if (appInfo.Secrets.Enabled)
            {
                configurationBuilder.Add(new KubernetesSecretSource(kubernetesClient, appSettings));
                configurationBuilder.Add(new KubernetesSecretSource(kubernetesClient, appEnvSettings));

                foreach (NamespacedResource secret in appInfo.Secrets.Sources)
                {
                    var secretSettings = new KubernetesConfigSourceSettings(secret.Namespace, secret.Name, appInfo.Reload, loggerFactory);
                    configurationBuilder.Add(new KubernetesSecretSource(kubernetesClient, secretSettings));
                }
            }
        }

        return configurationBuilder;
    }
}
