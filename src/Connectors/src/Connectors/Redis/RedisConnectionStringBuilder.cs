// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Steeltoe.Common;

namespace Steeltoe.Connectors.Redis;

/// <summary>
/// Provides a connection string builder for Redis. The format is defined at: https://stackexchange.github.io/StackExchange.Redis/Configuration.html.
/// </summary>
internal sealed class RedisConnectionStringBuilder : IConnectionStringBuilder
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

            // Allow getting unknown keyword, if it was set earlier. We don't pretend to know all valid keywords.
            if (_settings.TryGetValue(keyword, out string? value))
            {
                return value;
            }

            AssertIsKnownKeyword(keyword);
            return null;
        }
        set
        {
            ArgumentGuard.NotNull(keyword);

            string? stringValue = value?.ToString();

            if (stringValue == null)
            {
                _settings.Remove(keyword);
            }
            else
            {
                // Allow setting unknown keyword. We don't pretend to know all valid keywords.
                _settings[keyword] = stringValue;
            }
        }
    }

    private string ToConnectionString()
    {
        if (!_settings.TryGetValue(KnownKeywords.Host, out string? server))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(server);

        if (_settings.TryGetValue(KnownKeywords.Port, out string? port))
        {
            builder.Append(':');
            builder.Append(port);
        }

        foreach ((string keyword, string value) in _settings.Where(pair => !KnownKeywords.Exists(pair.Key)))
        {
            builder.Append(',');
            builder.Append(keyword);
            builder.Append('=');
            builder.Append(value);
        }

        return builder.ToString();
    }

    private void FromConnectionString(string? connectionString)
    {
        _settings.Clear();

        if (string.IsNullOrEmpty(connectionString))
        {
            return;
        }
        foreach (string option in connectionString.Split(',').Where(element => !string.IsNullOrWhiteSpace(element)))
        {
            int equalsIndex = option.IndexOf('=');

            if (equalsIndex != -1)
            {
                string name = option[..equalsIndex].Trim();
                string value = option[(equalsIndex + 1)..].Trim();
                _settings[name] = value;
            }
            else if (option.Contains(','))
            {
                // Redis allows multiple servers in the connection string, but we haven't found any service bindings that actually use that.
                throw new NotImplementedException("Support for multiple servers is not implemented. Please open a GitHub issue if you need this.");
            }
            else
            {
                string[] hostWithPort = option.Split(':', 2);
                _settings[KnownKeywords.Host] = hostWithPort[0];

                if (hostWithPort.Length > 1)
                {
                    _settings[KnownKeywords.Port] = hostWithPort[1];
                }
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
        public const string Host = "host";
        public const string Port = "port";

        private static readonly ISet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Host,
            Port
        };

        public static bool Exists(string keyword)
        {
            return All.Contains(keyword);
        }
    }
}
