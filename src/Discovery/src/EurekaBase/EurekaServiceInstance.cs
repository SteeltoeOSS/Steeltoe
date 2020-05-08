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

using Steeltoe.Common.Discovery;
using Steeltoe.Discovery.Eureka.AppInfo;
using System;
using System.Collections.Generic;

namespace Steeltoe.Discovery.Eureka
{
    public class EurekaServiceInstance : IServiceInstance
    {
        private readonly InstanceInfo _info;

        public EurekaServiceInstance(InstanceInfo info)
        {
            _info = info;
        }

        public string GetHost()
        {
            return _info.HostName;
        }

        public bool IsSecure => _info.IsSecurePortEnabled;

        public IDictionary<string, string> Metadata => _info.Metadata;

        public int Port { get => IsSecure ? _info.SecurePort : _info.Port; }

        public string ServiceId => _info.AppName;

        public Uri Uri
        {
            get
            {
                var scheme = IsSecure ? "https" : "http";
                return new Uri(scheme + "://" + GetHost() + ":" + Port.ToString());
            }
        }

        public string Host => GetHost();

        public string InstanceId => _info.InstanceId;
    }
}
