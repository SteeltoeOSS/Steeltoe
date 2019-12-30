// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Connector.Services;
using System.Net;

namespace Steeltoe.Connector.PostgreSql
{
    public class PostgresProviderConfigurer
    {
        internal string Configure(PostgresServiceInfo si, PostgresProviderConnectorOptions configuration)
        {
            UpdateConfiguration(si, configuration);
            return configuration.ToString();
        }

        internal void UpdateConfiguration(PostgresServiceInfo si, PostgresProviderConnectorOptions configuration)
        {
            if (si == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(si.Uri))
            {
                if (si.Port > 0)
                {
                    configuration.Port = si.Port;
                }

                if (configuration.UrlEncodedCredentials)
                {
                    configuration.Username = WebUtility.UrlDecode(si.UserName);
                    configuration.Password = WebUtility.UrlDecode(si.Password);
                }
                else
                {
                    configuration.Username = si.UserName;
                    configuration.Password = si.Password;
                }

                configuration.Host = si.Host;
                configuration.Database = si.Path;
                if (si.Query != null)
                {
                    foreach (var kvp in UriExtensions.ParseQuerystring(si.Query))
                    {
                        if (kvp.Key.Equals("sslmode", StringComparison.InvariantCultureIgnoreCase))
                        {
                            // Npgsql parses SSL Mode into an enum, the first character must be capitalized
                            configuration.SslMode = FirstCharToUpper(kvp.Value);
                        }
                        else if (kvp.Key.Equals("sslcert"))
                        {
                            // TODO: Map this client cert into the npgsql client cert callback
                            configuration.ClientCertificate = kvp.Value;
                        }
                        else if (kvp.Key.Equals("sslkey"))
                        {
                            // TODO: Map this client cert into the npgsql client cert callback
                            configuration.ClientKey = kvp.Value;
                        }
                        else if (kvp.Key.Equals("sslrootcert"))
                        {
                            // TODO: Map this client cert into the npgsql remote cert validation callback
                            configuration.SslRootCertificate = kvp.Value;
                        }
                        else
                        {
                            configuration.Options.Add(kvp.Key, kvp.Value);
                        }
                    }
                }
            }
        }

        // from https://stackoverflow.com/a/4405876/761468
        private string FirstCharToUpper(string input) =>
            input switch
            {
                null => throw new ArgumentNullException(nameof(input)),
#pragma warning disable SA1122 // Use string.Empty for empty strings
                "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
#pragma warning restore SA1122 // Use string.Empty for empty strings
                _ => input.First().ToString().ToUpper() + input.Substring(1)
            };
    }
}
