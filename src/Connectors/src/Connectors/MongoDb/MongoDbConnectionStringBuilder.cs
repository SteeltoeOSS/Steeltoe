// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Web;
using Microsoft.AspNetCore.Http;

namespace Steeltoe.Connectors.MongoDb;

/// <summary>
/// Provides a connection string builder for MongoDB URLs. The format is defined at: https://www.mongodb.com/docs/manual/reference/connection-string/.
/// </summary>
internal sealed class MongoDbConnectionStringBuilder : IConnectionStringBuilder
{
    private const string DefaultScheme = "mongodb";

    private readonly Dictionary<string, string> _settings = new(StringComparer.OrdinalIgnoreCase);
    private string _scheme = DefaultScheme;

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
        if (!_settings.TryGetValue(KnownKeywords.Server, out string? server))
        {
            return string.Empty;
        }

        var builder = new UriBuilder
        {
            Scheme = _scheme,
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

        if (_settings.TryGetValue(KnownKeywords.Port, out string? portText) && int.TryParse(portText, CultureInfo.InvariantCulture, out int port))
        {
            builder.Port = port;
        }

        if (_settings.TryGetValue(KnownKeywords.AuthenticationDatabase, out string? authenticationDatabase))
        {
            builder.Path = Uri.EscapeDataString(authenticationDatabase);
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
        _scheme = DefaultScheme;

        if (!string.IsNullOrEmpty(connectionString))
        {
            if (HasMultipleServers(connectionString))
            {
                // MongoDB allows multiple servers in the connection string, but we haven't found any service bindings that actually use that.
#pragma warning disable S3717 // Track use of "NotImplementedException"
                throw new NotImplementedException("Support for multiple servers is not implemented. Please open a GitHub issue if you need this.");
#pragma warning restore S3717 // Track use of "NotImplementedException"
            }

            var uri = new Uri(NormalizeQueryStringSeparators(connectionString));

            _scheme = uri.Scheme;

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

            ParseQueryString(uri);
        }
    }

    private static bool HasMultipleServers(string connectionString)
    {
        // Commas can also occur in the password and in query string parameters, so only look at the server list. The format is:
        // scheme://[username:password@]host1[:port1][,...hostN[:portN]][/[database][?options]]
        int schemeEndIndex = connectionString.IndexOf(Uri.SchemeDelimiter, StringComparison.Ordinal);
        ReadOnlySpan<char> authority = schemeEndIndex == -1 ? connectionString : connectionString.AsSpan(schemeEndIndex + Uri.SchemeDelimiter.Length);

        int authorityEndIndex = authority.IndexOfAny('/', '?');

        if (authorityEndIndex != -1)
        {
            authority = authority[..authorityEndIndex];
        }

        ReadOnlySpan<char> servers = authority[(authority.LastIndexOf('@') + 1)..];
        return servers.Contains(',');
    }

    private static string NormalizeQueryStringSeparators(string connectionString)
    {
        int queryStartIndex = connectionString.IndexOf('?');

        if (queryStartIndex == -1)
        {
            return connectionString;
        }

        // MongoDB allows semicolon as separator between query string parameters for backward compatibility.
        return connectionString[..queryStartIndex] + connectionString[queryStartIndex..].Replace(';', '&');
    }

    private void ParseQueryString(Uri uri)
    {
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

    private static class KnownKeywords
    {
        public const string Url = "url";
        public const string Username = "username";
        public const string Password = "password";
        public const string Server = "server";
        public const string Port = "port";
        public const string AuthenticationDatabase = "authenticationDatabase";

        private static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
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
