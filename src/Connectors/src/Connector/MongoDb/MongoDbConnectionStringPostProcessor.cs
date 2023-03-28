// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Steeltoe.Connector.MongoDb;

internal sealed class MongoDbConnectionStringPostProcessor : ConnectionStringPostProcessor
{
    protected override string BindingType => "mongodb";

    protected override IConnectionStringBuilder CreateConnectionStringBuilder()
    {
        return new MongoDbConnectionStringBuilder();
    }

    private sealed class MongoDbConnectionStringBuilder : IConnectionStringBuilder
    {
        private static readonly HashSet<string> NonQueryStringKeywords = new(new[]
        {
            KnownKeywords.Url,
            KnownKeywords.Username,
            KnownKeywords.Password,
            KnownKeywords.Server,
            KnownKeywords.Port,
            KnownKeywords.AuthenticationDatabase
        }, StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, string> _settings = new(StringComparer.OrdinalIgnoreCase);
        private string _explicitUrl;

        [AllowNull]
        public string ConnectionString
        {
            get => _explicitUrl ?? BuildConnectionString();
            set => _explicitUrl = value;
        }

        [AllowNull]
        public object this[string keyword]
        {
            get
            {
                if (keyword == KnownKeywords.Url)
                {
                    return ConnectionString;
                }

                if (_settings.TryGetValue(keyword, out string value))
                {
                    return value;
                }

                throw new ArgumentException($"Keyword not supported: '{keyword}'.");
            }
            set
            {
                if (keyword == KnownKeywords.Url)
                {
                    ConnectionString = value?.ToString();
                }
                else
                {
                    // Discard connection string from appsettings.json
                    _explicitUrl = null;

                    string stringValue = value?.ToString();

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

        private string BuildConnectionString()
        {
            var builder = new StringBuilder("mongodb://");

            if (_settings.TryGetValue(KnownKeywords.Username, out string username))
            {
                builder.Append(Uri.EscapeDataString(username));

                if (_settings.TryGetValue(KnownKeywords.Password, out string password))
                {
                    builder.Append(':');
                    builder.Append(Uri.EscapeDataString(password));
                }

                builder.Append('@');
            }

            if (_settings.TryGetValue(KnownKeywords.Server, out string server))
            {
                builder.Append(server);

                if (_settings.TryGetValue(KnownKeywords.Port, out string port))
                {
                    builder.Append(':');
                    builder.Append(port);
                }
            }

            if (_settings.TryGetValue(KnownKeywords.AuthenticationDatabase, out string authenticationDatabase))
            {
                builder.Append('/');
                builder.Append(authenticationDatabase);
            }

            var queryString = default(QueryString);

            foreach ((string keyword, string value) in _settings)
            {
                if (!NonQueryStringKeywords.Contains(keyword))
                {
                    queryString = queryString.Add(keyword, value);
                }
            }

            builder.Append(queryString);

            return builder.ToString();
        }

        private static class KnownKeywords
        {
            public const string Url = "url";
            public const string Username = "username";
            public const string Password = "password";
            public const string Server = "server";
            public const string Port = "port";
            public const string AuthenticationDatabase = "authenticationDatabase";
        }
    }
}
