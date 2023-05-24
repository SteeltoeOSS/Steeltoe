// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.CloudFoundry.ServiceBinding.PostProcessors;

internal sealed class SqlServerCloudFoundryPostProcessor : CloudFoundryPostProcessor
{
    internal const string BindingType = "sqlserver";

    public override void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configurationData)
    {
        foreach (string key in FilterKeys(configurationData, BindingType))
        {
            var mapper = ServiceBindingMapper.Create(configurationData, key, BindingType);

            // See SQL Server connection string parameters at: https://learn.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlconnection.connectionstring#remarks
            mapper.MapFromTo("credentials:hostname", "Data Source");
            mapper.MapFromAppendTo("credentials:port", "Data Source", ",");
            mapper.MapFromTo("credentials:name", "Initial Catalog");
            mapper.MapFromTo("credentials:username", "User ID");
            mapper.MapFromTo("credentials:password", "Password");
        }
    }
}
