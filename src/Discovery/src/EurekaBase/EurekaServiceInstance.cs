﻿// Licensed to the .NET Foundation under one or more agreements.
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
        private InstanceInfo _info;

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
                string scheme = IsSecure ? "https" : "http";
                return new Uri(scheme + "://" + GetHost() + ":" + Port.ToString());
            }
        }

        public string Host => GetHost();

        public string InstanceId => _info.InstanceId;
    }
}
