// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;

namespace Steeltoe.Connector.Services
{
    public class HystrixRabbitMQServiceInfo : ServiceInfo
    {
        public HystrixRabbitMQServiceInfo(string id, string uri, bool sslEnabled)
        : base(id)
        {
            IsSslEnabled = sslEnabled;
            RabbitInfo = new RabbitMQServiceInfo(id, uri);
        }

        public HystrixRabbitMQServiceInfo(string id, string uri, List<string> uris, bool sslEnabled)
            : base(id)
        {
            RabbitInfo = new RabbitMQServiceInfo(id, uri, null, uris, null);
            IsSslEnabled = sslEnabled;
        }

        public RabbitMQServiceInfo RabbitInfo { get; }

        public string Scheme => RabbitInfo.Scheme;

        public string Query => RabbitInfo.Query;

        public string Path => RabbitInfo.Path;

        public string Uri => RabbitInfo.Uri;

        public List<string> Uris => RabbitInfo.Uris;

        public string Host => RabbitInfo.Host;

        public int Port => RabbitInfo.Port;

        public string UserName => RabbitInfo.UserName;

        public string Password => RabbitInfo.Password;

        public bool IsSslEnabled { get; } = false;
    }
}
