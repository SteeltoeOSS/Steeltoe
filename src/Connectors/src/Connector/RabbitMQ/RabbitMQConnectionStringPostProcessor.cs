// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Steeltoe.Connector.RabbitMQ;

internal sealed class RabbitMQConnectionStringPostProcessor : ConnectionStringPostProcessor
{
    protected override string BindingType => "rabbitmq";

    protected override IConnectionStringBuilder CreateConnectionStringBuilder()
    {
        return new RabbitMQConnectionStringBuilder();
    }

    private sealed class RabbitMQConnectionStringBuilder : IConnectionStringBuilder
    {
        private readonly Dictionary<string, string> _settings = new(StringComparer.OrdinalIgnoreCase);
        private string _explicitUrl;

        [MaybeNull]
        [AllowNull]
        public string ConnectionString
        {
            get
            {
                if (_explicitUrl != null)
                {
                    return _explicitUrl;
                }

                if (_settings.Count == 0)
                {
                    return null;
                }

                return BuildConnectionString();
            }
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
            var builder = new UriBuilder
            {
                Scheme = _settings.TryGetValue(KnownKeywords.UseTls, out string useTlsString) && bool.TryParse(useTlsString, out bool useTls) && useTls
                    ? "amqps://"
                    : "amqp://"
            };

            if (_settings.TryGetValue(KnownKeywords.Username, out string username))
            {
                builder.UserName = Uri.EscapeDataString(username);

                if (_settings.TryGetValue(KnownKeywords.Password, out string password))
                {
                    builder.Password = Uri.EscapeDataString(password);
                }
            }

            if (_settings.TryGetValue(KnownKeywords.Host, out string server))
            {
                builder.Host = server;

                if (_settings.TryGetValue(KnownKeywords.Port, out string portString) && int.TryParse(portString, out int port))
                {
                    builder.Port = port;
                }
            }

            if (_settings.TryGetValue(KnownKeywords.VirtualHost, out string virtualHost))
            {
                builder.Path = Uri.EscapeDataString(virtualHost);
            }

            return builder.Uri.AbsoluteUri;
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
        }
    }
}
