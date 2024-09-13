// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.CloudFoundry.ServiceBindings.PostProcessors;

internal sealed class SqlServerCloudFoundryPostProcessor : CloudFoundryPostProcessor
{
    internal const string BindingType = "sqlserver";

    public override void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string?> configurationData)
    {
        foreach (string key in FilterKeys(configurationData, BindingType, KeyFilterSources.Tag))
        {
            var mapper = ServiceBindingMapper.Create(configurationData, key, BindingType);

            // Mapping from CloudFoundry service binding credentials to driver-specific connection string parameters.
            // The available credentials are documented at:
            // - Azure Service Broker: https://docs.vmware.com/en/Tanzu-Cloud-Service-Broker-for-Azure/1.4/csb-azure/GUID-reference-azure-mssql.html#binding-credentials-4

            mapper.MapFromTo("credentials:hostname", "Data Source");
            mapper.MapFromAppendTo("credentials:port", "Data Source", ",");
            mapper.MapFromTo("credentials:name", "Initial Catalog");
            mapper.MapFromTo("credentials:username", "User ID");
            mapper.MapFromTo("credentials:password", "Password");
        }
    }
}
