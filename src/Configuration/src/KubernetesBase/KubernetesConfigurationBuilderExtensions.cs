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
using System;
using System.Collections.Generic;

namespace Steeltoe.Extensions.Configuration.Kubernetes
{
    public static class KubernetesConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddKubernetes(this IConfigurationBuilder configurationBuilder, Action<KubernetesClientConfiguration> kubernetesClientConfiguration = null)
        {
            if (configurationBuilder == null)
            {
                throw new ArgumentNullException(nameof(configurationBuilder));
            }

            var appInfo = new KubernetesApplicationOptions(configurationBuilder.Build());

            if (appInfo.Enabled)
            {
                var lowercaseAppName = appInfo.ApplicationName.ToLowerInvariant();
                var lowercaseAppEnvName = (appInfo.ApplicationName + appInfo.NameEnvironmentSeparator + appInfo.EnvironmentName).ToLowerInvariant();

                KubernetesClientConfiguration k8sConfig;

                try
                {
                    k8sConfig = KubernetesClientConfiguration.BuildDefaultConfig();
                }
                catch (KubeConfigException)
                {
                    // probably couldn't locate .kube\config, just go with an empty config object and fall back on user-defined Action to set the configuration
                    // TODO: log exception
                    k8sConfig = new KubernetesClientConfiguration();
                }

                kubernetesClientConfiguration?.Invoke(k8sConfig);

                // ------- TEMPORARY, REMOVE BEFORE MERGE --------------------
                var k8sconfig = new Dictionary<string, string>
                {
                    { "k8s:host", k8sConfig.Host },
                    { "k8s:namespace", k8sConfig.Namespace }, // <-- doesn't appear to get set, but can be found in the AccessToken ?
                    { "k8s:context", k8sConfig.CurrentContext }
                };
                configurationBuilder.AddInMemoryCollection(k8sconfig);
                // -----------------------------------------------------------

                IKubernetes k8sClient;

                try
                {
                    k8sClient = new k8s.Kubernetes(k8sConfig);
                }
                catch(KubeConfigException e)
                {
                    // TODO: log exception
                    throw new Exception("Failed to create Kubernetes client", e);
                }

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

                if (appInfo.Secrets.Enabled)
                {
                    configurationBuilder
                        .Add(new KubernetesSecretSource(k8sClient, new KubernetesConfigSourceSettings { Name = lowercaseAppName, Namespace = appInfo.NameSpace, Watch = appInfo.Reload.Enabled }))
                        .Add(new KubernetesSecretSource(k8sClient, new KubernetesConfigSourceSettings { Name = lowercaseAppEnvName, Namespace = appInfo.NameSpace, Watch = appInfo.Reload.Enabled }));
                    foreach (var secret in appInfo.Secrets.Sources)
                    {
                        configurationBuilder.Add(new KubernetesSecretSource(k8sClient, new KubernetesConfigSourceSettings { Name = secret.Name, Namespace = secret.NameSpace, Watch = appInfo.Reload.Enabled }));
                    }
                }
            }

            return configurationBuilder;
        }
    }
}
