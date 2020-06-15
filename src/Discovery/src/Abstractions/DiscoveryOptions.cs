// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
