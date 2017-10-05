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

using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System;
using System.Globalization;
using System.Net;
using System.Text;

namespace Steeltoe.CloudFoundry.Connector.Redis
{
    public class RedisCacheConnectorOptions : AbstractServiceConnectorOptions
    {
        public const string Default_Host = "localhost";
        public const int Default_Port = 6379;
        public static string Default_EndPoints = Default_Host + ":" + Default_Port;

        private const string REDIS_CLIENT_SECTION_PREFIX = "redis:client";

        public RedisCacheConnectorOptions()
            : base(',', Default_Separator)
        {
        }

        public RedisCacheConnectorOptions(IConfiguration config)
            : base(',', Default_Separator)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var section = config.GetSection(REDIS_CLIENT_SECTION_PREFIX);
            section.Bind(this);
        }

        // Configure either a single Host/Port or optionaly provide
        // a list of endpoints (ie. host1:port1,host2:port2)
        public string Host { get; set; } = Default_Host;

        public int Port { get; set; } = Default_Port;

        public string EndPoints { get; set; }

        public string Password { get; set; }

        public bool AllowAdmin { get; set; } = false;

        public string ClientName { get; set; }

        public int ConnectRetry { get; set; }

        public int ConnectTimeout { get; set; }

        public bool AbortOnConnectFail { get; set; } = true;

        public int KeepAlive { get; set; }

        public bool ResolveDns { get; set; } = false;

        public string ServiceName { get; set; }

        public bool Ssl { get; set; } = false;

        public string SslHost { get; set; }

        public string TieBreaker { get; set; }

        public int WriteBuffer { get; set; }

        // You can use this instead of configuring each option seperately
        // If a connection string is provided, the string will be used and
        // the optioons above will be ignored
        public string ConnectionString { get; set; }

        // This configuration option specfic to https://github.com/aspnet/Caching
        public string InstanceId { get; set; }

        // Add back in when https://github.com/aspnet/Caching updates to new StackExchange
        //public bool HighPrioritySocketThreads { get; set; }
        //public int ResponseTimeout { get; set; }
        //public int? DefaultDatabase { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(ConnectionString))
            {
                return ConnectionString;
            }

            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrEmpty(this.EndPoints))
            {
                string endpoints = EndPoints.Trim();
                sb.Append(endpoints);
                sb.Append(',');
            }
            else
            {
                sb.Append(this.Host + ":" + this.Port);
                sb.Append(',');
            }

            AddKeyValue(sb, "password", this.Password);
            AddKeyValue(sb, "allowAdmin", this.AllowAdmin);
            AddKeyValue(sb, "name", this.ClientName);

            if (this.ConnectRetry > 0)
            {
                AddKeyValue(sb, "connectRetry", this.ConnectRetry);
            }

            if (this.ConnectTimeout > 0)
            {
                AddKeyValue(sb, "connectTimeout", this.ConnectTimeout);
            }

            AddKeyValue(sb, "abortConnect", this.AbortOnConnectFail);

            if (this.KeepAlive > 0)
            {
                AddKeyValue(sb, "keepAlive", this.KeepAlive);
            }

            AddKeyValue(sb, "resolveDns", this.ResolveDns);
            AddKeyValue(sb, "serviceName", this.ServiceName);
            AddKeyValue(sb, "ssl", this.Ssl);
            AddKeyValue(sb, "sslHost", this.SslHost);
            AddKeyValue(sb, "tiebreaker", this.TieBreaker);

            if (this.WriteBuffer > 0)
            {
                AddKeyValue(sb, "writeBuffer", this.WriteBuffer);
            }

            // Trim ending ','
            sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }

        private static char[] COMMA = new char[] { ',' };

        internal void AddEndPoints(EndPointCollection result, string endpoints)
        {
            if (string.IsNullOrEmpty(endpoints))
            {
                return;
            }

            endpoints = endpoints.Trim();
            if (!string.IsNullOrEmpty(endpoints))
            {
                string[] points = endpoints.Split(COMMA);
                if (points.Length > 0)
                {
                    foreach (string point in points)
                    {
                        EndPoint p = TryParseEndPoint(point);
                        if (p != null)
                        {
                            result.Add(p);
                        }
                    }
                }
            }
        }

        //
        // Note: The code below lifted from StackExchange.Redis.Format {}
        //
        internal static EndPoint TryParseEndPoint(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return null;
            }

            string host;
            int port;
            int i = endpoint.IndexOf(':');
            if (i < 0)
            {
                host = endpoint;
                port = 0;
            }
            else
            {
                host = endpoint.Substring(0, i);
                var portAsString = endpoint.Substring(i + 1);
                if (string.IsNullOrEmpty(portAsString))
                {
                    return null;
                }

                if (int.TryParse(portAsString, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out port))
                {
                    return null;
                }
            }

            if (string.IsNullOrWhiteSpace(host))
            {
                return null;
            }

            return ParseEndPoint(host, port);
        }

        internal static EndPoint ParseEndPoint(string host, int port)
        {
            IPAddress ip;
            if (IPAddress.TryParse(host, out ip))
            {
                return new IPEndPoint(ip, port);
            }

            return new DnsEndPoint(host, port);
        }
    }
}
