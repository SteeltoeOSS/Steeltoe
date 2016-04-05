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

using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.Logging;
using SteelToe.Discovery.Eureka;
using SteelToe.Discovery.Eureka.AppInfo;
using System;
using System.Collections.Generic;


namespace SteelToe.Discovery.Client
{
    public class EurekaDiscoveryClient : IDiscoveryClient
    {
        internal IEurekaClientConfig ClientConfig;
        internal IEurekaInstanceConfig InstConfig;
        internal IEurekaClient Client;

        internal EurekaDiscoveryClient(EurekaClientOptions clientOptions, EurekaInstanceOptions instOptions, IApplicationLifetime lifeCycle = null, ILoggerFactory logFactory = null)
        {
            if (clientOptions == null)
            {
                throw new ArgumentNullException(nameof(clientOptions));
            }

            ClientConfig = clientOptions;
            InstConfig = instOptions;

            if (InstConfig == null)
            {
                DiscoveryManager.Instance.Initialize(ClientConfig, logFactory);
            }
            else
            {
                ConfigureInstanceIfNeeded(InstConfig);
                DiscoveryManager.Instance.Initialize(ClientConfig, InstConfig, logFactory);
            }

            if (lifeCycle != null)
            {
                lifeCycle.ApplicationStopping.Register(() => { ShutdownAsync(); });
            }

            Client = DiscoveryManager.Instance.Client;

        }


        public string Description
        {
            get
            {
                return "Spring Cloud Eureka Discovery Client";
            }
        }

        public IList<string> Services
        {
            get
            {
                return GetServices();
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

        protected virtual internal IList<string> GetServices()
        {
            Applications applications = Client.Applications;
            if (applications == null)
            {
                return new List<string>();
            }
            IList<Application> registered = applications.GetRegisteredApplications();
            List<string> names = new List<string>();
            foreach (Application app in registered)
            {
                if (app.Instances.Count == 0)
                {
                    continue;
                }

                names.Add(app.Name.ToLowerInvariant());

            }
            return names;
        }

        protected virtual internal void ConfigureInstanceIfNeeded(IEurekaInstanceConfig instConfig)
        {
            if (string.IsNullOrEmpty(instConfig.AppName))
            {
                instConfig.AppName = "unknown";
            }

            if (string.IsNullOrEmpty(instConfig.InstanceId))
            {
                var hostName = instConfig.GetHostName(false);
                var appName = instConfig.AppName;
                var index = instConfig.NonSecurePort.ToString();
                instConfig.InstanceId = hostName + ":" + appName + ":" + index;
            }

            if (string.IsNullOrEmpty(instConfig.VirtualHostName))
            {
                instConfig.VirtualHostName = instConfig.AppName;
            }

        }

        public void ShutdownAsync()
        {
            ApplicationInfoManager.Instance.InstanceStatus = InstanceStatus.DOWN;
            Client.ShutdownAsyc();
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
