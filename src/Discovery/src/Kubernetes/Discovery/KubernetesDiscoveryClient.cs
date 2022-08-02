// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Discovery;

namespace Steeltoe.Discovery.Kubernetes.Discovery;

public class KubernetesDiscoveryClient : IDiscoveryClient
{
    private const string DefaultNamespace = "default";
    private readonly ILogger<KubernetesDiscoveryClient> _logger;
    private readonly IOptionsMonitor<KubernetesDiscoveryOptions> _discoveryOptions;
    private readonly DefaultIsServicePortSecureResolver _isServicePortSecureResolver;

    public string Description => "Steeltoe provided Kubernetes native service discovery client";

    public IList<string> Services => GetServices(null);

    public IKubernetes KubernetesClient { get; set; }

    public KubernetesDiscoveryClient(DefaultIsServicePortSecureResolver isServicePortSecureResolver, IKubernetes kubernetesClient,
        IOptionsMonitor<KubernetesDiscoveryOptions> discoveryOptions, ILogger<KubernetesDiscoveryClient> logger = null)
    {
        _isServicePortSecureResolver = isServicePortSecureResolver;
        KubernetesClient = kubernetesClient;
        _discoveryOptions = discoveryOptions;
        _logger = logger;
    }

    public IList<string> GetServices(IDictionary<string, string> labels)
    {
        if (!_discoveryOptions.CurrentValue.Enabled)
        {
            return Array.Empty<string>();
        }

        string labelSelectorValue = labels != null ? string.Join(",", labels.Keys.Select(k => $"{k}={labels[k]}")) : null;

        if (_discoveryOptions.CurrentValue.AllNamespaces)
        {
            return KubernetesClient.ListServiceForAllNamespaces(labelSelector: labelSelectorValue).Items.Select(service => service.Metadata.Name).ToList();
        }

        return KubernetesClient.ListNamespacedService(_discoveryOptions.CurrentValue.Namespace, labelSelector: labelSelectorValue).Items
            .Select(service => service.Metadata.Name).ToList();
    }

    public IList<IServiceInstance> GetInstances(string serviceId)
    {
        if (serviceId == null)
        {
            throw new ArgumentNullException(nameof(serviceId));
        }

        IList<V1Endpoints> endpoints = _discoveryOptions.CurrentValue.AllNamespaces
            ? KubernetesClient.ListEndpointsForAllNamespaces(fieldSelector: $"metadata.name={serviceId}").Items
            : KubernetesClient.ListNamespacedEndpoints(
                _discoveryOptions.CurrentValue.Namespace ?? DefaultNamespace, fieldSelector: $"metadata.name={serviceId}").Items;

        IEnumerable<EndpointSubsetNs> subsetsNs = endpoints.Select(GetSubsetsFromEndpoints);

        var serviceInstances = new List<IServiceInstance>();

        if (subsetsNs.Any())
        {
            foreach (EndpointSubsetNs es in subsetsNs)
            {
                serviceInstances.AddRange(GetNamespacedServiceInstances(es, serviceId));
            }
        }

        return serviceInstances;
    }

    public IServiceInstance GetLocalServiceInstance()
    {
        IList<IServiceInstance> instances = GetInstances(_discoveryOptions.CurrentValue.ServiceName);

        if (instances.Count == 1)
        {
            return instances.First();
        }

        // todo: identify which instance is actually correct!
        _logger?.LogWarning("The local service instance was requested, but what we returned might not be correct!");
        return instances[0];
    }

    public Task ShutdownAsync()
    {
        return Task.CompletedTask;
    }

    private IList<IServiceInstance> GetNamespacedServiceInstances(EndpointSubsetNs es, string serviceId)
    {
        string k8SNamespace = es.Namespace;
        IList<V1EndpointSubset> subsets = es.EndpointSubsets;
        var instances = new List<IServiceInstance>();

        if (subsets.Any())
        {
            V1Service service = KubernetesClient.ListNamespacedService(k8SNamespace, fieldSelector: $"metadata.name={serviceId}").Items.FirstOrDefault();
            IDictionary<string, string> serviceMetadata = GetServiceMetadata(service);
            Metadata metadataProps = _discoveryOptions.CurrentValue.Metadata;

            foreach (V1EndpointSubset subset in subsets)
            {
                // Extend the service metadata map with per-endpoint port information (if requested)
                var endpointMetadata = new Dictionary<string, string>(serviceMetadata);

                if (metadataProps.AddPorts)
                {
                    Dictionary<string, string> ports = subset.Ports.Where(p => !string.IsNullOrWhiteSpace(p.Name))
                        .ToDictionary(p => p.Name, p => p.Port.ToString());

                    IDictionary<string, string> portMetadata = GetDictionaryWithPrefixedKeys(ports, metadataProps.PortsPrefix);

                    foreach (KeyValuePair<string, string> portMetadataRecord in portMetadata)
                    {
                        endpointMetadata.Add(portMetadataRecord.Key, portMetadataRecord.Value);
                    }
                }

                IList<V1EndpointAddress> addresses = subset.Addresses;

                foreach (V1EndpointAddress endpointAddress in addresses)
                {
                    string instanceId = null;

                    if (endpointAddress.TargetRef != null)
                    {
                        instanceId = endpointAddress.TargetRef.Uid;
                    }

                    Corev1EndpointPort endpointPort = FindEndpointPort(subset);

                    instances.Add(new KubernetesServiceInstance(instanceId, serviceId, endpointAddress, endpointPort, endpointMetadata,
                        _isServicePortSecureResolver.Resolve(new Input(service?.Metadata.Name, endpointPort.Port, service?.Metadata.Labels,
                            service?.Metadata.Annotations))));
                }
            }
        }

        return instances;
    }

    private IDictionary<string, string> GetServiceMetadata(V1Service service)
    {
        var serviceMetadata = new Dictionary<string, string>();
        Metadata metadataProps = _discoveryOptions.CurrentValue.Metadata;

        if (metadataProps.AddLabels)
        {
            IDictionary<string, string> labelMetadata = GetDictionaryWithPrefixedKeys(service.Metadata.Labels, metadataProps.LabelsPrefix);

            foreach (KeyValuePair<string, string> label in labelMetadata)
            {
                serviceMetadata.Add(label.Key, label.Value);
            }
        }

        if (metadataProps.AddAnnotations)
        {
            IDictionary<string, string> annotationMetadata = GetDictionaryWithPrefixedKeys(service.Metadata.Annotations, metadataProps.AnnotationsPrefix);

            foreach (KeyValuePair<string, string> annotation in annotationMetadata)
            {
                serviceMetadata.Add(annotation.Key, annotation.Value);
            }
        }

        return serviceMetadata;
    }

    private Corev1EndpointPort FindEndpointPort(V1EndpointSubset subset)
    {
        IList<Corev1EndpointPort> ports = subset.Ports;
        Corev1EndpointPort endpointPort;

        if (ports.Count == 1)
        {
            endpointPort = ports[0];
        }
        else
        {
            endpointPort = ports.FirstOrDefault(port =>
                string.IsNullOrEmpty(_discoveryOptions.CurrentValue.PrimaryPortName) ||
                _discoveryOptions.CurrentValue.PrimaryPortName.ToUpper().Equals(port.Name.ToUpper()));
        }

        return endpointPort;
    }

    private EndpointSubsetNs GetSubsetsFromEndpoints(V1Endpoints endpoints)
    {
        // Start with config or default
        var es = new EndpointSubsetNs
        {
            Namespace = _discoveryOptions.CurrentValue.Namespace ?? DefaultNamespace
        };

        if (endpoints?.Subsets == null)
        {
            return es;
        }

        es.Namespace = endpoints.Metadata.NamespaceProperty;
        es.EndpointSubsets = endpoints.Subsets;

        return es;
    }

    /// <summary>
    /// Returns a new dictionary with supplied prefix applied to all keys.
    /// </summary>
    /// <param name="dict">
    /// Dictionary with keys for prefixing.
    /// </param>
    /// <param name="prefix">
    /// Prefix to add to keys.
    /// </param>
    /// <returns>
    /// A new dictionary that contains all the entries of the original dictionary with the keys prefixed.
    /// </returns>
    /// <remarks>
    /// If the prefix is null or empty, the dictionary itself is returned unchanged.
    /// </remarks>
    private IDictionary<string, string> GetDictionaryWithPrefixedKeys(IDictionary<string, string> dict, string prefix)
    {
        if (dict == null)
        {
            return new Dictionary<string, string>();
        }

        // when the prefix is empty, just return the same dictionary
        if (string.IsNullOrEmpty(prefix) || string.IsNullOrWhiteSpace(prefix))
        {
            return dict;
        }

        var prefixedDict = new Dictionary<string, string>();

        foreach (KeyValuePair<string, string> entry in dict)
        {
            prefixedDict.Add($"{prefix}{entry.Key}", entry.Key);
        }

        return prefixedDict;
    }
}
