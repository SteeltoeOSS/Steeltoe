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
using Steeltoe.Common.Discovery;
using Steeltoe.Consul.Util;
using System;
using System.Collections.Generic;

namespace Steeltoe.Discovery.Consul.Discovery
{
    /// <summary>
    /// A Consul service instance constructed from a ServiceEntry
    /// </summary>
    public class ConsulServiceInstance : IServiceInstance
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConsulServiceInstance"/> class.
        /// </summary>
        /// <param name="serviceEntry">the service entry from the Consul server</param>
        public ConsulServiceInstance(ServiceEntry serviceEntry)
        {
            // TODO: 3.0  ID = healthService.ID;
            Host = ConsulServerUtils.FindHost(serviceEntry);
            var metadata = ConsulServerUtils.GetMetadata(serviceEntry);
            IsSecure = metadata.TryGetValue("secure", out var secureString) && bool.Parse(secureString);
            ServiceId = serviceEntry.Service.Service;
            Port = serviceEntry.Service.Port;
            Metadata = metadata;
            var scheme = IsSecure ? "https" : "http";
            Uri = new Uri($"{scheme}://{Host}:{Port}");
        }

        #region Implementation of IServiceInstance

        /// <inheritdoc/>
        public string ServiceId { get; }

        /// <inheritdoc/>
        public string Host { get; }

        /// <inheritdoc/>
        public int Port { get; }

        /// <inheritdoc/>
        public bool IsSecure { get; }

        /// <inheritdoc/>
        public Uri Uri { get; }

        /// <inheritdoc/>
        public IDictionary<string, string> Metadata { get; }

        #endregion Implementation of IServiceInstance
    }
}