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

using Steeltoe.Common.Discovery;
using Steeltoe.Discovery.Consul.Util;
using System;
using System.Collections.Generic;

namespace Steeltoe.Discovery.Consul.Discovery
{
    internal class ThisServiceInstance : IServiceInstance
    {
        public ThisServiceInstance(ConsulDiscoveryOptions options)
        {
            ServiceId = options.ServiceName;
            Host = options.HostName;
            IsSecure = options.Scheme == "https";
            Port = options.Port ?? (IsSecure ? 443 : 80);
            var metadata = ConsulServerUtils.GetMetadata(options.Tags);
            Metadata = metadata;
            Uri = new Uri($"{options.Scheme}://{Host}:{Port}");
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