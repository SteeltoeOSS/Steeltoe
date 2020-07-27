// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
                var scheme = IsSecure ? "https" : "http";
                return new Uri(scheme + "://" + GetHost() + ":" + Port.ToString());
            }
        }

        public string Host => GetHost();

        public string InstanceId => _info.InstanceId;
    }
}
