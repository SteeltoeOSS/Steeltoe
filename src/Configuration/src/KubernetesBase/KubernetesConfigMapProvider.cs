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
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Extensions.Configuration
{
    public class KubernetesConfigMapProvider : ConfigurationProvider
    {
        private List<string> ConfigMapNames { get; set; }

        private string Namespace { get; set; } = string.Empty;

        public KubernetesConfigMapProvider(List<string> configmapNames)
        {
            ConfigMapNames = configmapNames;
        }

        internal IDictionary<string, string> Properties => Data;

        public override void Load()
        {
            var kubeconfig = KubernetesClientConfiguration.BuildDefaultConfig();
            IKubernetes client = new Kubernetes(kubeconfig);
            var configmaps = client.ListConfigMapForAllNamespacesWithHttpMessagesAsync(true);
            using (configmaps.Watch<V1ConfigMap, V1ConfigMapList>((type, item) =>
            {
                // when a config map is added or updated, make sure it fits expected namespace and name
                if ((!string.IsNullOrEmpty(Namespace) && !item.Metadata.NamespaceProperty.Equals(Namespace)) || !ConfigMapNames.Contains(item.Metadata.Name))
                {
                    return;
                }

                foreach (var data in item.Data)
                {
                    Properties[data.Key] = data.Value;
                }
            }))
            {
                // idk?
            }
        }
    }
}
