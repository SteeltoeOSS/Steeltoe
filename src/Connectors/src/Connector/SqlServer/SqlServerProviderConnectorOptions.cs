// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.Extensions.Configuration;

namespace Steeltoe.Connector.SqlServer;

public class SqlServerProviderConnectorOptions : AbstractServiceConnectorOptions
{
    private const string SqlClientSectionPrefix = "sqlserver:credentials";
    public const string DefaultServer = "localhost";
    public const int DefaultPort = 1433;
    private readonly bool _cloudFoundryConfigFound;

    internal Dictionary<string, string> Options { get; set; } = new();

    public string ConnectionString { get; set; }

    public string Server { get; set; } = DefaultServer;

    public string InstanceName { get; set; }

    /// <summary>
    /// Gets or sets the port SQL Server is listening on. To exclude from connection string, use a value less than 0.
    /// </summary>
    public int Port { get; set; } = DefaultPort;

    public string Username { get; set; }

    public string Password { get; set; }

    public string Database { get; set; }

    public string IntegratedSecurity { get; set; }

    public string Uid { get; set; }

    public string Uri { get; set; }

    public string Db { get; set; }

    public string Pw { get; set; }

    /// <summary>
    /// Gets or sets the length of time (in seconds) to wait for a connection to the server before terminating the attempt and generating an error.
    /// </summary>
    /// <remarks>
    /// Default value is 15.
    /// </remarks>
    public int? Timeout { get; set; }

    public SqlServerProviderConnectorOptions()
    {
    }

    public SqlServerProviderConnectorOptions(IConfiguration config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        IConfigurationSection section = config.GetSection(SqlClientSectionPrefix);

        section.Bind(this);

        if (Uri != null)
        {
            Server = Uri.Split(':')[2].Substring(2);
        }

        Username = Uid;
        Database = Db;
        Password = Pw;

        section.Bind(this);

        _cloudFoundryConfigFound = config.HasCloudFoundryServiceConfigurations();
    }

    public override string ToString()
    {
        if (!string.IsNullOrEmpty(ConnectionString) && !_cloudFoundryConfigFound)
        {
            return ConnectionString;
        }

        var sb = new StringBuilder();
        AddDataSource(sb);
        AddKeyValue(sb, "Initial Catalog", Database);
        AddKeyValue(sb, "User Id", Username);
        AddKeyValue(sb, "Password", Password);
        AddKeyValue(sb, "Integrated Security", IntegratedSecurity);
        AddKeyValue(sb, "Connection Timeout", Timeout);

        if (Options != null && Options.Any())
        {
            foreach (KeyValuePair<string, string> o in Options)
            {
                AddKeyValue(sb, o.Key, o.Value);
            }
        }

        return sb.ToString();
    }

    private void AddDataSource(StringBuilder sb)
    {
        sb.Append("Data Source");
        sb.Append(DefaultSeparator);
        sb.Append(Server);

        if (InstanceName != null)
        {
            sb.Append($"\\{InstanceName}");
        }

        if (Port > 0)
        {
            sb.Append($",{Port}");
        }

        sb.Append(DefaultTerminator);
    }
}
