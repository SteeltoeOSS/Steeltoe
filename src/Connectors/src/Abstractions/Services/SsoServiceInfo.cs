﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Connector.Services
{
    public class SsoServiceInfo : ServiceInfo
    {
        public SsoServiceInfo(string id, string clientId, string clientSecret, string domain)
            : base(id)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
            AuthDomain = domain;
        }

        public string ClientId { get; internal set; }

        public string ClientSecret { get; internal set; }

        public string AuthDomain { get; internal set; }
    }
}
