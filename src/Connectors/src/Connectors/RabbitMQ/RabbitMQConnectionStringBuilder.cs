// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Web;
using Microsoft.AspNetCore.Http;

namespace Steeltoe.Connectors.RabbitMQ;

/// <summary>
/// Provides a connection string builder for RabbitMQ URLs. The format is defined at: https://www.rabbitmq.com/uri-spec.html.
/// </summary>
internal sealed class RabbitMQConnectionStringBuilder : IConnectionStringBuilder
{
    private readonly Dictionary<string, string> _settings = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    [AllowNull]
    public string ConnectionString
    {
        get => ToConnectionString();
        set => FromConnectionString(value);
    }

    /// <inheritdoc />
    public object? this[string keyword]
    {
        get
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(keyword);

            if (string.Equals(keyword, KnownKeywords.Url, StringComparison.OrdinalIgnoreCase))
            {
                return ConnectionString;
            }

            // Allow getting unknown keyword. We don't pretend to know all valid query string parameters.
            return _settings.GetValueOrDefault(keyword);
        }
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(keyword);

            if (string.Equals(keyword, KnownKeywords.Url, StringComparison.OrdinalIgnoreCase))
            {
                ConnectionString = value?.ToString();
            }
            else
            {
                string? stringValue = value?.ToString();

                if (stringValue == null)
                {
                    _settings.Remove(keyword);
                }
                else
                {
                    // Allow setting unknown keyword. We don't pretend to know all valid query string parameters.
                    _settings[keyword] = stringValue;
                }
            }
        }
    }

    private string ToConnectionString()
    {
        if (!_settings.TryGetValue(KnownKeywords.Host, out string? host))
        {
            return string.Empty;
        }

        var builder = new UriBuilder
        {
            Scheme = _settings.TryGetValue(KnownKeywords.UseTls, out string? useTlsText) && bool.TryParse(useTlsText, out bool useTls) && useTls
                ? "amqps"
                : "amqp",
            Host = host
        };

        if (_settings.TryGetValue(KnownKeywords.Username, out string? username))
        {
            builder.UserName = Uri.EscapeDataString(username);

            if (_settings.TryGetValue(KnownKeywords.Password, out string? password))
            {
                builder.Password = Uri.EscapeDataString(password);
            }
        }

        if (_settings.TryGetValue(KnownKeywords.Port, out string? portText) && int.TryParse(portText, CultureInfo.InvariantCulture, out int port))
        {
            builder.Port = port;
        }

        if (_settings.TryGetValue(KnownKeywords.VirtualHost, out string? virtualHost))
        {
            builder.Path = Uri.EscapeDataString(virtualHost);
        }

        var queryString = default(QueryString);

        foreach ((string name, string value) in _settings.Where(pair => !KnownKeywords.Exists(pair.Key)))
        {
            queryString = queryString.Add(name, value);
        }

        builder.Query = queryString.Value;

        return builder.Uri.AbsoluteUri;
    }

    private void FromConnectionString(string? connectionString)
    {
        _settings.Clear();

        if (!string.IsNullOrEmpty(connectionString))
        {
            var uri = new Uri(connectionString);

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                string[] parts = uri.UserInfo.Split(':', 2);

                _settings[KnownKeywords.Username] = Uri.UnescapeDataString(parts[0]);

                if (parts.Length == 2)
                {
                    _settings[KnownKeywords.Password] = Uri.UnescapeDataString(parts[1]);
                }
            }

            _settings[KnownKeywords.UseTls] = uri.Scheme == "amqps" ? bool.TrueString : bool.FalseString;
            _settings[KnownKeywords.Host] = uri.Host;

            if (uri.Port != -1)
            {
                _settings[KnownKeywords.Port] = uri.Port.ToString(CultureInfo.InvariantCulture);
            }

            if (uri.AbsolutePath.StartsWith('/') && uri.AbsolutePath.Length > 1)
            {
                _settings[KnownKeywords.VirtualHost] = Uri.UnescapeDataString(uri.AbsolutePath[1..]);
            }

            NameValueCollection queryString = HttpUtility.ParseQueryString(uri.Query);

            foreach (string remainingKeyword in queryString.AllKeys.Where(key => key != null && !KnownKeywords.Exists(key)).Cast<string>())
            {
                string? value = queryString.Get(remainingKeyword);

                if (value != null)
                {
                    _settings[remainingKeyword] = value;
                }
            }
        }
    }

    private static class KnownKeywords
    {
        public const string UseTls = "useTls";
        public const string Host = "host";
        public const string Port = "port";
        public const string Username = "username";
        public const string Password = "password";
        public const string VirtualHost = "virtualHost";
        public const string Url = "url";

        private static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
        {
            UseTls,
            Host,
            Port,
            Username,
            Password,
            VirtualHost,
            Url
        };

        public static bool Exists(string keyword)
        {
            return All.Contains(keyword);
        }
    }
}
