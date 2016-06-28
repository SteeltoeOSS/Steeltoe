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

namespace SteelToe.CloudFoundry.Connector.Services
{
    public class UriInfo
    {
        public string Scheme { get; internal protected set; }
        public string Host { get; internal protected set; }
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

        public UriInfo(string scheme, string host, int port, string username, string password) :
            this(scheme, host, port, username, password, null, null)
        {

        }

        public UriInfo(string scheme, string host, int port, string username, string password, string path) :
             this(scheme, host, port, username, password, path, null)
        {

        }

        public UriInfo(string scheme, string host, int port, string username, string password, string path, string query)
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

 
        public UriInfo(string uristring)
        {
            this.UriString = uristring;

            Uri uri = MakeUri(uristring);
            Scheme = uri.Scheme;
            Host = uri.Host;
            Port = uri.Port;
            Path = GetPath(uri.PathAndQuery);
            Query = GetQuery(uri.PathAndQuery);
    

            string[] userinfo = GetUserInfo(uri.UserInfo);
            this.UserName = userinfo[0];
            this.Password = userinfo[1];
        }

        internal protected Uri MakeUri(string scheme, string host, int port, string username, string password, string path, string query)
        {
 
            string cleanedPath = path == null || path.StartsWith("/") ? path : "/" + path;
            cleanedPath = query != null ? cleanedPath + "?" + query : cleanedPath;

            if(!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
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
            } else
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

        internal protected Uri MakeUri(string uriString)
        {
            var builder = new UriBuilder(uriString);
            return builder.Uri;
        }

        private char[] QUESTION_MARK = new char[] { '?' };
        internal protected string GetPath(string pathAndQuery)
        {
            if (string.IsNullOrEmpty(pathAndQuery))
                return null;

            string[] split = pathAndQuery.Split(QUESTION_MARK);
            if (split.Length == 0)
                return null;
            return split[0].Substring(1);

        }

        internal protected string GetQuery(string pathAndQuery)
        {
            if (string.IsNullOrEmpty(pathAndQuery))
                return null;

            string[] split = pathAndQuery.Split(QUESTION_MARK);
            if (split.Length <= 1)
                return null;
            return split[1];
        }

        private char[] COLON = new char[] { ':' };
        internal protected string[] GetUserInfo(string userPass)
        {
     
            if (string.IsNullOrEmpty(userPass))
                return new string[2] { null, null };

            string[] split = userPass.Split(COLON);
            if (split.Length != 2)
            {
                throw new ArgumentException(
                    string.Format("Bad user/password in URI: {0}", userPass));
            }

            return split;

        }

        public override string ToString()
        {
            return UriString;
        }
    }
}
