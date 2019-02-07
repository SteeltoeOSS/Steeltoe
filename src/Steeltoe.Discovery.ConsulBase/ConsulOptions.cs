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

namespace Steeltoe.Consul.Client
{
    /// <summary>
    /// Configuration options to use in creating a Consul client. See the documentation
    /// of the Consul client for more details
    /// </summary>
    public class ConsulOptions
    {
        public const string CONSUL_CONFIGURATION_PREFIX = "consul";

        /// <summary>
        /// Gets or sets the host address of the Consul server, default localhost
        /// </summary>
        public string Host { get; set; } = "localhost";

        /// <summary>
        /// Gets or sets the scheme used for the Consul server, default http
        /// </summary>
        public string Scheme { get; set; } = "http";

        /// <summary>
        /// Gets or sets the port number used for the Consul server, default 8500
        /// </summary>
        public int Port { get; set; } = 8500;

        /// <summary>
        /// Gets or sets the data center to use
        /// </summary>
        public string Datacenter { get; set; }

        /// <summary>
        /// Gets or sets the access token to use
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets the wait time to use
        /// </summary>
        public string WaitTime { get; set; }

        /// <summary>
        /// Gets or sets the user name to use
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets password to use
        /// </summary>
        public string Password { get; set; }
    }
}