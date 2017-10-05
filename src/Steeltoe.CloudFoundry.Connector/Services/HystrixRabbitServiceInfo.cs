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

using System.Collections.Generic;

namespace Steeltoe.CloudFoundry.Connector.Services
{
    public class HystrixRabbitServiceInfo : ServiceInfo
    {
        private RabbitServiceInfo rabbitInfo;
        private bool sslEnabled = false;

        public HystrixRabbitServiceInfo(string id, string uri, bool sslEnabled)
            : base(id)
        {
            this.sslEnabled = sslEnabled;
            rabbitInfo = new RabbitServiceInfo(id, uri);
        }

        public HystrixRabbitServiceInfo(string id, string uri, List<string> uris, bool sslEnabled)
            : base(id)
        {
            rabbitInfo = new RabbitServiceInfo(id, uri, null, uris, null);
            this.sslEnabled = sslEnabled;
        }

        public RabbitServiceInfo RabbitInfo
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
