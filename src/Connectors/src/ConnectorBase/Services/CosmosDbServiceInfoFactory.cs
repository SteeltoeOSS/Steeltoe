// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Extensions.Configuration;
using System.Collections.Generic;

namespace Steeltoe.Connector.Services
{
    public class CosmosDbServiceInfoFactory : ServiceInfoFactory
    {
        public CosmosDbServiceInfoFactory()
            : base(new Tags(new string[] { "azure-cosmosdb", "cosmosdb" }), "cosmosdb") // this URI scheme isn't know to be in use
        {
        }

        public override IServiceInfo Create(Service binding)
        {
            return new CosmosDbServiceInfo(binding.Name)
            {
                Host = GetStringFromCredentials(binding.Credentials, new List<string> { "host_endpoint", "cosmosdb_host_endpoint", "uri" }),
                MasterKey = GetStringFromCredentials(binding.Credentials, new List<string> { "master_key", "cosmosdb_master_key" }),
                ReadOnlyKey = GetStringFromCredentials(binding.Credentials, new List<string> { "readonly_master_key", "readonly_key", "cosmosdb_readonly_master_key" }),
                DatabaseId = GetStringFromCredentials(binding.Credentials, new List<string> { "database_id", "cosmosdb_database_id" }),
                DatabaseLink = GetStringFromCredentials(binding.Credentials, new List<string> { "database_link", "cosmosdb_database_link" })
            };
        }

        public override bool Accepts(Service binding)
        {
            return (TagsMatch(binding) || LabelStartsWithTag(binding))
                && IsNotMongoDb(binding);
        }

        private bool IsNotMongoDb(Service binding)
        {
            var connString = GetStringFromCredentials(binding.Credentials, "cosmosdb_connection_string");
            if (!string.IsNullOrEmpty(connString) && connString.StartsWith("mongodb"))
            {
                return false;
            }

            return true;
        }
    }
}
