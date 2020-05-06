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
using k8s.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using Steeltoe.Common.Kubernetes;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace Steeltoe.Extensions.Configuration.Kubernetes
{
    internal class KubernetesConfigMapProvider : KubernetesProviderBase, IDisposable
    {
        private Watcher<V1ConfigMap> ConfigMapWatcher { get; set; }

        internal KubernetesConfigMapProvider(IKubernetes kubernetes, KubernetesConfigSourceSettings settings, CancellationToken cancellationToken = default)
            : base(kubernetes, settings, cancellationToken)
        {
            settings.Namespace ??= "default";
        }

        public override void Load()
        {
            try
            {
                var configMapResponse = K8sClient.ReadNamespacedConfigMapWithHttpMessagesAsync(Settings.Name, Settings.Namespace).GetAwaiter().GetResult();
                ProcessData(configMapResponse.Body);
                EnableReloading();
            }
            catch (HttpOperationException e)
            {
                if (e.Response.StatusCode == HttpStatusCode.Forbidden)
                {
                    Settings.Logger?.LogCritical(e, "Failed to retrieve config map '{configmapName}' in namespace '{configmapNamespace}'. Confirm that your service account has the necessary permissions", Settings.Name, Settings.Namespace);
                }
                else if (e.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    EnableReloading();
                    return;
                }

                throw;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                ConfigMapWatcher?.Dispose();
                ConfigMapWatcher = null;
                K8sClient.Dispose();
                K8sClient = null;
            }
        }

        private void EnableReloading()
        {
            if (Settings.ReloadSettings.ConfigMaps && !Polling)
            {
                switch (Settings.ReloadSettings.Mode)
                {
                    case ReloadMethods.Event:
                        ConfigMapWatcher = K8sClient.WatchNamespacedConfigMapAsync(
                            Settings.Name,
                            Settings.Namespace,
                            onEvent: (eventType, item) =>
                            {
                                Settings.Logger?.LogInformation("Receved {eventType} event for ConfigMap {configMapName} with {entries} values", eventType.ToString(), Settings.Name, item?.Data?.Count);
                                switch (eventType)
                                {
                                    case WatchEventType.Added:
                                    case WatchEventType.Modified:
                                    case WatchEventType.Deleted:
                                        ProcessData(item);
                                        break;
                                    default:
                                        Settings.Logger?.LogDebug("Event type {eventType} is not support, no action has been taken", eventType);
                                        break;
                                }
                            },
                            onError: (exception) =>
                            {
                                Settings.Logger?.LogCritical(exception, "ConfigMap watcher on {namespace}.{name} encountered an error!", Settings.Namespace, Settings.Name);
                            },
                            onClosed: () => { Settings.Logger?.LogInformation("ConfigMap watcher on {namespace}.{name} connection has closed", Settings.Namespace, Settings.Name); }).GetAwaiter().GetResult();
                        break;
                    case ReloadMethods.Polling:
                        StartPolling(Settings.ReloadSettings.Period);
                        break;
                    default:
                        Settings.Logger?.LogError("Unsupported reload method!");
                        break;
                }
            }
        }

        private void ProcessData(V1ConfigMap item)
        {
            if (item is null)
            {
                Settings.Logger?.LogWarning("ConfigMap response is null, no data could be processed");
                return;
            }

            var configMapContents = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            if (item?.Data != null)
            {
                foreach (var data in item?.Data)
                {
                    configMapContents[data.Key] = data.Value;
                }
            }

            Data = configMapContents;
        }
    }
}
