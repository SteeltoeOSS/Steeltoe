// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

namespace Steeltoe.Connector.PostgreSql;

public class PostgresProviderConnectorOptions : AbstractServiceConnectorOptions
{
    private const string PostgresClientSectionPrefix = "postgres:client";
    public const string DefaultHost = "localhost";
    public const int DefaultPort = 5432;
    private readonly bool _cloudFoundryConfigFound;

    internal Dictionary<string, string> Options { get; set; } = new();

    public string ConnectionString { get; set; }

    public string Host { get; set; } = DefaultHost;

    public int Port { get; set; } = DefaultPort;

    public string Username { get; set; }

    public string Password { get; set; }

    public string Database { get; set; }

    public string SearchPath { get; set; }

    public string SslMode { get; set; }

    public string ClientCertificate { get; set; }

    public string ClientKey { get; set; }

    public string SslRootCertificate { get; set; }

    public int Timeout { get; set; } = 15;

    public int CommandTimeout { get; set; } = 30;

    public bool? TrustServerCertificate { get; set; } = null;

    public PostgresProviderConnectorOptions()
    {
    }

    public PostgresProviderConnectorOptions(IConfiguration configuration)
    {
        ArgumentGuard.NotNull(configuration);

        IConfigurationSection section = configuration.GetSection(PostgresClientSectionPrefix);
        section.Bind(this);

        _cloudFoundryConfigFound = configuration.HasCloudFoundryServiceConfigurations();
    }

    public override string ToString()
    {
        StringBuilder sb;

        if (!string.IsNullOrEmpty(ConnectionString) && !_cloudFoundryConfigFound)
        {
            sb = new StringBuilder(ConnectionString);
        }
        else
        {
            sb = new StringBuilder();
            AddKeyValue(sb, nameof(Host), Host);
            AddKeyValue(sb, nameof(Port), Port);
            AddKeyValue(sb, nameof(Username), Username);
            AddKeyValue(sb, nameof(Password), Password);
            AddKeyValue(sb, nameof(Database), Database);
        }

        AddKeyValue(sb, nameof(Timeout), Timeout);
        AddKeyValue(sb, "Command Timeout", CommandTimeout);
        AddKeyValue(sb, "Search Path", SearchPath);
        AddKeyValue(sb, "sslmode", SslMode);
        AddKeyValue(sb, "Trust Server Certificate", TrustServerCertificate);

        if (Options != null && Options.Any())
        {
            foreach (KeyValuePair<string, string> o in Options)
            {
                AddKeyValue(sb, o.Key, o.Value);
            }
        }

        return sb.ToString();
    }
}
