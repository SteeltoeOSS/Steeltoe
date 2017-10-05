//
// Copyright 2015 the original author or authors.
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
//

using System.Collections.Generic;

namespace Steeltoe.CloudFoundry.Connector.Services
{
    public class RabbitServiceInfo : UriServiceInfo
    {
        public const string AMQP_SCHEME = "amqp";
        public const string AMQPS_SCHEME = "amqps";

        public string ManagementUri { get; internal protected set; }

        public List<string> Uris { get; internal protected set; }

        public List<string> ManagementUris { get; internal protected set; }

        public RabbitServiceInfo(string id, string host, int port, string username, string password, string virtualHost)
            : this(id, host, port, username, password, virtualHost, null)
        {
        }

        public RabbitServiceInfo(string id, string host, int port, string username, string password, string virtualHost, string managementUri)
            : base(id, AMQP_SCHEME, host, port, username, password, virtualHost)
        {
            ManagementUri = managementUri;
        }

        public RabbitServiceInfo(string id, string uri, string managementUri, List<string> uris, List<string> managementUris)
            : this(id, uri, managementUri)
        {
            Uris = uris;
            ManagementUris = managementUris;
        }

        public RabbitServiceInfo(string id, string uri)
            : this(id, uri, null)
        {
        }

        public RabbitServiceInfo(string id, string uri, string managementUri)
            : base(id, uri)
        {
            ManagementUri = managementUri;
        }

        public string VirtualHost
        {
            get
            {
                return Info.Path;
            }
        }
    }
}
