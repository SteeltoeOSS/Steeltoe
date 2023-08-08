// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Steeltoe.Common;

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
            ArgumentGuard.NotNull(keyword);
            AssertIsKnownKeyword(keyword);

            if (string.Equals(keyword, KnownKeywords.Url, StringComparison.OrdinalIgnoreCase))
            {
                return ConnectionString;
            }

            if (_settings.TryGetValue(keyword, out string? value))
            {
                return value;
            }

            return null;
        }
        set
        {
            ArgumentGuard.NotNull(keyword);
            AssertIsKnownKeyword(keyword);

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

        if (_settings.TryGetValue(KnownKeywords.Port, out string? portText) && int.TryParse(portText, out int port))
        {
            builder.Port = port;
        }

        if (_settings.TryGetValue(KnownKeywords.VirtualHost, out string? virtualHost))
        {
            builder.Path = Uri.EscapeDataString(virtualHost);
        }

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
        }
    }

    private static void AssertIsKnownKeyword(string keyword)
    {
        if (!KnownKeywords.Exists(keyword))
        {
            throw new ArgumentException($"Keyword not supported: '{keyword}'.", nameof(keyword));
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

        private static readonly ISet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
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
