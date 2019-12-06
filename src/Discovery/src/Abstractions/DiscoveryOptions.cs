﻿// Copyright 2017 the original author or authors.
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

using Microsoft.Extensions.Configuration;
using Steeltoe.Discovery;
using System;

namespace Steeltoe.Common.Discovery
{
    public enum DiscoveryClientType
    {
        EUREKA,
        UNKNOWN
    }

    public class DiscoveryOptions
    {
        protected string _type;

        public DiscoveryOptions(IConfiguration config)
            : this()
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

#pragma warning disable S1699 // Constructors should only call non-overridable methods
            Configure(config);
#pragma warning restore S1699 // Constructors should only call non-overridable methods
        }

        public DiscoveryOptions()
        {
            ClientType = DiscoveryClientType.UNKNOWN;
        }

        public string Type => _type;

        public DiscoveryClientType ClientType
        {
            get
            {
                if (string.IsNullOrEmpty(_type))
                {
                    return DiscoveryClientType.UNKNOWN;
                }

                return (DiscoveryClientType)System.Enum.Parse(typeof(DiscoveryClientType), _type);
            }

            set
            {
                _type = System.Enum.GetName(typeof(DiscoveryClientType), value);
            }
        }

        public IDiscoveryClientOptions ClientOptions { get; set; }

        public IDiscoveryRegistrationOptions RegistrationOptions { get; set; }

        public virtual void Configure(IConfiguration config)
        {
            ClientType = DiscoveryClientType.UNKNOWN;
        }
    }
}
