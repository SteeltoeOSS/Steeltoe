// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Web;
using Microsoft.AspNetCore.Http;
using Steeltoe.Common;

namespace Steeltoe.Connectors.MongoDb;

/// <summary>
/// Provides a connection string builder for MongoDB URLs. The format is defined at: https://www.mongodb.com/docs/manual/reference/connection-string/.
/// </summary>
internal sealed class MongoDbConnectionStringBuilder : IConnectionStringBuilder
{
    private readonly Dictionary<string, string> _settings = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    [AllowNull]
    public string ConnectionString
    {
        get => ToConnectionString();
        set => FromConnectionString(value, false);
    }

    /// <inheritdoc />
    public object? this[string keyword]
    {
        get
        {
            ArgumentGuard.NotNull(keyword);

            if (string.Equals(keyword, KnownKeywords.Url, StringComparison.OrdinalIgnoreCase))
            {
                return ConnectionString;
            }

            // Allow getting unknown keyword, if it was set earlier. We don't pretend to know all valid query string parameters.
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

            if (string.Equals(keyword, KnownKeywords.Url, StringComparison.OrdinalIgnoreCase))
            {
                FromConnectionString(value?.ToString(), true);
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
        if (!_settings.TryGetValue(KnownKeywords.Server, out string? server))
        {
            return string.Empty;
        }

        var builder = new UriBuilder
        {
            Scheme = "mongodb",
            Host = server
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

        if (_settings.TryGetValue(KnownKeywords.AuthenticationDatabase, out string? authenticationDatabase))
        {
            builder.Path = Uri.EscapeDataString(authenticationDatabase);
        }

        var queryString = default(QueryString);

        foreach ((string keyword, string value) in _settings.Where(pair => !KnownKeywords.Exists(pair.Key)))
        {
            queryString = queryString.Add(keyword, value);
        }

        builder.Query = queryString.Value;

        return builder.Uri.AbsoluteUri;
    }

    private void FromConnectionString(string? connectionString, bool preserveUnknownSettings)
    {
        if (preserveUnknownSettings)
        {
            foreach (string keywordToRemove in _settings.Keys.Where(KnownKeywords.Exists).ToArray())
            {
                _settings.Remove(keywordToRemove);
            }
        }
        else
        {
            _settings.Clear();
        }

        if (!string.IsNullOrEmpty(connectionString))
        {
            if (connectionString.Contains(','))
            {
                // MongoDB allows multiple servers in the connection string, but we haven't found any service bindings that actually use that.
                throw new NotImplementedException("Support for multiple servers is not implemented. Please open a GitHub issue if you need this.");
            }

            // MongoDB allows semicolon as separator for query string parameters, to provide backwards compatibility.
            connectionString = connectionString.Replace(';', '&');

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

            _settings[KnownKeywords.Server] = uri.Host;

            if (uri.Port != -1)
            {
                _settings[KnownKeywords.Port] = uri.Port.ToString(CultureInfo.InvariantCulture);
            }

            if (uri.AbsolutePath.StartsWith('/') && uri.AbsolutePath.Length > 1)
            {
                _settings[KnownKeywords.AuthenticationDatabase] = Uri.UnescapeDataString(uri.AbsolutePath[1..]);
            }

            NameValueCollection queryCollection = HttpUtility.ParseQueryString(uri.Query);

            foreach (string? key in queryCollection.AllKeys)
            {
                if (key != null)
                {
                    string? value = queryCollection.Get(key);

                    if (value != null)
                    {
                        _settings[key] = value;
                    }
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
        public const string Url = "url";
        public const string Username = "username";
        public const string Password = "password";
        public const string Server = "server";
        public const string Port = "port";
        public const string AuthenticationDatabase = "authenticationDatabase";

        private static readonly ISet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Url,
            Username,
            Password,
            Server,
            Port,
            AuthenticationDatabase
        };

        public static bool Exists(string keyword)
        {
            return All.Contains(keyword);
        }
    }
}
