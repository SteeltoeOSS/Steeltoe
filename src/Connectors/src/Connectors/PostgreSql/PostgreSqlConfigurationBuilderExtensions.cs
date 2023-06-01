// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding.PostProcessors;
using Steeltoe.Configuration.Kubernetes.ServiceBinding;
using Steeltoe.Configuration.Kubernetes.ServiceBinding.PostProcessors;
using Steeltoe.Connectors.PostgreSql.DynamicTypeAccess;

namespace Steeltoe.Connectors.PostgreSql;

public static class PostgreSqlConfigurationBuilderExtensions
{
    public static IConfigurationBuilder ConfigurePostgreSql(this IConfigurationBuilder builder)
    {
        return ConfigurePostgreSql(builder, PostgreSqlPackageResolver.Default);
    }

    private static IConfigurationBuilder ConfigurePostgreSql(this IConfigurationBuilder builder, PostgreSqlPackageResolver packageResolver)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(packageResolver);

        RegisterPostProcessors(builder, packageResolver);
        return builder;
    }

    private static void RegisterPostProcessors(IConfigurationBuilder builder, PostgreSqlPackageResolver packageResolver)
    {
        builder.AddCloudFoundryServiceBindings();
        CloudFoundryServiceBindingConfigurationSource cloudFoundrySource = builder.Sources.OfType<CloudFoundryServiceBindingConfigurationSource>().First();
        cloudFoundrySource.RegisterPostProcessor(new PostgreSqlCloudFoundryPostProcessor());

        builder.AddKubernetesServiceBindings();
        KubernetesServiceBindingConfigurationSource kubernetesSource = builder.Sources.OfType<KubernetesServiceBindingConfigurationSource>().First();
        kubernetesSource.RegisterPostProcessor(new PostgreSqlKubernetesPostProcessor());

        var connectionStringPostProcessor = new PostgreSqlConnectionStringPostProcessor(packageResolver);
        var connectionStringSource = new ConnectionStringPostProcessorConfigurationSource();
        connectionStringSource.RegisterPostProcessor(connectionStringPostProcessor);
        builder.Add(connectionStringSource);
    }
}
