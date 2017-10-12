//
// Copyright 2017 the original author or authors.
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

using Microsoft.Extensions.Configuration;
using System;


namespace Steeltoe.Common.Discovery
{
    public class DiscoveryOptions
    {
        public DiscoveryOptions(IConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            Configure(config);

        }
        public DiscoveryOptions()
        {
            ClientType = DiscoveryClientType.UNKNOWN;
        }

        public string Type
        {
            get
            {
                return _type;
            }
        }

        protected string _type;
        public DiscoveryClientType ClientType
        {
            get
            {
                return (DiscoveryClientType)System.Enum.Parse(typeof(DiscoveryClientType), _type);
            }
            set
            {
                _type = System.Enum.GetName(typeof(DiscoveryClientType), value);
            }
        }

        protected IDiscoveryClientOptions _clientOptions;
        public IDiscoveryClientOptions ClientOptions
        {
            get
            {
                return _clientOptions;
            }
            set
            {
                _clientOptions = value;
            }

        }
        protected IDiscoveryRegistrationOptions _registrationOptions;
        public IDiscoveryRegistrationOptions RegistrationOptions
        {
            get
            {
                return _registrationOptions;
            }
            set
            {
                _registrationOptions = value;
            }
        }
        public virtual void Configure(IConfiguration config)
        {
        }

    }

    public enum DiscoveryClientType { EUREKA, UNKNOWN }


}
