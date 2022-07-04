// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using System.Text;

namespace Steeltoe.Connector.CosmosDb;

public class CosmosDbConnectorOptions : AbstractServiceConnectorOptions
{
    private const string CosmosdbClientSectionPrefix = "cosmosdb:client";

    public CosmosDbConnectorOptions()
    {
    }

    public CosmosDbConnectorOptions(IConfiguration configuration)
        : base(configuration)
    {
        var section = configuration.GetSection(CosmosdbClientSectionPrefix);
        section.Bind(this);
    }

    public string ConnectionString { get; set; }

    public string Host { get; set; }

    public string MasterKey { get; set; }

    public string ReadOnlyKey { get; set; }

    public string DatabaseId { get; set; }

    public string DatabaseLink { get; set; }

    public bool UseReadOnlyCredentials { get; set; }

    public override string ToString()
    {
        if (!string.IsNullOrEmpty(ConnectionString) && !Platform.IsCloudFoundry)
        {
            // Connection string was provided and we don't appear to be running on a cloud platform
            return ConnectionString;
        }
        else
        {
            // build a CosmosDB connection string
            var sb = new StringBuilder();

            AddKeyValue(sb, "AccountEndpoint", Host);
            AddKeyValue(sb, "AccountKey", UseReadOnlyCredentials ? ReadOnlyKey : MasterKey);

            return sb.ToString();
        }
    }
}
