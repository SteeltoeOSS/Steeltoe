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
using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using Steeltoe.Common.Discovery;

namespace Steeltoe.Discovery.KubernetesBase.Discovery
{
    public class KubernetesDiscoveryClient : IDiscoveryClient
    {
        private const string DefaultNamespace = "default";
        private readonly KubernetesDiscoveryOptions _discoveryOptions;
        private readonly DefaultIsServicePortSecureResolver _isServicePortSecureResolver;


        public string Description => "Steeltoe provided Kubernetes native service discovery client";

        public IList<string> Services => this.GetServices(null);

        public IKubernetes kubernetesClient { get; set; }
        
        public KubernetesDiscoveryClient(
            DefaultIsServicePortSecureResolver isServicePortSecureResolver,
            IKubernetes kubernetesClient, 
            KubernetesDiscoveryOptions discoveryOptions)
        {
            _isServicePortSecureResolver = isServicePortSecureResolver;
            this.kubernetesClient = kubernetesClient;
            _discoveryOptions = discoveryOptions;
        }

        public IList<string> GetServices(IDictionary<string, string> labels)
        {
            var labelSelectorValue = 
                labels != null ?
                    string.Join(',', labels.Keys.Select(k => k + "=" + labels[k])) :
                    null;
            if (_discoveryOptions.AllNamespaces)
            {
                return this.kubernetesClient.ListServiceForAllNamespaces(
                        labelSelector: labelSelectorValue).Items
                    .Select(service => service.Metadata.Name).ToList();
            }
            else
            {
                return this.kubernetesClient.ListNamespacedService(namespaceParameter: _discoveryOptions.Namespace,
                        labelSelector: labelSelectorValue).Items
                    .Select(service => service.Metadata.Name).ToList();
            }
        }
        
        public IList<IServiceInstance> GetInstances(string serviceId)
        {
            if (serviceId == null)
            {
                throw new ArgumentNullException(nameof(serviceId));
            }


            var endpoints = this._discoveryOptions.AllNamespaces
                ? this.kubernetesClient.ListEndpointsForAllNamespaces(fieldSelector: $"metadata.name={serviceId}").Items
                : this.kubernetesClient.ListNamespacedEndpoints(_discoveryOptions.Namespace ?? DefaultNamespace, 
                    fieldSelector: $"metadata.name={serviceId}").Items;

            var subsetsNs = endpoints.Select(GetSubsetsFromEndpoints);

            var serviceInstances = new List<IServiceInstance>();
            if (subsetsNs.Any())
            {
                foreach (var es in subsetsNs)
                {
                    serviceInstances.AddRange(this.GetNamespacedServiceInstances(es, serviceId));
                }
            }

            return serviceInstances;
        }

        private IList<IServiceInstance> GetNamespacedServiceInstances(EndpointSubsetNs es, string serviceId)
        {
            var k8SNamespace = es.Namespace;
            var subsets = es.EndpointSubsets;
            var instances = new List<IServiceInstance>();
            if (subsets.Any())
            {
                var service = this.kubernetesClient.ListNamespacedService(namespaceParameter: k8SNamespace,
                    fieldSelector: $"metadata.name={serviceId}").Items.FirstOrDefault();
                var serviceMetadata = this.GetServiceMetadata(service);
                var metadataProps = this._discoveryOptions.Metadata;

                foreach (var subset in subsets)
                {
                    // Extend the service metadata map with per-endpoint port information (if requested)
                    var endpointMetadata = new Dictionary<string, string>(serviceMetadata);
                    if (metadataProps.AddPorts)
                    {
                        var ports = subset.Ports
                            .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                            .ToDictionary(p => p.Name, p => p.Port.ToString());

                        var portMetadata = GetDictionaryWithPrefixedKeys(ports, metadataProps.PortsPrefix);
                        
                        foreach (var portMetadataRecord in portMetadata)
                        {
                            endpointMetadata.Add(portMetadataRecord.Key, portMetadataRecord.Value);
                        }
                    }

                    var addresses = subset.Addresses;
                    foreach (var endpointAddress in addresses)
                    {
                        string instanceId = null;
                        if (endpointAddress.TargetRef != null)
                        {
                            instanceId = endpointAddress.TargetRef.Uid;
                        }

                        var endpointPort = FindEndpointPort(subset);
                        instances.Add(new KubernetesServiceInstance(instanceId, serviceId,
                            endpointAddress, endpointPort, endpointMetadata,
                            this._isServicePortSecureResolver.Resolve(
                                new Input(
                                    service?.Metadata.Name,
                                    endpointPort.Port,
                                    service?.Metadata.Labels,
                                    service?.Metadata.Annotations))));
                    }
                }
            }
            return instances;
        }

        private IDictionary<string, string> GetServiceMetadata(V1Service service)
        {
            var serviceMetadata = new Dictionary<string, string>();
            var metadataProps = this._discoveryOptions.Metadata;
            if (metadataProps.AddLabels)
            {
                var labelMetadata = GetDictionaryWithPrefixedKeys(
                    service.Metadata.Labels, metadataProps.LabelsPrefix);
                foreach (var label in labelMetadata)
                {
                    serviceMetadata.Add(label.Key, label.Value);
                }
            }

            if (metadataProps.AddAnnotations)
            {
                var annotationMetadata = GetDictionaryWithPrefixedKeys(
                    service.Metadata.Annotations,
                    metadataProps.AnnotationsPrefix);
                foreach (var annotation in annotationMetadata)
                {
                    serviceMetadata.Add(annotation.Key, annotation.Value);
                }
            }

            return serviceMetadata;
        }

        private V1EndpointPort FindEndpointPort(V1EndpointSubset subset)
        {
            var ports = subset.Ports;
            V1EndpointPort endpointPort;
            if (ports.Count == 1)
            {
                endpointPort = ports[0];
            }
            else
            {
                endpointPort = ports
                    .FirstOrDefault(port =>
                        string.IsNullOrEmpty(_discoveryOptions.PrimaryPortName) ||
                        _discoveryOptions.PrimaryPortName.ToUpper().Equals(port.Name.ToUpper()));
            }

            return endpointPort;
        }
        
        private EndpointSubsetNs GetSubsetsFromEndpoints(V1Endpoints endpoints)
        {
            // Start with config or default
            var es = new EndpointSubsetNs {Namespace = this._discoveryOptions.Namespace ?? DefaultNamespace};
            
            if (endpoints?.Subsets == null) return es;
            
            es.Namespace = endpoints.Metadata.NamespaceProperty;
            es.EndpointSubsets = endpoints.Subsets;

            return es;
        }

        // returns a new dictionary that contain all the entries of the original dictionary
        // but with the keys prefixed
        // if the prefix is null or empty, the dictionary itself is returned unchanged
        private IDictionary<string, string> GetDictionaryWithPrefixedKeys(IDictionary<string, string> dict,
            string prefix)
        {
            if (dict == null) return new Dictionary<string, string>();
            
            // when the prefix is empty, just return the same dictionary
            if (string.IsNullOrEmpty(prefix) || string.IsNullOrWhiteSpace(prefix)) return dict;

            var prefixedDict = new Dictionary<string, string>();
            foreach (var entry in dict)
            {
                prefixedDict.Add($"{prefix}{entry.Key}", entry.Key);
            }

            return prefixedDict;
        }

        public IServiceInstance GetLocalServiceInstance()
        {
            var instances = GetInstances(_discoveryOptions.ServiceName);
            
            return null;
        }

        public Task ShutdownAsync()
        {
            return Task.CompletedTask;
        }
    }
}