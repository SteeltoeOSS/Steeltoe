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

namespace Steeltoe.Extensions.Configuration.Kubernetes
{
    internal class KubernetesConfigMapSource : IConfigurationSource
    {
        private IKubernetes K8sClient { get; set; }

        private KubernetesConfigSourceSettings ConfigSettings { get; set; }

        internal KubernetesConfigMapSource(IKubernetes kubernetesClient, KubernetesConfigSourceSettings settings)
        {
            K8sClient = kubernetesClient;
            ConfigSettings = settings;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder) => new KubernetesConfigMapProvider(K8sClient, ConfigSettings);
    }
}
