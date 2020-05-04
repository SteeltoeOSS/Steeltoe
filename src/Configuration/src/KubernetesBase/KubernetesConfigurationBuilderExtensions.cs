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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Kubernetes;
using System;

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

            if (appInfo.Enabled && (appInfo.Config.Enabled || appInfo.Secrets.Enabled))
            {
                logger?.LogTrace("Steeltoe Kubernetes is enabled");

                var lowercaseAppName = appInfo.Name.ToLowerInvariant();
                var lowercaseAppEnvName = (appInfo.Name + appInfo.NameEnvironmentSeparator + appInfo.EnvironmentName).ToLowerInvariant();

                var k8sClient = KubernetesClientHelpers.GetKubernetesClient(configurationBuilder.Build(), appInfo, kubernetesClientConfiguration, logger);

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
