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
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Extensions.Configuration.Kubernetes
{
    internal class KubernetesProviderBase : ConfigurationProvider
    {
        protected IKubernetes K8sClient { get; set; }

        protected KubernetesConfigSourceSettings Settings { get; set; }

        protected bool Polling { get; set; }

        protected CancellationToken CancellationToken { get; set; }

        internal KubernetesProviderBase(IKubernetes kubernetes, KubernetesConfigSourceSettings settings, CancellationToken token = default)
        {
            if (kubernetes is null)
            {
                throw new ArgumentNullException(nameof(kubernetes));
            }

            if (settings is null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            K8sClient = kubernetes;
            Settings = settings;
            CancellationToken = token;
        }

        protected void StartPolling(int interval)
        {
            Task.Factory.StartNew(
                () =>
                {
                    Polling = true;
                    while (Polling)
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(interval));
                        Load();
                        if (CancellationToken.IsCancellationRequested)
                        {
                            break;
                        }
                    }
                },
                CancellationToken);
        }
    }
}
