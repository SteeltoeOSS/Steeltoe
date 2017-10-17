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

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Discovery;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Transport;
using System;
using System.Collections.Generic;
using T=System.Threading.Tasks;

namespace Steeltoe.Discovery.Eureka
{
    
    public class EurekaDiscoveryClient : DiscoveryClient, IDiscoveryClient
    {
        class EurekaHttpClientInternal : EurekaHttpClient
        {
            private IOptionsMonitor<EurekaClientOptions> _configOptions;
            protected override IEurekaClientConfig Config
            {
                get
                {
                    return _configOptions.CurrentValue;
                }
            }

            public EurekaHttpClientInternal(IOptionsMonitor<EurekaClientOptions> config, ILoggerFactory logFactory = null) :
            this(config, new Dictionary<string, string>(), logFactory)
            {
            }

            public EurekaHttpClientInternal(IOptionsMonitor<EurekaClientOptions> config, IDictionary<string, string> headers, ILoggerFactory logFactory = null)
            {
                if (config == null)
                {
                    throw new ArgumentNullException(nameof(config));
                }
                _config = null;
                _configOptions = config;
                base.Initialize(headers, logFactory);
            }

        }

        private IOptionsMonitor<EurekaClientOptions> _configOptions;
        private IServiceInstance _thisInstance;

        public override IEurekaClientConfig ClientConfig
        {
            get
            {
                return _configOptions.CurrentValue;
            }
        }
        public EurekaDiscoveryClient(
            IOptionsMonitor<EurekaClientOptions> clientConfig,
            IOptionsMonitor<EurekaInstanceOptions> instConfig,
            EurekaApplicationInfoManager appInfoManager,
            IEurekaHttpClient httpClient = null, 
            ILoggerFactory logFactory = null)
            : base(appInfoManager, logFactory)
        {
            _thisInstance = new ThisServiceInstance(instConfig);
            _configOptions = clientConfig;
            _httpClient = httpClient;

            if (_httpClient == null)
            {
                _httpClient = new EurekaHttpClientInternal(clientConfig, logFactory);
            }

            base.Initialize();

        }
        public IList<string> Services
        {
            get
            {
                return GetServices();
            }
        }

        public string Description
        {
            get
            {
                return "Spring Cloud Eureka Client";
            }
        }
        public IList<string> GetServices()
        {
            Applications applications = Applications;
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
        public IList<IServiceInstance> GetInstances(string serviceId)
        {
            IList<InstanceInfo> infos = GetInstancesByVipAddress(serviceId, false);
            List<IServiceInstance> instances = new List<IServiceInstance>();
            foreach (InstanceInfo info in infos)
            {
                _logger?.LogDebug("GetInstances returning: {0}", info.ToString());
                instances.Add(new EurekaServiceInstance(info));
            }
            return instances;
        }
        public IServiceInstance GetLocalServiceInstance()
        {
            return _thisInstance;
        }

        public override T.Task ShutdownAsync()
        {
            _appInfoManager.InstanceStatus = InstanceStatus.DOWN;
            return ShutdownAsync();
        }

    }

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

        public string Host
        {
            get
            {
                return InstConfig.GetHostName(false);
            }
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
                var _uri = new Uri(scheme + "://" + Host + ":" + uriPort.ToString());
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
