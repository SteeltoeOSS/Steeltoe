// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding.PostProcessors;

namespace Steeltoe.Configuration.CloudFoundry.ServiceBinding;

/// <summary>
/// Extension methods for registering CloudFoundry <see cref="CloudFoundryServiceBindingConfigurationProvider" /> with
/// <see cref="IConfigurationBuilder" />.
/// </summary>
public static class ConfigurationBuilderExtensions
{
    private static readonly Predicate<string> DefaultIgnoreKeyPredicate = _ => false;
    private static readonly IServiceBindingsReader DefaultReader = new EnvironmentServiceBindingsReader();

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
        return builder.AddCloudFoundryServiceBindings(DefaultIgnoreKeyPredicate, DefaultReader, NullLoggerFactory.Instance);
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
        return builder.AddCloudFoundryServiceBindings(DefaultIgnoreKeyPredicate, serviceBindingsReader, NullLoggerFactory.Instance);
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
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    /// <returns>
    /// The <see cref="IConfigurationBuilder" />.
    /// </returns>
    public static IConfigurationBuilder AddCloudFoundryServiceBindings(this IConfigurationBuilder builder, Predicate<string> ignoreKeyPredicate,
        IServiceBindingsReader serviceBindingsReader, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(ignoreKeyPredicate);
        ArgumentGuard.NotNull(serviceBindingsReader);
        ArgumentGuard.NotNull(loggerFactory);

        if (!builder.Sources.OfType<CloudFoundryServiceBindingConfigurationSource>().Any())
        {
            var source = new CloudFoundryServiceBindingConfigurationSource(serviceBindingsReader)
            {
                IgnoreKeyPredicate = ignoreKeyPredicate
            };

            // All post-processors must be registered *before* the configuration source is added to the builder. When adding the source,
            // WebApplicationBuilder immediately builds the configuration provider and loads it, which executes the post-processors.
            // Therefore adding post-processors afterwards is a no-op.

            RegisterPostProcessors(source, loggerFactory);
            builder.Add(source);
        }

        return builder;
    }

    private static void RegisterPostProcessors(CloudFoundryServiceBindingConfigurationSource source, ILoggerFactory loggerFactory)
    {
        ILogger<EurekaCloudFoundryPostProcessor> eurekaLogger = loggerFactory.CreateLogger<EurekaCloudFoundryPostProcessor>();

        source.RegisterPostProcessor(new CosmosDbCloudFoundryPostProcessor());
        source.RegisterPostProcessor(new EurekaCloudFoundryPostProcessor(eurekaLogger));
        source.RegisterPostProcessor(new MongoDbCloudFoundryPostProcessor());
        source.RegisterPostProcessor(new MySqlCloudFoundryPostProcessor());
        source.RegisterPostProcessor(new PostgreSqlCloudFoundryPostProcessor());
        source.RegisterPostProcessor(new RabbitMQCloudFoundryPostProcessor());
        source.RegisterPostProcessor(new RedisCloudFoundryPostProcessor());
        source.RegisterPostProcessor(new SqlServerCloudFoundryPostProcessor());
    }
}
