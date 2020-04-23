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
using System.Text;

namespace Steeltoe.Extensions.Configuration.Kubernetes
{
    public class KubernetesSecretProvider : ConfigurationProvider
    {
        private IKubernetes K8sClient { get; set; }

        private string SecretName { get; set; }

        private string Namespace { get; set; }

        private Watcher<V1Secret> SecretWatcher { get; set; }

        public KubernetesSecretProvider(IKubernetes kubernetes, string secretName, string @namespace = "default")
        {
            K8sClient = kubernetes;
            SecretName = secretName;
            Namespace = @namespace;
        }

        internal IDictionary<string, string> Properties => Data;

        public override void Load()
        {
            var configMapWatch = K8sClient.ListNamespacedSecretWithHttpMessagesAsync(Namespace, fieldSelector: $"metadata.name={SecretName},metadata.namespace={Namespace}").GetAwaiter().GetResult();
            SecretWatcher = configMapWatch.Watch<V1Secret, V1SecretList>((type, item) =>
            {
                if (item?.Data?.Any() == true)
                {
                    foreach (var data in item.Data)
                    {
                        Properties[data.Key] = Encoding.UTF8.GetString(data.Value);
                    }
                }
                else
                {
                    Properties.Clear();
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
                SecretWatcher.Dispose();
                SecretWatcher = null;
                K8sClient.Dispose();
                K8sClient = null;
            }
        }
    }
}
