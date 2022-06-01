// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Steeltoe.Connector.MongoDb;

public class MongoDbConnectorOptions : AbstractServiceConnectorOptions
{
    public const string Default_Server = "localhost";
    public const int Default_Port = 27017;
    private const string MONGODB_CLIENT_SECTION_PREFIX = "mongodb:client";
    private readonly bool _cloudFoundryConfigFound;

    public MongoDbConnectorOptions()
    {
    }

    public MongoDbConnectorOptions(IConfiguration config)
        : base('&', '=')
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        var section = config.GetSection(MONGODB_CLIENT_SECTION_PREFIX);
        section.Bind(this);

        _cloudFoundryConfigFound = config.HasCloudFoundryServiceConfigurations();
    }

    public string ConnectionString { get; set; }

    public string Server { get; set; } = Default_Server;

    public int Port { get; set; } = Default_Port;

    public string Username { get; set; }

    public string Password { get; set; }

    public string Database { get; set; }

    public Dictionary<string, string> Options { get; set; } = new (StringComparer.InvariantCultureIgnoreCase);

    internal string Uri { get; set; }

    public override string ToString()
    {
        if (!string.IsNullOrEmpty(ConnectionString) && !_cloudFoundryConfigFound)
        {
            // Connection string was provided and VCAP_SERVICES wasn't found, just use the connectionstring
            return ConnectionString;
        }
        else if (Uri != null)
        {
            // VCAP_SERVICES provided a URI, the MongoDB driver can just use that
            return Uri;
        }
        else
        {
            // build a MongoDB connection string
            var sb = new StringBuilder();

            sb.Append("mongodb://");
            AddColonDelimitedPair(sb, Username, Password, '@');
            AddColonDelimitedPair(sb, Server, Port.ToString());

            if (!string.IsNullOrEmpty(Database))
            {
                sb.Append('/');
                sb.Append(Database);
            }

            if (Options != null && Options.Any())
            {
                sb.Append('?');
                foreach (var o in Options)
                {
                    AddKeyValue(sb, o.Key, o.Value);
                }
            }

            return sb.ToString().TrimEnd('&');
        }
    }
}
