// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s.Models;
using Steeltoe.Common.Discovery;
using System;
using System.Collections.Generic;

namespace Steeltoe.Discovery.Kubernetes.Discovery;

public class KubernetesServiceInstance : IServiceInstance
{
    private const string HttpPrefix = "http";
    private const string HttpsPrefix = "https";
    private const string Dsl = "//";
    private const string Coln = ":";

    private readonly V1EndpointAddress _endpointAddress;
    private readonly Corev1EndpointPort _endpointPort;

    public string InstanceId { get; }

    public string ServiceId { get; }

    public string Host => _endpointAddress.Ip;

    public int Port => _endpointPort.Port;

    public bool IsSecure { get; }

    public Uri Uri => new ($"{GetScheme()}{Coln}{Dsl}{Host}{Coln}{Port}");

    public IDictionary<string, string> Metadata { get; }

    public KubernetesServiceInstance(
        string instanceId,
        string serviceId,
        V1EndpointAddress endpointAddress,
        Corev1EndpointPort endpointPort,
        IDictionary<string, string> metadata,
        bool isSecure)
    {
        InstanceId = instanceId;
        ServiceId = serviceId;
        _endpointAddress = endpointAddress;
        _endpointPort = endpointPort;
        IsSecure = isSecure;
        Metadata = metadata;
    }

    public string GetScheme()
    {
        return IsSecure ? HttpsPrefix : HttpPrefix;
    }
}
