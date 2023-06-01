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
using Steeltoe.Connectors.MySql.DynamicTypeAccess;

namespace Steeltoe.Connectors.MySql;

public static class MySqlConfigurationBuilderExtensions
{
    public static IConfigurationBuilder ConfigureMySql(this IConfigurationBuilder builder)
    {
        return ConfigureMySql(builder, MySqlPackageResolver.Default);
    }

    internal static IConfigurationBuilder ConfigureMySql(this IConfigurationBuilder builder, MySqlPackageResolver packageResolver)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(packageResolver);

        RegisterPostProcessors(builder, packageResolver);
        return builder;
    }

    private static void RegisterPostProcessors(IConfigurationBuilder builder, MySqlPackageResolver packageResolver)
    {
        builder.AddCloudFoundryServiceBindings();
        CloudFoundryServiceBindingConfigurationSource cloudFoundrySource = builder.Sources.OfType<CloudFoundryServiceBindingConfigurationSource>().First();
        cloudFoundrySource.RegisterPostProcessor(new MySqlCloudFoundryPostProcessor());

        builder.AddKubernetesServiceBindings();
        KubernetesServiceBindingConfigurationSource kubernetesSource = builder.Sources.OfType<KubernetesServiceBindingConfigurationSource>().First();
        kubernetesSource.RegisterPostProcessor(new MySqlKubernetesPostProcessor());

        var connectionStringPostProcessor = new MySqlConnectionStringPostProcessor(packageResolver);
        var connectionStringSource = new ConnectionStringPostProcessorConfigurationSource();
        connectionStringSource.RegisterPostProcessor(connectionStringPostProcessor);
        builder.Add(connectionStringSource);
    }
}
