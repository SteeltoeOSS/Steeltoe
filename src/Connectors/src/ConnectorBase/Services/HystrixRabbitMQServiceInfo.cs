// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.CloudFoundry.Connector.Services
{
    public class HystrixRabbitMQServiceInfo : ServiceInfo
    {
        private RabbitMQServiceInfo _rabbitInfo;
        private bool _sslEnabled = false;

        public HystrixRabbitMQServiceInfo(string id, string uri, bool sslEnabled)
            : base(id)
        {
            this._sslEnabled = sslEnabled;
            _rabbitInfo = new RabbitMQServiceInfo(id, uri);
        }

        public HystrixRabbitMQServiceInfo(string id, string uri, List<string> uris, bool sslEnabled)
            : base(id)
        {
            _rabbitInfo = new RabbitMQServiceInfo(id, uri, null, uris, null);
            this._sslEnabled = sslEnabled;
        }

        public RabbitMQServiceInfo RabbitInfo
        {
            get
            {
                return _rabbitInfo;
            }
        }

        public string Scheme
        {
            get
            {
                return _rabbitInfo.Scheme;
            }
        }

        public string Query
        {
            get
            {
                return _rabbitInfo.Query;
            }
        }

        public string Path
        {
            get
            {
                return _rabbitInfo.Path;
            }
        }

        public string Uri
        {
            get
            {
                return _rabbitInfo.Uri;
            }
        }

        public List<string> Uris
        {
            get
            {
                return _rabbitInfo.Uris;
            }
        }

        public string Host
        {
            get
            {
                return _rabbitInfo.Host;
            }
        }

        public int Port
        {
            get
            {
                return _rabbitInfo.Port;
            }
        }

        public string UserName
        {
            get
            {
                return _rabbitInfo.UserName;
            }
        }

        public string Password
        {
            get
            {
                return _rabbitInfo.Password;
            }
        }

        public bool IsSslEnabled
        {
            get
            {
                return this._sslEnabled;
            }
        }
    }
}
