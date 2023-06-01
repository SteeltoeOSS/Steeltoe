// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding.PostProcessors;
using Steeltoe.Connectors.SqlServer.RuntimeTypeAccess;

namespace Steeltoe.Connectors.SqlServer;

public static class SqlServerConfigurationBuilderExtensions
{
    public static IConfigurationBuilder ConfigureSqlServer(this IConfigurationBuilder builder)
    {
        return ConfigureSqlServer(builder, SqlServerPackageResolver.Default);
    }

    internal static IConfigurationBuilder ConfigureSqlServer(this IConfigurationBuilder builder, SqlServerPackageResolver packageResolver)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(packageResolver);

        RegisterPostProcessors(builder, packageResolver);
        return builder;
    }

    private static void RegisterPostProcessors(IConfigurationBuilder builder, SqlServerPackageResolver packageResolver)
    {
        builder.AddCloudFoundryServiceBindings();
        CloudFoundryServiceBindingConfigurationSource cloudFoundrySource = builder.Sources.OfType<CloudFoundryServiceBindingConfigurationSource>().First();
        cloudFoundrySource.RegisterPostProcessor(new SqlServerCloudFoundryPostProcessor());

        var connectionStringPostProcessor = new SqlServerConnectionStringPostProcessor(packageResolver);
        var connectionStringSource = new ConnectionStringPostProcessorConfigurationSource();
        connectionStringSource.RegisterPostProcessor(connectionStringPostProcessor);
        builder.Add(connectionStringSource);
    }
}
