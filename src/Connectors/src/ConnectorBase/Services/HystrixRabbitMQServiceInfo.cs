// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.CloudFoundry.Connector.Services
{
    public class HystrixRabbitMQServiceInfo : ServiceInfo
    {
        public HystrixRabbitMQServiceInfo(string id, string uri, bool sslEnabled)
            : base(id)
        {
            this.IsSslEnabled = sslEnabled;
            RabbitInfo = new RabbitMQServiceInfo(id, uri);
        }

        public HystrixRabbitMQServiceInfo(string id, string uri, List<string> uris, bool sslEnabled)
            : base(id)
        {
            RabbitInfo = new RabbitMQServiceInfo(id, uri, null, uris, null);
            this.IsSslEnabled = sslEnabled;
        }

        public RabbitMQServiceInfo RabbitInfo { get; }

        public string Scheme
        {
            get
            {
                return RabbitInfo.Scheme;
            }
        }

        public string Query
        {
            get
            {
                return RabbitInfo.Query;
            }
        }

        public string Path
        {
            get
            {
                return RabbitInfo.Path;
            }
        }

        public string Uri
        {
            get
            {
                return RabbitInfo.Uri;
            }
        }

        public List<string> Uris
        {
            get
            {
                return RabbitInfo.Uris;
            }
        }

        public string Host
        {
            get
            {
                return RabbitInfo.Host;
            }
        }

        public int Port
        {
            get
            {
                return RabbitInfo.Port;
            }
        }

        public string UserName
        {
            get
            {
                return RabbitInfo.UserName;
            }
        }

        public string Password
        {
            get
            {
                return RabbitInfo.Password;
            }
        }

        public bool IsSslEnabled { get; } = false;
    }
}
