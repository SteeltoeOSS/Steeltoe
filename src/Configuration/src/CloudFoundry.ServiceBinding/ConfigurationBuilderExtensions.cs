// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

namespace Steeltoe.Configuration.CloudFoundry.ServiceBinding;

/// <summary>
/// Extension methods for registering CloudFoundry <see cref="ServiceBindingConfigurationProvider" /> with <see cref="IConfigurationBuilder" />.
/// </summary>
public static class ConfigurationBuilderExtensions
{
    private static readonly Predicate<string> DefaultIgnoreKeyPredicate = _ => false;

    /// <summary>
    /// Adds CloudFoundry service bindings from the JSON in the "VCAP_SERVICES" environment variable.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add to.
    /// </param>
    /// <returns>
    /// The <see cref="IConfigurationBuilder" />.
    /// </returns>
    public static IConfigurationBuilder AddCloudFoundryServiceBindings(this IConfigurationBuilder builder)
    {
        var reader = new EnvironmentServiceBindingsReader();
        return builder.AddCloudFoundryServiceBindings(DefaultIgnoreKeyPredicate, reader);
    }

    /// <summary>
    /// Adds CloudFoundry service bindings from the JSON in the "VCAP_SERVICES" environment variable.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add to.
    /// </param>
    /// <param name="ignoreKeyPredicate">
    /// A predicate which is called before adding a key to the configuration. If it returns false, the key will be ignored.
    /// </param>
    /// <returns>
    /// The <see cref="IConfigurationBuilder" />.
    /// </returns>
    public static IConfigurationBuilder AddCloudFoundryServiceBindings(this IConfigurationBuilder builder, Predicate<string> ignoreKeyPredicate)
    {
        var reader = new EnvironmentServiceBindingsReader();
        return builder.AddCloudFoundryServiceBindings(ignoreKeyPredicate, reader);
    }

    /// <summary>
    /// Adds CloudFoundry service bindings from the JSON provided by the specified reader.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add to.
    /// </param>
    /// <param name="serviceBindingsReader">
    /// The source to read JSON service bindings from.
    /// </param>
    /// <returns>
    /// The <see cref="IConfigurationBuilder" />.
    /// </returns>
    public static IConfigurationBuilder AddCloudFoundryServiceBindings(this IConfigurationBuilder builder, IServiceBindingsReader serviceBindingsReader)
    {
        return builder.AddCloudFoundryServiceBindings(DefaultIgnoreKeyPredicate, serviceBindingsReader);
    }

    /// <summary>
    /// Adds CloudFoundry service bindings from the JSON provided by the specified reader.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add to.
    /// </param>
    /// <param name="ignoreKeyPredicate">
    /// A predicate which is called before adding a key to the configuration. If it returns false, the key will be ignored.
    /// </param>
    /// <param name="serviceBindingsReader">
    /// The source to read JSON service bindings from.
    /// </param>
    /// <returns>
    /// The <see cref="IConfigurationBuilder" />.
    /// </returns>
    public static IConfigurationBuilder AddCloudFoundryServiceBindings(this IConfigurationBuilder builder, Predicate<string> ignoreKeyPredicate,
        IServiceBindingsReader serviceBindingsReader)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(ignoreKeyPredicate);
        ArgumentGuard.NotNull(serviceBindingsReader);

        var source = new ServiceBindingConfigurationSource(serviceBindingsReader)
        {
            IgnoreKeyPredicate = ignoreKeyPredicate
        };

        return RegisterPostProcessors(builder, source);
    }

    private static IConfigurationBuilder RegisterPostProcessors(IConfigurationBuilder builder, ServiceBindingConfigurationSource source)
    {
        source.RegisterPostProcessor(new PostgreSqlPostProcessor());
        source.RegisterPostProcessor(new MySqlPostProcessor());
        source.RegisterPostProcessor(new SqlServerPostProcessor());
        source.RegisterPostProcessor(new MongoDbPostProcessor());

        builder.Add(source);
        return builder;
    }
}
