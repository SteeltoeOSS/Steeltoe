// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.CloudFoundry.ServiceBindings.PostProcessors;

internal sealed class PostgreSqlCloudFoundryPostProcessor : CloudFoundryPostProcessor
{
    private const string HostsSeparator = ",";

    internal const string BindingType = "postgresql";
    internal const string TanzuBindingType = "postgres";

    public override void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string?> configurationData)
    {
        ICollection<string> keys = FilterKeys(configurationData, BindingType, KeyFilterSources.Tag);
        ICollection<string> tanzuKeys = FilterKeys(configurationData, TanzuBindingType, KeyFilterSources.Tag);

        foreach (string key in keys.Concat(tanzuKeys))
        {
            var mapper = ServiceBindingMapper.Create(configurationData, key, BindingType);

            // Mapping from CloudFoundry service binding credentials to driver-specific connection string parameters.
            // The available credentials are documented at:
            // - Tanzu Broker: https://techdocs.broadcom.com/us/en/vmware-tanzu/data-solutions/tanzu-for-postgres-on-cloud-foundry/10-1/postgres/app-setup-single-instance-service-guide.html
            // - GCP Service Broker: https://techdocs.broadcom.com/us/en/vmware-tanzu/platform-services/tanzu-cloud-service-broker-for-gcp/1-9/csb-gcp/reference-gcp-postgresql.html#binding-creds
            // - AWS Service Broker: https://techdocs.broadcom.com/us/en/vmware-tanzu/platform-services/tanzu-cloud-service-broker-for-aws/1-14/csb-aws/reference-aws-postgres.html#binding-creds

            string? hosts = mapper.MapArrayFromTo("credentials:hosts", "host", HostsSeparator);

            if (hosts != null)
            {
                // Tanzu Broker.
                if (hosts.Contains(HostsSeparator, StringComparison.Ordinal))
                {
                    // We can't know whether the connection is used for reading or writing, so always pick the primary node.
                    // https://www.npgsql.org/doc/failover-and-load-balancing.html
                    // See also: https://github.gwd.broadcom.net/TNZ/postgres-tile/issues/235.
                    mapper.SetToValue("Target Session Attributes", "primary");
                }
            }
            else
            {
                // Cloud Service Broker.
                mapper.MapFromTo("credentials:hostname", "host");
            }

            mapper.MapFromTo("credentials:port", "port");

            // Tanzu Broker.
            if (mapper.MapFromTo("credentials:db", "database") == null)
            {
                // Cloud Service Broker.
                mapper.MapFromTo("credentials:name", "database");
            }

            // Tanzu Broker.
            if (mapper.MapFromTo("credentials:user", "username") == null)
            {
                // Cloud Service Broker.
                mapper.MapFromTo("credentials:username", "username");
            }

            mapper.MapFromTo("credentials:password", "password");
            mapper.MapFromToFile("credentials:sslcert", "SSL Certificate");
            mapper.MapFromToFile("credentials:sslkey", "SSL Key");
            mapper.MapFromToFile("credentials:sslrootcert", "Root Certificate");
        }
    }
}
