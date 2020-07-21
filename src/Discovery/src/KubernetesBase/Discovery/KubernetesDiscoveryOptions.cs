// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.Discovery.KubernetesBase.Discovery
{
    public class KubernetesDiscoveryOptions
    {
        public const string KUBERNETES_DISCOVERY_CONFIGURATION_PREFIX = "spring:cloud:kubernetes:discovery";

        /// <summary>
        /// Gets or sets a value indicating whether service discovery by Kubernetes API is enabled
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets a value representing the service name of the local instance
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the client is discovering all namespaces
        /// </summary>
        public bool AllNamespaces { get; set; } = false;

        /// <summary>
        /// Gets or sets a value representing the namespace the service is being deployed to.
        /// <para>If AllNamespaces = false, will only discover services in this namespace; </para>
        /// If AllNamespaces = true, this + ServiceName is used to identify the local service instance
        /// </summary>
        public string Namespace { get; set; } = "default";

        /// <summary>
        /// Gets or sets a list of port numbers that are considered secure and use HTTPS
        /// </summary>
        public List<int> KnownSecurePorts { get; set; } = new List<int> { 443, 8443 };

        /// <summary>
        /// Gets or sets a list of labels to filter on when fetching services from the Kubernetes API
        /// </summary>
        public Dictionary<string, string> ServiceLabels { get; set; }

        /// <summary>
        /// Gets or sets a value holding the name of the primary port when multiple ports are defined for a service
        /// </summary>
        public string PrimaryPortName { get; set; }

        /// <summary>
        /// Gets or sets additional service data
        /// </summary>
        public Metadata Metadata { get; set; } = new Metadata();

        public override string ToString()
        {
            return $"serviceName: {ServiceName}, serviceLabels: {ServiceLabels}";
        }
    }
}