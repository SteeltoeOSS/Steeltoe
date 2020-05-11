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

using System.Collections.Generic;

namespace Steeltoe.Discovery.KubernetesBase.Discovery
{
    public class KubernetesDiscoveryOptions
    {
        public const string KUBERNETES_DISCOVERY_CONFIGURATION_PREFIX = "spring:cloud:kubernetes:discovery";
        
        // If Kubernetes Discovery is enabled
        public bool Enabled { get; set; }
        
        // The service name of the local instance
        public string ServiceName { get; set; }
        
        // If discovering all namespaces
        public bool AllNamespaces { get; set; } = false;
        
        // Namespace the service is being deployed to.  If AllNamespaces = false,
        // will only discover services in this namespace;  If AllNamespaces = true,
        // this + ServiceName is used to identify the local service instance
        public string Namespace { get; set; } = "default";

        // Port numbers that are considered secure and use HTTPS
        public List<int> KnownSecurePorts { get; set; } = new List<int> { 443, 8443 };
        
        // If set, then only the services matching these labels will be
        // fetched from the Kubernetes API server
        public Dictionary<string, string> ServiceLabels { get; set; }
        
        // If set, then the port with a given name is used as primary when
        // multiple ports are defined for a service
        public string PrimaryPortName { get; set; }

        public Metadata Metadata { get; set; } = new Metadata();

        
        public override string ToString()
        {
            return $"serviceName: {ServiceName}, serviceLabels: {ServiceLabels}";
        }
    }
}