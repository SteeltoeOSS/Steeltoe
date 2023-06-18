// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

namespace Steeltoe.Connectors.MongoDb;

public class MongoDbConnectorOptions : AbstractServiceConnectorOptions
{
    private const string MongodbClientSectionPrefix = "mongodb:client";
    public const string DefaultServer = "localhost";
    public const int DefaultPort = 27017;
    private readonly bool _bindingsFound;

    internal string Uri { get; set; }

    public string ConnectionString { get; set; }

    public string Server { get; set; } = DefaultServer;

    public int Port { get; set; } = DefaultPort;

    public string Username { get; set; }

    public string Password { get; set; }

    public string Database { get; set; }

    public Dictionary<string, string> Options { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public MongoDbConnectorOptions()
    {
    }

    public MongoDbConnectorOptions(IConfiguration configuration)
        : base('&', '=')
    {
        ArgumentGuard.NotNull(configuration);

        IConfigurationSection section = configuration.GetSection(MongodbClientSectionPrefix);
        section.Bind(this);

        _bindingsFound = configuration.HasCloudFoundryServiceConfigurations() || configuration.HasKubernetesServiceBindings();
    }

    public override string ToString()
    {
        if (!string.IsNullOrEmpty(ConnectionString) && !_bindingsFound)
        {
            // Connection string was provided and VCAP_SERVICES wasn't found, just use the connectionstring
            return ConnectionString;
        }

        if (Uri != null)
        {
            // VCAP_SERVICES provided a URI, the MongoDB driver can just use that
            return Uri;
        }

        // build a MongoDB connection string
        var sb = new StringBuilder();

        sb.Append("mongodb://");
        AddColonDelimitedPair(sb, Username, Password, '@');
        AddColonDelimitedPair(sb, Server, Port.ToString(CultureInfo.InvariantCulture));

        if (!string.IsNullOrEmpty(Database))
        {
            sb.Append('/');
            sb.Append(Database);
        }

        if (Options != null && Options.Any())
        {
            sb.Append('?');

            foreach (KeyValuePair<string, string> o in Options)
            {
                AddKeyValue(sb, o.Key, o.Value);
            }
        }

        return sb.ToString().TrimEnd('&');
    }
}
