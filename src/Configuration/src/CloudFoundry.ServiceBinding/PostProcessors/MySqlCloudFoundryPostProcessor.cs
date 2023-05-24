// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.CloudFoundry.ServiceBinding.PostProcessors;

internal sealed class MySqlCloudFoundryPostProcessor : CloudFoundryPostProcessor
{
    internal const string BindingType = "mysql";

    public override void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configurationData)
    {
        foreach (string key in FilterKeys(configurationData, BindingType))
        {
            var mapper = ServiceBindingMapper.Create(configurationData, key, BindingType);

            // See MySQL connection string parameters at: https://dev.mysql.com/doc/refman/8.0/en/connecting-using-uri-or-key-value-pairs.html#connection-parameters-base
            mapper.MapFromTo("credentials:hostname", "host");
            mapper.MapFromTo("credentials:port", "port");
            mapper.MapFromTo("credentials:name", "database");
            mapper.MapFromTo("credentials:username", "username");
            mapper.MapFromTo("credentials:password", "password");
        }
    }
}
