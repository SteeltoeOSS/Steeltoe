// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.CloudFoundry.ServiceBinding;

internal sealed class PostgreSqlPostProcessor : CloudFoundryConfigurationPostProcessor
{
    internal const string BindingType = "postgresql";

    public override void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configurationData)
    {
        if (!provider.IsBindingTypeEnabled(BindingType))
        {
            return;
        }

        foreach (string key in FilterKeys(configurationData, BindingType))
        {
            var mapper = ServiceBindingMapper.Create(configurationData, key, BindingType);

            // See PostgreSQL connection string parameters at: https://www.npgsql.org/doc/connection-string-parameters.html
            mapper.MapFromTo("credentials:hostname", "host");
            mapper.MapFromTo("credentials:port", "port");
            mapper.MapFromTo("credentials:name", "database");
            mapper.MapFromTo("credentials:username", "username");
            mapper.MapFromTo("credentials:password", "password");
            mapper.MapFromToFile("credentials:sslcert", "SSL Certificate");
            mapper.MapFromToFile("credentials:sslkey", "SSL Key");
            mapper.MapFromToFile("credentials:sslrootcert", "Root Certificate");
        }
    }
}
