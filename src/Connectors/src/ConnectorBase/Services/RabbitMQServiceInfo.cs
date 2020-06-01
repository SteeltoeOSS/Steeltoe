// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.CloudFoundry.Connector.Services
{
    public class RabbitMQServiceInfo : UriServiceInfo
    {
        public const string AMQP_SCHEME = "amqp";
        public const string AMQPS_SCHEME = "amqps";

        public RabbitMQServiceInfo(string id, string host, int port, string username, string password, string virtualHost)
            : this(id, host, port, username, password, virtualHost, null)
        {
        }

        public RabbitMQServiceInfo(string id, string host, int port, string username, string password, string virtualHost, string managementUri)
            : base(id, AMQP_SCHEME, host, port, username, password, virtualHost)
        {
            ManagementUri = managementUri;
        }

        public RabbitMQServiceInfo(string id, string uri, string managementUri, List<string> uris, List<string> managementUris)
            : this(id, uri, managementUri)
        {
            Uris = uris;
            ManagementUris = managementUris;
        }

        public RabbitMQServiceInfo(string id, string uri)
            : this(id, uri, null)
        {
        }

        public RabbitMQServiceInfo(string id, string uri, string managementUri)
            : base(id, uri)
        {
            ManagementUri = managementUri;
        }

        public string ManagementUri { get; internal protected set; }

        public List<string> Uris { get; internal protected set; }

        public List<string> ManagementUris { get; internal protected set; }

        public string VirtualHost
        {
            get
            {
                return Info.Path;
            }
        }
    }
}
