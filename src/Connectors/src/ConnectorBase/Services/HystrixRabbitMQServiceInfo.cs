// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.CloudFoundry.Connector.Services
{
    public class HystrixRabbitMQServiceInfo : ServiceInfo
    {
        private RabbitMQServiceInfo rabbitInfo;
        private bool sslEnabled = false;

        public HystrixRabbitMQServiceInfo(string id, string uri, bool sslEnabled)
            : base(id)
        {
            this.sslEnabled = sslEnabled;
            rabbitInfo = new RabbitMQServiceInfo(id, uri);
        }

        public HystrixRabbitMQServiceInfo(string id, string uri, List<string> uris, bool sslEnabled)
            : base(id)
        {
            rabbitInfo = new RabbitMQServiceInfo(id, uri, null, uris, null);
            this.sslEnabled = sslEnabled;
        }

        public RabbitMQServiceInfo RabbitInfo
        {
            get
            {
                return rabbitInfo;
            }
        }

        public string Scheme
        {
            get
            {
                return rabbitInfo.Scheme;
            }
        }

        public string Query
        {
            get
            {
                return rabbitInfo.Query;
            }
        }

        public string Path
        {
            get
            {
                return rabbitInfo.Path;
            }
        }

        public string Uri
        {
            get
            {
                return rabbitInfo.Uri;
            }
        }

        public List<string> Uris
        {
            get
            {
                return rabbitInfo.Uris;
            }
        }

        public string Host
        {
            get
            {
                return rabbitInfo.Host;
            }
        }

        public int Port
        {
            get
            {
                return rabbitInfo.Port;
            }
        }

        public string UserName
        {
            get
            {
                return rabbitInfo.UserName;
            }
        }

        public string Password
        {
            get
            {
                return rabbitInfo.Password;
            }
        }

        public bool IsSslEnabled
        {
            get
            {
                return this.sslEnabled;
            }
        }
    }
}
