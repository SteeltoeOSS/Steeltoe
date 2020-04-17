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

using KubeClient;
using KubeClient.Extensions.Configuration;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using System;
using System.Collections.Generic;

namespace Steeltoe.Extensions.Configuration
{
    public static class KubernetesConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddKubernetesConfiguration(this IConfigurationBuilder configurationBuilder, IEnumerable<string> additionalConfigMaps = null, IEnumerable<string> additionalSecrets = null)
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

                // use KubeClient's configuration providers for configmaps and secrets
                var kubeOptions = KubeClientOptions.FromPodServiceAccount();
                configurationBuilder
                    .AddKubeConfigMap(kubeOptions, lowercaseAppName, appInfo.NameSpace, reloadOnChange: true)
                    .AddKubeConfigMap(kubeOptions, lowercaseAppEnvName, appInfo.NameSpace, reloadOnChange: true, throwOnNotFound: false);
                
                if (additionalConfigMaps != null)
                {
                    foreach (var c in additionalConfigMaps)
                    {
                        configurationBuilder.AddKubeConfigMap(kubeOptions, c, throwOnNotFound: false, reloadOnChange: true);
                    }
                }

                configurationBuilder
                    .AddKubeSecret(kubeOptions, lowercaseAppName, appInfo.NameSpace, reloadOnChange: true)
                    .AddKubeSecret(kubeOptions, lowercaseAppEnvName, appInfo.NameSpace, reloadOnChange: true);

                if (additionalSecrets != null)
                {
                    foreach (var secret in additionalSecrets)
                    {
                        configurationBuilder.AddKubeSecret(kubeOptions, secret, reloadOnChange: true);
                    }
                }

                // ---------------------------------------------------------------------------------------
                // use KubernetesClient (official) with our own providers
                //return configurationBuilder
                //    .Add(new KubernetesConfigMapSource(appInfo))
                //    .Add(new KubernetesSecretSource(appInfo));
            }

            return configurationBuilder;
        }
    }
}
