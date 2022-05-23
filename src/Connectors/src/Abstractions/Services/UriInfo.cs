// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Net;

namespace Steeltoe.Connector.Services
{
    public class UriInfo
    {
        private readonly char[] _questionMark = new char[] { '?' };

        private readonly char[] _colon = new char[] { ':' };

        public UriInfo(string scheme, string host, int port, string username, string password, string path = null, string query = null)
        {
            Scheme = scheme;
            Host = host;
            Port = port;
            UserName = WebUtility.UrlEncode(username);
            Password = WebUtility.UrlEncode(password);
            Path = path;
            Query = query;

            UriString = MakeUri(scheme, host, port, username, password, path, query).ToString();
        }

        public UriInfo(string uristring)
        {
            var uri = MakeUri(uristring);
            if (uri != null)
            {
                Scheme = uri.Scheme;
                Host ??= uri.Host;

                Port = uri.Port;
                Path = GetPath(uri.PathAndQuery);
                Query = GetQuery(uri.PathAndQuery);

                var userinfo = GetUserInfo(uri.UserInfo);
                UserName = userinfo[0];
                Password = userinfo[1];
            }

            UriString = uristring;
        }

        public UriInfo(string uristring, string username, string password)
        {
            var uri = MakeUri(uristring);
            if (uri != null)
            {
                Scheme = uri.Scheme;
                Host ??= uri.Host;

                Port = uri.Port;
                Path = GetPath(uri.PathAndQuery);
                Query = GetQuery(uri.PathAndQuery);
            }

            UserName = WebUtility.UrlEncode(username);
            Password = WebUtility.UrlEncode(password);
            UriString = uristring;
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

        public Uri Uri => MakeUri(UriString);

        public override string ToString() => UriString;

        protected internal Uri MakeUri(string scheme, string host, int port, string username, string password, string path, string query)
        {
            var cleanedPath = path == null || path.StartsWith("/") ? path : $"/{path}";
            cleanedPath = query != null ? cleanedPath + "?" + query : cleanedPath;

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                var builder = new UriBuilder()
                {
                    Scheme = scheme,
                    Host = host,
                    Port = port,
                    UserName = WebUtility.UrlEncode(username),
                    Password = WebUtility.UrlEncode(password),
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
                if (uriString.StartsWith("jdbc") || uriString.Contains(";"))
                {
                    ConvertJdbcToUri(ref uriString);
                }

                return new Uri(uriString);
            }
            catch (Exception)
            {
                // URI parsing will fail if multiple (comma separated) hosts were provided...
                if (uriString.Contains(","))
                {
                    // Slide past the protocol
                    var splitUri = uriString.Split('/');

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

        protected internal void ConvertJdbcToUri(ref string uriString)
        {
            uriString = uriString.Replace("jdbc:", string.Empty).Replace(";", "&");
            if (!uriString.Contains("?"))
            {
                var firstAmp = uriString.IndexOf("&");

                // If there is an equals sign before any ampersands, it is likely a key was included for the db name.
                // Make the database name part of the path rather than query string if possible
                var firstEquals = uriString.IndexOf("=");
                if (firstEquals > 0 && (firstEquals < firstAmp || firstAmp == -1))
                {
                    var dbnameindex = uriString.IndexOf("databasename=", StringComparison.InvariantCultureIgnoreCase);
                    if (dbnameindex > 0)
                    {
                        uriString = uriString.Remove(dbnameindex, 13);

                        // recalculate the location of the first '&'
                        firstAmp = uriString.IndexOf("&");
                    }
                }

                if (firstAmp > 0)
                {
                    uriString = uriString.Substring(0, firstAmp) + _questionMark[0] + uriString.Substring(firstAmp + 1, uriString.Length - firstAmp - 1);
                }
            }
        }

        protected internal string GetPath(string pathAndQuery)
        {
            if (string.IsNullOrEmpty(pathAndQuery))
            {
                return null;
            }

            var split = pathAndQuery.Split(_questionMark);
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

            var split = pathAndQuery.Split(_questionMark);
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

            var split = userPass.Split(_colon);
            if (split.Length != 2)
            {
                throw new ArgumentException($"Bad user/password in URI: {userPass}");
            }

            return split;
        }
    }
}
