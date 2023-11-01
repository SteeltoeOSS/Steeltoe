// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.CloudFoundry.ServiceBinding.PostProcessors;

internal sealed class PostgreSqlCloudFoundryPostProcessor : CloudFoundryPostProcessor
{
    private const string HostsSeparator = ",";

    internal const string BindingType = "postgresql";
    internal const string AlternateInputBindingType = "postgres";

    public override void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string?> configurationData)
    {
        ICollection<string> keys = FilterKeys(configurationData, BindingType);
        ICollection<string> alternateKeys = FilterKeys(configurationData, AlternateInputBindingType);

        foreach (string key in keys.Concat(alternateKeys))
        {
            var mapper = ServiceBindingMapper.Create(configurationData, key, BindingType);

            // Mapping from CloudFoundry service binding credentials to driver-specific connection string parameters.
            // The available credentials are documented at:
            // - VMware Broker: https://docs.vmware.com/en/VMware-Postgres-for-VMware-Tanzu-Application-Service/1.0/postgres/app-dev-guide.html
            // - Azure Service Broker: https://docs.vmware.com/en/Tanzu-Cloud-Service-Broker-for-Azure/1.4/csb-azure/GUID-reference-azure-postgresql.html#binding-credentials-5
            // - GCP Service Broker: https://docs.vmware.com/en/Tanzu-Cloud-Service-Broker-for-GCP/1.2/csb-gcp/GUID-reference-gcp-postgresql.html#binding-credentials-6
            // - AWS Service Broker: https://docs.vmware.com/en/Tanzu-Cloud-Service-Broker-for-AWS/1.5/csb-aws/GUID-reference-aws-postgres.html#binding-credentials-3

            string? hosts = mapper.MapArrayFromTo("credentials:hosts", "host", HostsSeparator);

            if (hosts != null)
            {
                if (hosts.Contains(HostsSeparator, StringComparison.Ordinal))
                {
                    // https://www.npgsql.org/doc/failover-and-load-balancing.html
                    mapper.SetToValue("Target Session Attributes", "primary");
                }
            }
            else
            {
                mapper.MapFromTo("credentials:hostname", "host");
            }

            mapper.MapFromTo("credentials:port", "port");

            if (mapper.MapFromTo("credentials:db", "database") == null)
            {
                mapper.MapFromTo("credentials:name", "database");
            }

            if (mapper.MapFromTo("credentials:user", "username") == null)
            {
                mapper.MapFromTo("credentials:username", "username");
            }

            mapper.MapFromTo("credentials:password", "password");
            mapper.MapFromToFile("credentials:sslcert", "SSL Certificate");
            mapper.MapFromToFile("credentials:sslkey", "SSL Key");
            mapper.MapFromToFile("credentials:sslrootcert", "Root Certificate");
        }
    }
}
