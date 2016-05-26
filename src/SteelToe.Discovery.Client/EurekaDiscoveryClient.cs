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

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using SteelToe.Discovery.Eureka;
using SteelToe.Discovery.Eureka.AppInfo;
using SteelToe.Discovery.Eureka.Transport;
using System;
using System.Collections.Generic;


namespace SteelToe.Discovery.Client
{
    public class EurekaDiscoveryClient : EurekaDiscoveryClientBase, IDiscoveryClient
    {

        internal protected EurekaDiscoveryClient(EurekaClientOptions clientOptions, EurekaInstanceOptions instOptions, IEurekaHttpClient httpClient, IApplicationLifetime lifeCycle = null, ILoggerFactory logFactory = null)
         : base(clientOptions, instOptions, httpClient, lifeCycle, logFactory) {
        }

        internal protected EurekaDiscoveryClient(EurekaClientOptions clientOptions, EurekaInstanceOptions instOptions,  IApplicationLifetime lifeCycle = null, ILoggerFactory logFactory = null) 
            : base(clientOptions, instOptions, null, lifeCycle, logFactory)
        {
        }


        public override string Description
        {
            get
            {
                return "Spring Cloud Eureka Client";
            }
        }

        public IList<IServiceInstance> GetInstances(string serviceId)
        {
            IList<InstanceInfo> infos = Client.GetInstancesByVipAddress(serviceId, false);
            List<IServiceInstance> instances = new List<IServiceInstance>();
            foreach (InstanceInfo info in infos)
            {
                instances.Add(new EurekaServiceInstance(info));
            }
            return instances;
        }

        public IServiceInstance GetLocalServiceInstance()
        {
            return new ThisServiceInstance(InstConfig.GetHostName(false),
                InstConfig.SecurePortEnabled, InstConfig.MetadataMap, InstConfig.NonSecurePort, InstConfig.AppName);
        }

    }

    public class ThisServiceInstance : IServiceInstance
    {
        private string _host;
        private bool _isSecure;
        private IDictionary<string, string> _metadata;
        private int _port;
        private string _serviceId;
        private Uri _uri;

        public ThisServiceInstance(string host, bool isSecure, IDictionary<string, string> metadata, int port, string serviceId)
        {
            this._host = host;
            this._isSecure = isSecure;
            this._metadata = metadata;
            this._port = port;
            this._serviceId = serviceId;
            string scheme = isSecure ? "https" : "http";
            _uri = new Uri(scheme + "://" + host + ":" + port.ToString());
        }
        public string Host
        {
            get
            {
                return _host;
            }
        }

        public bool IsSecure
        {
            get
            {
                return _isSecure;
            }
        }

        public IDictionary<string, string> Metadata
        {
            get
            {
                return _metadata;
            }
        }

        public int Port
        {
            get
            {
                return _port;
            }
        }

        public string ServiceId
        {
            get
            {
                return _serviceId;
            }
        }

        public Uri Uri
        {
            get
            {
                return _uri;
            }
        }
    }

    public class EurekaServiceInstance : IServiceInstance
    {
        private InstanceInfo _info;
        internal EurekaServiceInstance(InstanceInfo info)
        {
            this._info = info;
        }
        public string Host
        {
            get
            {
                return _info.HostName;
            }
        }

        public bool IsSecure
        {
            get
            {
                return _info.IsSecurePortEnabled;
            }
        }

        public IDictionary<string, string> Metadata
        {
            get
            {
                return _info.Metadata;
            }
        }

        public int Port
        {
            get
            {
                if (IsSecure)
                {
                    return _info.SecurePort;
                }
                return _info.Port;
            }
        }

        public string ServiceId
        {
            get
            {
                return _info.AppName;
            }
        }

        public Uri Uri
        {
            get
            {
                string scheme = IsSecure ? "https" : "http";
                return new Uri(scheme + "://" + Host + ":" + Port.ToString());
            }
        }
    }
}
