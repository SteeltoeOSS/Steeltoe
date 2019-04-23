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

using Microsoft.Extensions.Options;
using Steeltoe.Common.Discovery;
using System;
using System.Collections.Generic;

namespace Steeltoe.Discovery.Eureka
{
    public class ThisServiceInstance : IServiceInstance
    {
        private IOptionsMonitor<EurekaInstanceOptions> _instConfig;

        private EurekaInstanceOptions InstConfig
        {
            get
            {
                return _instConfig.CurrentValue;
            }
        }

        public ThisServiceInstance(IOptionsMonitor<EurekaInstanceOptions> instConfig)
        {
            _instConfig = instConfig;
        }

        public string GetHost()
        {
            return InstConfig.GetHostName(false);
        }

        public bool IsSecure
        {
            get
            {
                return InstConfig.SecurePortEnabled;
            }
        }

        public IDictionary<string, string> Metadata
        {
            get
            {
                return InstConfig.MetadataMap;
            }
        }

        public int Port
        {
            get
            {
                return (InstConfig.NonSecurePort == -1) ? EurekaInstanceConfig.Default_NonSecurePort : InstConfig.NonSecurePort;
            }
        }

        public int SecurePort
        {
            get
            {
                return (InstConfig.SecurePort == -1) ? EurekaInstanceConfig.Default_SecurePort : InstConfig.SecurePort;
            }
        }

        public string ServiceId
        {
            get
            {
                return InstConfig.AppName;
            }
        }

        public Uri Uri
        {
            get
            {
                string scheme = IsSecure ? "https" : "http";
                int uriPort = IsSecure ? SecurePort : Port;
                var uri = new Uri(scheme + "://" + GetHost() + ":" + uriPort.ToString());
                return uri;
            }
        }

        public string Host => GetHost();
    }
}
