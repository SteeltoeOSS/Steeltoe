// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding.PostProcessors;

namespace Steeltoe.Connectors.CosmosDb;

public static class CosmosDbConfigurationBuilderExtensions
{
    public static IConfigurationBuilder ConfigureCosmosDb(this IConfigurationBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        RegisterPostProcessors(builder);
        return builder;
    }

    private static void RegisterPostProcessors(IConfigurationBuilder builder)
    {
        builder.AddCloudFoundryServiceBindings();
        CloudFoundryServiceBindingConfigurationSource cloudFoundrySource = builder.Sources.OfType<CloudFoundryServiceBindingConfigurationSource>().First();
        cloudFoundrySource.RegisterPostProcessor(new CosmosDbCloudFoundryPostProcessor());

        var connectionStringPostProcessor = new CosmosDbConnectionStringPostProcessor();
        var connectionStringSource = new ConnectionStringPostProcessorConfigurationSource();
        connectionStringSource.RegisterPostProcessor(connectionStringPostProcessor);
        builder.Add(connectionStringSource);
    }
}
