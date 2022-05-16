// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;

namespace Steeltoe.Connector.Services
{
    public abstract class UriServiceInfo : ServiceInfo
    {
        protected UriServiceInfo(string id, string scheme, string host, int port, string username, string password, string path)
            : base(id)
        {
            Info = new UriInfo(scheme, host, port, username, password, path);
        }

        protected UriServiceInfo(string id, string uriString, string username, string password)
            : base(id)
        {
            Info = new UriInfo(uriString, username, password);
        }

        protected UriServiceInfo(string id, string uriString)
            : base(id)
        {
            Info = new UriInfo(uriString);
        }

        public UriInfo Info { get; internal protected set; }

        public string Uri => Info.UriString;

        public string UserName => WebUtility.UrlDecode(Info.UserName);

        public string Password => WebUtility.UrlDecode(Info.Password);

        public string Host => Info.Host;

        public string[] Hosts => Info.Hosts;

        public int Port => Info.Port;

        public string Path => Info.Path;

        public string Query => Info.Query;

        public string Scheme => Info.Scheme;
    }
}
