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
using Steeltoe.Common.Discovery;
using k8s.Models;

namespace Steeltoe.Discovery.KubernetesBase.Discovery
{
    public class KubernetesServiceInstance : IServiceInstance
    {
        private const string HttpPrefix = "http";
        private const string HttpsPrefix = "https";
        private const string Dsl = "//";
        private const string Coln = ":";

        private V1EndpointAddress _endpointAddress;
        private V1EndpointPort _endpointPort;

        public string InstanceId { get; }
        public string ServiceId { get; }

        public string Host => _endpointAddress.Ip;

        public int Port => _endpointPort.Port;
        public bool IsSecure { get; }

        public Uri Uri => new Uri($"{GetScheme()}{Coln}{Dsl}{Host}{Coln}{Port}");

        public IDictionary<string, string> Metadata { get; }

        public KubernetesServiceInstance(string instanceId, string serviceId, V1EndpointAddress endpointAddress, 
            V1EndpointPort endpointPort, IDictionary<string, string> metadata, bool isSecure)
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
}