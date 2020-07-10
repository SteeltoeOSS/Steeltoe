// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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