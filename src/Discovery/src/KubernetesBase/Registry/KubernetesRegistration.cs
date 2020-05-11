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

using System;
using System.Collections.Generic;
using k8s;
using Steeltoe.Discovery.KubernetesBase.Discovery;

namespace Steeltoe.Discovery.KubernetesBase.Registry
{
    public class KubernetesRegistration : IKubernetesRegistration
    {
        public IKubernetes KubernetesClient { get; set; }

        public KubernetesDiscoveryOptions KubernetesDiscoveryOptions { get; set; }

        public bool IsRunning { get; set; }

        public KubernetesRegistration(IKubernetes kubernetesClient, 
            KubernetesDiscoveryOptions kubernetesDiscoveryOptions)
        {
            KubernetesClient = kubernetesClient;
            KubernetesDiscoveryOptions = kubernetesDiscoveryOptions;
        }
        
        public string ServiceId => this.KubernetesDiscoveryOptions.ServiceName;
        public string Host => this.KubernetesClient.BaseUri.Host;
        public int Port => 0;
        public bool IsSecure => false;
        public Uri Uri => this.KubernetesClient.BaseUri;
        public IDictionary<string, string> Metadata => new Dictionary<string, string>();
        public string InstanceId { get; set; }
    }
}