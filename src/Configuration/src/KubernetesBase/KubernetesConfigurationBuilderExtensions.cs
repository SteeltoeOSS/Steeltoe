// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using k8s;
using k8s.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using System;
using System.Linq;

namespace Steeltoe.Extensions.Configuration.Kubernetes
{
    public static class KubernetesConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddKubernetes(this IConfigurationBuilder configurationBuilder, Action<KubernetesClientConfiguration> kubernetesClientConfiguration = null, ILoggerFactory loggerFactory = null)
        {
            if (configurationBuilder == null)
            {
                throw new ArgumentNullException(nameof(configurationBuilder));
            }

            var logger = loggerFactory?.CreateLogger("Steeltoe.Extensions.Configuration.Kubernetes");

            var appInfo = new KubernetesApplicationOptions(configurationBuilder.Build());

            if (appInfo.Enabled)
            {
                logger?.LogTrace("Steeltoe Kubernetes is enabled");

                var lowercaseAppName = appInfo.ApplicationName.ToLowerInvariant();
                var lowercaseAppEnvName = (appInfo.ApplicationName + appInfo.NameEnvironmentSeparator + appInfo.EnvironmentName).ToLowerInvariant();

                KubernetesClientConfiguration k8sConfig = null;

                try
                {
                    if (appInfo.Config.Paths.Any())
                    {
                        var delimiter = Platform.IsWindows ? ';' : ':';
                        var joinedPaths = appInfo.Config.Paths.Aggregate((i, j) => i + delimiter + j);
                        Environment.SetEnvironmentVariable("KUBECONFIG", joinedPaths);
                    }

                    k8sConfig = KubernetesClientConfiguration.BuildDefaultConfig();
                }
                catch (KubeConfigException e)
                {
                    // couldn't locate .kube\config or user-identified files. use an empty config object and fall back on user-defined Action to set the configuration
                    logger?.LogWarning(e, "Failed to build KubernetesClientConfiguration using files at configured or default location, creating an empty config...");
                }

                kubernetesClientConfiguration?.Invoke(k8sConfig ?? new KubernetesClientConfiguration());

                IKubernetes k8sClient;

                try
                {
                    k8sClient = new k8s.Kubernetes(k8sConfig);
                }
                catch (KubeConfigException e)
                {
                    logger?.LogCritical(e, "Failed to create Kubernetes client");
                    throw;
                }

                // ---------------------------------------------------------------------------------------
                // use KubernetesClient (official) with our own providers
                if (appInfo.Config.Enabled)
                {
                    var configMapProviderLogger = loggerFactory?.CreateLogger<KubernetesConfigMapProvider>();

                    configurationBuilder
                        .Add(new KubernetesConfigMapSource(k8sClient, new KubernetesConfigSourceSettings(appInfo.NameSpace, lowercaseAppName, appInfo.Reload.Enabled, configMapProviderLogger)))
                        .Add(new KubernetesConfigMapSource(k8sClient, new KubernetesConfigSourceSettings(appInfo.NameSpace, lowercaseAppEnvName, appInfo.Reload.Enabled, configMapProviderLogger)));

                    foreach (var configmap in appInfo.Config.Sources)
                    {
                        configurationBuilder.Add(new KubernetesConfigMapSource(k8sClient, new KubernetesConfigSourceSettings(configmap.NameSpace, configmap.Name, appInfo.Reload.Enabled, configMapProviderLogger)));
                    }
                }

                if (appInfo.Secrets.Enabled)
                {
                    var secretProviderLogger = loggerFactory?.CreateLogger<KubernetesSecretProvider>();

                    configurationBuilder
                        .Add(new KubernetesSecretSource(k8sClient, new KubernetesConfigSourceSettings(appInfo.NameSpace, lowercaseAppName, appInfo.Reload.Enabled, secretProviderLogger)))
                        .Add(new KubernetesSecretSource(k8sClient, new KubernetesConfigSourceSettings(appInfo.NameSpace, lowercaseAppEnvName, appInfo.Reload.Enabled, secretProviderLogger)));
                    foreach (var secret in appInfo.Secrets.Sources)
                    {
                        configurationBuilder.Add(new KubernetesSecretSource(k8sClient, new KubernetesConfigSourceSettings(secret.NameSpace, secret.Name, appInfo.Reload.Enabled, secretProviderLogger)));
                    }
                }
            }

            return configurationBuilder;
        }
    }
}
