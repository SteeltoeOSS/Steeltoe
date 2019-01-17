// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Consul;
using Steeltoe.Discovery.Consul.Discovery;
using Steeltoe.Discovery.Consul.Util;
using System;
using System.Collections.Generic;

namespace Steeltoe.Discovery.Consul.ServiceRegistry
{
    public class ConsulRegistration
    {
        public ConsulRegistration(AgentServiceRegistration agentServiceRegistration, ConsulDiscoveryOptions options)
        {
            AgentServiceRegistration = agentServiceRegistration;
            InstanceId = agentServiceRegistration.ID;
            ServiceId = agentServiceRegistration.Name;
            Host = agentServiceRegistration.Address;
            Port = agentServiceRegistration.Port;
            IsSecure = options.Scheme == "https";
            Uri = new Uri($"{options.Scheme}://{Host}:{Port}");
            Metadata = ConsulServerUtils.GetMetadata(agentServiceRegistration.Tags);
        }

        public AgentServiceRegistration AgentServiceRegistration { get; }

        public string InstanceId { get; }

        public string ServiceId { get; }

        public string Host { get; }

        public int Port { get; }

        public bool IsSecure { get; }

        public Uri Uri { get; }

        public IDictionary<string, string> Metadata { get; }
    }
}