// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Connector.Services
{
    public class EurekaServiceInfo : UriServiceInfo
    {
        public EurekaServiceInfo(string id, string uri, string clientId, string clientSecret, string tokenUri)
            : base(id, uri)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
            TokenUri = tokenUri;
        }

        public string ClientId { get; internal set; }

        public string ClientSecret { get; internal set; }

        public string TokenUri { get; internal set; }
    }
}
