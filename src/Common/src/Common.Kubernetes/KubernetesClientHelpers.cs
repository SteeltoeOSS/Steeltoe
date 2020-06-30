// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using k8s.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace Steeltoe.Common.Kubernetes
{
    public static class KubernetesClientHelpers
    {
        public static IKubernetes GetKubernetesClient(IConfiguration configuration, KubernetesApplicationOptions appInfo = null, Action<KubernetesClientConfiguration> kubernetesClientConfiguration = null, ILogger logger = null)
        {
            appInfo ??= new KubernetesApplicationOptions(configuration);

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

            return new k8s.Kubernetes(k8sConfig);
        }
    }
}
