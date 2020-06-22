﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Kubernetes;
using System;

namespace Steeltoe.Extensions.Configuration.Kubernetes
{
    public static class KubernetesConfigurationBuilderExtensions
    {
        /// <summary>
        /// Add configuration providers for ConfigMaps and Secrets
        /// </summary>
        /// <param name="configurationBuilder"><see cref="IConfigurationBuilder"/></param>
        /// <param name="kubernetesClientConfiguration">Kubernetes client configuration customization</param>
        /// <param name="loggerFactory"><see cref="ILoggerFactory"/> for logging within config providers</param>
        public static IConfigurationBuilder AddKubernetes(this IConfigurationBuilder configurationBuilder, Action<KubernetesClientConfiguration> kubernetesClientConfiguration = null, ILoggerFactory loggerFactory = null)
        {
            if (configurationBuilder == null)
            {
                throw new ArgumentNullException(nameof(configurationBuilder));
            }

            var logger = loggerFactory?.CreateLogger("Steeltoe.Extensions.Configuration.Kubernetes");

            var appInfo = new KubernetesApplicationOptions(configurationBuilder.Build());

            if (appInfo.Enabled && (appInfo.Config.Enabled || appInfo.Secrets.Enabled))
            {
                logger?.LogTrace("Steeltoe Kubernetes is enabled");

                var lowercaseAppName = appInfo.Name.ToLowerInvariant();
                var lowercaseAppEnvName = (appInfo.Name + appInfo.NameEnvironmentSeparator + appInfo.EnvironmentName).ToLowerInvariant();

                var k8sClient = KubernetesClientHelpers.GetKubernetesClient(appInfo, kubernetesClientConfiguration, logger);

                if (appInfo.Config.Enabled)
                {
                    var configMapProviderLogger = loggerFactory?.CreateLogger<KubernetesConfigMapProvider>();

                    configurationBuilder
                        .Add(new KubernetesConfigMapSource(k8sClient, new KubernetesConfigSourceSettings(appInfo.NameSpace, lowercaseAppName, appInfo.Reload, configMapProviderLogger)))
                        .Add(new KubernetesConfigMapSource(k8sClient, new KubernetesConfigSourceSettings(appInfo.NameSpace, lowercaseAppEnvName, appInfo.Reload, configMapProviderLogger)));

                    foreach (var configmap in appInfo.Config.Sources)
                    {
                        configurationBuilder.Add(new KubernetesConfigMapSource(k8sClient, new KubernetesConfigSourceSettings(configmap.Namespace, configmap.Name, appInfo.Reload, configMapProviderLogger)));
                    }
                }

                if (appInfo.Secrets.Enabled)
                {
                    var secretProviderLogger = loggerFactory?.CreateLogger<KubernetesSecretProvider>();

                    configurationBuilder
                        .Add(new KubernetesSecretSource(k8sClient, new KubernetesConfigSourceSettings(appInfo.NameSpace, lowercaseAppName, appInfo.Reload, secretProviderLogger)))
                        .Add(new KubernetesSecretSource(k8sClient, new KubernetesConfigSourceSettings(appInfo.NameSpace, lowercaseAppEnvName, appInfo.Reload, secretProviderLogger)));
                    foreach (var secret in appInfo.Secrets.Sources)
                    {
                        configurationBuilder.Add(new KubernetesSecretSource(k8sClient, new KubernetesConfigSourceSettings(secret.Namespace, secret.Name, appInfo.Reload, secretProviderLogger)));
                    }
                }
            }

            return configurationBuilder;
        }
    }
}
