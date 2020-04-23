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
using KubeClient;
using KubeClient.Extensions.Configuration;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Extensions.Configuration.Kubernetes
{
    public static class KubernetesConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddKubernetesConfiguration(this IConfigurationBuilder configurationBuilder, Action<KubernetesClientConfiguration> kubernetesClientConfiguration = null)
        {
            if (configurationBuilder == null)
            {
                throw new ArgumentNullException(nameof(configurationBuilder));
            }

            if (Platform.IsKubernetes)
            {
                var appInfo = new KubernetesApplicationOptions(configurationBuilder.Build());
                var lowercaseAppName = appInfo.ApplicationName.ToLowerInvariant();
                var lowercaseAppEnvName = (appInfo.ApplicationName + appInfo.NameEnvironmentSeparator + appInfo.EnvironmentName).ToLowerInvariant();

                KubernetesClientConfiguration k8sConfig = KubernetesClientConfiguration.BuildDefaultConfig();

                kubernetesClientConfiguration?.Invoke(k8sConfig);

                // ------- TEMPORARY, REMOVE BEFORE MERGET -------------------
                var k8sconfig = new Dictionary<string, string>
                {
                    { "k8s:host", k8sConfig.Host },
                    { "k8s:namespace", k8sConfig.Namespace },
                    { "k8s:context", k8sConfig.CurrentContext }
                };
                configurationBuilder.AddInMemoryCollection(k8sconfig);
                // -----------------------------------------------------------

                var k8sClient = new k8s.Kubernetes(k8sConfig);
                // var k8sClient = new k8s.Kubernetes(KubernetesClientConfiguration.BuildDefaultConfig());

                // ---------------------------------------------------------------------------------------
                // use KubernetesClient (official) with our own providers
                if (appInfo.Config.Enabled)
                {
                    configurationBuilder
                        .Add(new KubernetesConfigMapSource(k8sClient, new KubernetesConfigSourceSettings { Name = lowercaseAppName, Namespace = appInfo.NameSpace, Watch = appInfo.Reload.Enabled }))
                        .Add(new KubernetesConfigMapSource(k8sClient, new KubernetesConfigSourceSettings { Name = lowercaseAppEnvName, Namespace = appInfo.NameSpace, Watch = appInfo.Reload.Enabled }));
                    
                    foreach (var configmap in appInfo.Config.Sources)
                    {
                        configurationBuilder.Add(new KubernetesConfigMapSource(k8sClient, new KubernetesConfigSourceSettings { Name = configmap.Name, Namespace = configmap.NameSpace, Watch = appInfo.Reload.Enabled }));
                    }
                }

                //configurationBuilder
                //    .Add(new KubernetesSecretSource(k8sClient, lowercaseAppName, appInfo.NameSpace))
                //    .Add(new KubernetesSecretSource(k8sClient, lowercaseAppEnvName, appInfo.NameSpace));
                //foreach (var secret in appInfo.Secrets.Sources)
                //{
                //    configurationBuilder.Add(new KubernetesSecretSource(k8sClient, secret.Name, secret.NameSpace ?? appInfo.NameSpace));
                //}
            }

            return configurationBuilder;
        }
    }
}
