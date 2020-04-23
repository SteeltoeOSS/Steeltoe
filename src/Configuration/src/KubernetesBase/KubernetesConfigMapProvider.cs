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
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;

namespace Steeltoe.Extensions.Configuration.Kubernetes
{
    internal class KubernetesConfigMapProvider : ConfigurationProvider, IDisposable
    {
        private IKubernetes K8sClient { get; set; }

        private KubernetesConfigSourceSettings Settings { get; set; }

        private Watcher<V1ConfigMap> ConfigMapWatcher { get; set; }

        internal KubernetesConfigMapProvider(IKubernetes kubernetes, KubernetesConfigSourceSettings settings)
        {
            K8sClient = kubernetes;
            Settings = settings;
        }

        public override void Load()
        {
            var configMapWatch = K8sClient.ListNamespacedConfigMapWithHttpMessagesAsync(Settings.Namespace, fieldSelector: $"metadata.name={Settings.Name}", watch: Settings.Watch).GetAwaiter().GetResult();
            ConfigMapWatcher = configMapWatch.Watch<V1ConfigMap, V1ConfigMapList>((type, item) =>
                {
                    if (item?.Data?.Any() == true)
                    {
                        foreach (var data in item.Data)
                        {
                            Data[data.Key] = data.Value;
                        }
                    }
                    else
                    {
                        Data.Clear();
                    }
                });
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                ConfigMapWatcher.Dispose();
                ConfigMapWatcher = null;
                K8sClient.Dispose();
                K8sClient = null;
            }
        }
    }
}
