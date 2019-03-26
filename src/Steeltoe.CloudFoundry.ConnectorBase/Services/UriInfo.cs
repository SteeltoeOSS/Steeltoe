// Copyright 2017 the original author or authors.
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Steeltoe.CloudFoundry.Connector.Services
{
    public class UriInfo
    {
        private char[] questionMark = new char[] { '?' };

        private char[] colon = new char[] { ':' };

        public UriInfo(string scheme, string host, int port, string username, string password, string path = null, string query = null)
        {
            Scheme = scheme;
            Host = host;
            Port = port;
            UserName = username;
            Password = password;
            Path = path;
            Query = query;

            UriString = MakeUri(scheme, host, port, username, password, path, query).ToString();
        }

        public UriInfo(string uristring, bool urlEncodedCredentials = false)
        {
            this.UriString = uristring;

            Uri uri = MakeUri(uristring);
            if (uri != null)
            {
                Scheme = uri.Scheme;
                if (Host == null)
                {
                    Host = uri.Host;
                }

                Port = uri.Port;
                Path = GetPath(uri.PathAndQuery);
                Query = GetQuery(uri.PathAndQuery);

                string[] userinfo = GetUserInfo(uri.UserInfo);

                if (urlEncodedCredentials)
                {
                    UserName = WebUtility.UrlDecode(userinfo[0]);
                    Password = WebUtility.UrlDecode(userinfo[1]);
                }
                else
                {
                    UserName = userinfo[0];
                    Password = userinfo[1];
                }
            }
        }

        public UriInfo(string uristring, string username, string password)
        {
            this.UriString = uristring;

            Uri uri = MakeUri(uristring);
            if (uri != null)
            {
                Scheme = uri.Scheme;
                if (Host == null)
                {
                    Host = uri.Host;
                }

                Port = uri.Port;
                Path = GetPath(uri.PathAndQuery);
                Query = GetQuery(uri.PathAndQuery);
            }

            this.UserName = username;
            this.Password = password;
        }

        public string Scheme { get; internal protected set; }

        public string Host { get; internal protected set; }

        public string[] Hosts { get; internal protected set; }

        public int Port { get; internal protected set; }

        public string UserName { get; internal protected set; }

        public string Password { get; internal protected set; }

        public string Path { get; internal protected set; }

        public string Query { get; internal protected set; }

        public string UriString { get; internal protected set; }

        public Uri Uri
        {
            get
            {
                return MakeUri(UriString);
            }
        }

        public override string ToString()
        {
            return UriString;
        }

        protected internal Uri MakeUri(string scheme, string host, int port, string username, string password, string path, string query)
        {
            string cleanedPath = path == null || path.StartsWith("/") ? path : "/" + path;
            cleanedPath = query != null ? cleanedPath + "?" + query : cleanedPath;

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                var builder = new UriBuilder()
                {
                    Scheme = scheme,
                    Host = host,
                    Port = port,
                    UserName = username,
                    Password = password,
                    Path = cleanedPath
                };
                return builder.Uri;
            }
            else
            {
                var builder = new UriBuilder()
                {
                    Scheme = scheme,
                    Host = host,
                    Port = port,
                    Path = cleanedPath
                };
                return builder.Uri;
            }
        }

        protected internal Uri MakeUri(string uriString)
        {
            try
            {
                return new Uri(uriString);
            }
            catch (Exception)
            {
                // URI parsing will fail if multiple (comma separated) hosts were provided...
                if (uriString.Contains(","))
                {
                    // Slide past the protocol
                    var splitUri = UriString.Split('/');

                    // get the host list (and maybe credentials)
                    // -- pre-emptively set it as the Host property rather than a local variable
                    //      since the connector is likely to expect this format here anyway
                    var credentialAndHost = splitUri[2];

                    // skip over credentials if they're present
                    Host = credentialAndHost.Contains('@') ? credentialAndHost.Split('@')[1] : credentialAndHost;

                    // add the hosts to a separate property for reconstruction later
                    Hosts = Host.Split(',');

                    // swap all the hosts out with a placeholder so we can parse the rest of the info
                    return new Uri(uriString.Replace(Host, "multipleHostsDetected"));
                }

                return null;
            }
        }

        protected internal string GetPath(string pathAndQuery)
        {
            if (string.IsNullOrEmpty(pathAndQuery))
            {
                return null;
            }

            string[] split = pathAndQuery.Split(questionMark);
            if (split.Length == 0)
            {
                return null;
            }

            return split[0].Substring(1);
        }

        protected internal string GetQuery(string pathAndQuery)
        {
            if (string.IsNullOrEmpty(pathAndQuery))
            {
                return null;
            }

            string[] split = pathAndQuery.Split(questionMark);
            if (split.Length <= 1)
            {
                return null;
            }

            return split[1];
        }

        protected internal string[] GetUserInfo(string userPass)
        {
            if (string.IsNullOrEmpty(userPass))
            {
                return new string[2] { null, null };
            }

            string[] split = userPass.Split(colon);
            if (split.Length != 2)
            {
                throw new ArgumentException(
                    string.Format("Bad user/password in URI: {0}", userPass));
            }

            return split;
        }
    }
}
