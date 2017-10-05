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

using System;

namespace Steeltoe.CloudFoundry.Connector.Services
{
    public abstract class UriServiceInfo : ServiceInfo
    {
        public UriInfo Info { get; internal protected set; }

        public UriServiceInfo(string id, string scheme, string host, int port, string username, string password, string path)
            : base(id)
        {
            Info = new UriInfo(scheme, host, port, username, password, path);
        }

        public UriServiceInfo(string id, string uriString, string username, string password)
            : base(id)
        {
            Info = new UriInfo(uriString, username, password);
        }

        public UriServiceInfo(string id, string uriString)
            : base(id)
        {
            Info = new UriInfo(uriString);
        }

        public string Uri
        {
            get
            {
                return Info.UriString;
            }
        }

        public string UserName
        {
            get
            {
                return Info.UserName;
            }
        }

        public string Password
        {
            get
            {
                return Info.Password;
            }
        }

        public string Host
        {
            get
            {
                return Info.Host;
            }
        }

        public int Port
        {
            get
            {
                return Info.Port;
            }
        }

        public string Path
        {
            get
            {
                return Info.Path;
            }
        }

        public string Query
        {
            get
            {
                return Info.Query;
            }
        }

        public string Scheme
        {
            get
            {
                return Info.Scheme;
            }
        }
    }
}
