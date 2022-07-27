// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Reflection;
using System;
using System.Globalization;
using System.Net;
using System.Text;

namespace Steeltoe.Connector.Redis;

public class RedisCacheConnectorOptions : AbstractServiceConnectorOptions
{
    private const string Default_Host = "localhost";
    private const int Default_Port = 6379;
    private const string RedisClientSectionPrefix = "redis:client";
    private readonly bool _cloudFoundryConfigFound = false;

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

        var section = config.GetSection(RedisClientSectionPrefix);
        section.Bind(this);

        _cloudFoundryConfigFound = config.HasCloudFoundryServiceConfigurations();
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

    public int SyncTimeout { get; set; }

    // You can use this instead of configuring each option seperately
    // If a connection string is provided, the string will be used and
    // the options above will be ignored
    public string ConnectionString { get; set; }

    // This configuration option specfic to https://github.com/aspnet/Caching
    public string InstanceName { get; set; }

    // TODO: Add back in when https://github.com/aspnet/Caching updates to new StackExchange
    // public bool HighPrioritySocketThreads { get; set; }
    // public int ResponseTimeout { get; set; }
    // public int? DefaultDatabase { get; set; }
    public override string ToString()
    {
        if (!string.IsNullOrEmpty(ConnectionString) && !_cloudFoundryConfigFound)
        {
            return ConnectionString;
        }

        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(EndPoints))
        {
            var endpoints = EndPoints.Trim();
            sb.Append(endpoints);
            sb.Append(',');
        }
        else
        {
            sb.Append(Host + ":" + Port);
            sb.Append(',');
        }

        AddKeyValue(sb, "password", Password);
        AddKeyValue(sb, "allowAdmin", AllowAdmin);
        AddKeyValue(sb, "name", ClientName);

        if (ConnectRetry > 0)
        {
            AddKeyValue(sb, "connectRetry", ConnectRetry);
        }

        if (ConnectTimeout > 0)
        {
            AddKeyValue(sb, "connectTimeout", ConnectTimeout);
        }

        AddKeyValue(sb, "abortConnect", AbortOnConnectFail);

        if (KeepAlive > 0)
        {
            AddKeyValue(sb, "keepAlive", KeepAlive);
        }

        AddKeyValue(sb, "resolveDns", ResolveDns);
        AddKeyValue(sb, "serviceName", ServiceName);
        AddKeyValue(sb, "ssl", Ssl);
        AddKeyValue(sb, "sslHost", SslHost);
        AddKeyValue(sb, "tiebreaker", TieBreaker);

        if (WriteBuffer > 0)
        {
            AddKeyValue(sb, "writeBuffer", WriteBuffer);
        }

        if (SyncTimeout > 0)
        {
            AddKeyValue(sb, "syncTimeout", SyncTimeout);
        }

        // Trim ending ','
        sb.Remove(sb.Length - 1, 1);
        return sb.ToString();
    }

    /// <summary>
    /// Get a Redis configuration object for use with Microsoft.Extensions.Caching.Redis
    /// </summary>
    /// <param name="optionsType">Expects Microsoft.Extensions.Caching.Redis.RedisCacheOptions</param>
    /// <returns>This object typed as RedisCacheOptions</returns>
    public object ToMicrosoftExtensionObject(Type optionsType)
    {
        var msftConnection = Activator.CreateInstance(optionsType);
        msftConnection.GetType().GetProperty("Configuration").SetValue(msftConnection, ToString());
        msftConnection.GetType().GetProperty("InstanceName").SetValue(msftConnection, InstanceName);

        return msftConnection;
    }

    /// <summary>
    /// Get a Redis configuration object for use with StackExchange.Redis
    /// </summary>
    /// <param name="optionsType">Expects StackExchange.Redis.ConfigurationOptions</param>
    /// <returns>This object typed as ConfigurationOptions</returns>
    /// <remarks>Includes comma in password detection and workaround for https://github.com/SteeltoeOSS/Connectors/issues/10 </remarks>
    public object ToStackExchangeObject(Type optionsType)
    {
        var stackObject = Activator.CreateInstance(optionsType);

        // to remove this comma workaround, follow up on https://github.com/StackExchange/StackExchange.Redis/issues/680
        var tempPassword = Password;
        var resetPassword = false;
        if (Password?.Contains(",") == true)
        {
            Password = string.Empty;
            resetPassword = true;
        }

        // this return is effectively "StackExchange.Redis.ConfigurationOptions.Parse(this.ToString())"
        var config = optionsType.GetMethod("Parse", new Type[] { typeof(string) })
            .Invoke(stackObject, new object[] { ToString() });

        if (resetPassword)
        {
            ReflectionHelpers.TrySetProperty(config, "Password", tempPassword);
        }

        return config;
    }

    // internal void AddEndPoints(EndPointCollection result, string endpoints)
    // {
    //    if (string.IsNullOrEmpty(endpoints))
    //    {
    //        return;
    //    }
    //    endpoints = endpoints.Trim();
    //    if (!string.IsNullOrEmpty(endpoints))
    //    {
    //        string[] points = endpoints.Split(comma);
    //        if (points.Length > 0)
    //        {
    //            foreach (string point in points)
    //            {
    //                EndPoint p = TryParseEndPoint(point);
    //                if (p != null)
    //                {
    //                    result.Add(p);
    //                }
    //            }
    //        }
    //    }
    // }

    // Note: The code below lifted from StackExchange.Redis.Format {}
    internal static EndPoint TryParseEndPoint(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return null;
        }

        string host;
        int port;
        var i = endpoint.IndexOf(':');
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
        if (IPAddress.TryParse(host, out var ip))
        {
            return new IPEndPoint(ip, port);
        }

        return new DnsEndPoint(host, port);
    }
}