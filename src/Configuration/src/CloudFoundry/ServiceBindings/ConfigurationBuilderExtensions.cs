// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Configuration.CloudFoundry.ServiceBindings.PostProcessors;

namespace Steeltoe.Configuration.CloudFoundry.ServiceBindings;

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
    /// The <see cref="IConfigurationBuilder" /> to add configuration to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IConfigurationBuilder AddCloudFoundryServiceBindings(this IConfigurationBuilder builder)
    {
        return builder.AddCloudFoundryServiceBindings(DefaultIgnoreKeyPredicate, null, CloudFoundryServiceBrokerTypes.All, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds CloudFoundry service bindings from the JSON provided by the specified reader.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add configuration to.
    /// </param>
    /// <param name="brokerTypes">
    /// The set of broker types to read service bindings for.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IConfigurationBuilder AddCloudFoundryServiceBindings(this IConfigurationBuilder builder, CloudFoundryServiceBrokerTypes brokerTypes)
    {
        return builder.AddCloudFoundryServiceBindings(DefaultIgnoreKeyPredicate, null, brokerTypes, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds CloudFoundry service bindings from the JSON provided by the specified reader.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add configuration to.
    /// </param>
    /// <param name="serviceBindingsReader">
    /// The source to read JSON service bindings from.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IConfigurationBuilder AddCloudFoundryServiceBindings(this IConfigurationBuilder builder, IServiceBindingsReader serviceBindingsReader)
    {
        return builder.AddCloudFoundryServiceBindings(DefaultIgnoreKeyPredicate, serviceBindingsReader, CloudFoundryServiceBrokerTypes.All,
            NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds CloudFoundry service bindings from the JSON provided by the specified reader.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add configuration to.
    /// </param>
    /// <param name="ignoreKeyPredicate">
    /// A predicate that is called before adding a key to the configuration. If it returns false, the key will be ignored.
    /// </param>
    /// <param name="serviceBindingsReader">
    /// The source to read JSON service bindings from.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IConfigurationBuilder AddCloudFoundryServiceBindings(this IConfigurationBuilder builder, Predicate<string> ignoreKeyPredicate,
        IServiceBindingsReader serviceBindingsReader, ILoggerFactory loggerFactory)
    {
        return AddCloudFoundryServiceBindings(builder, ignoreKeyPredicate, serviceBindingsReader, CloudFoundryServiceBrokerTypes.All, loggerFactory);
    }

    /// <summary>
    /// Adds CloudFoundry service bindings from the JSON provided by the specified reader.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add configuration to.
    /// </param>
    /// <param name="ignoreKeyPredicate">
    /// A predicate that is called before adding a key to the configuration. If it returns false, the key will be ignored.
    /// </param>
    /// <param name="serviceBindingsReader">
    /// The source to read JSON service bindings from.
    /// </param>
    /// <param name="brokerTypes">
    /// The set of broker types to read service bindings for.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IConfigurationBuilder AddCloudFoundryServiceBindings(this IConfigurationBuilder builder, Predicate<string> ignoreKeyPredicate,
        IServiceBindingsReader? serviceBindingsReader, CloudFoundryServiceBrokerTypes brokerTypes, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(ignoreKeyPredicate);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        CloudFoundryServiceBrokerTypes missingBrokerTypes = GetMissingBrokerTypes(builder, brokerTypes);

        if (missingBrokerTypes != CloudFoundryServiceBrokerTypes.None)
        {
            var source = new CloudFoundryServiceBindingConfigurationSource(serviceBindingsReader ?? DefaultReader, missingBrokerTypes)
            {
                IgnoreKeyPredicate = ignoreKeyPredicate
            };

            // All post-processors must be registered *before* the configuration source is added to the builder. When adding the source,
            // WebApplicationBuilder immediately builds the configuration provider and loads it, which executes the post-processors.
            // Therefore, adding post-processors afterward is a no-op.

            RegisterPostProcessors(source, missingBrokerTypes, loggerFactory);
            builder.Add(source);
        }

        return builder;
    }

    private static CloudFoundryServiceBrokerTypes GetMissingBrokerTypes(IConfigurationBuilder builder, CloudFoundryServiceBrokerTypes brokerTypesRequested)
    {
        CloudFoundryServiceBrokerTypes missingBrokerTypes = brokerTypesRequested;

        if (brokerTypesRequested != CloudFoundryServiceBrokerTypes.None)
        {
            foreach (CloudFoundryServiceBindingConfigurationSource existingSource in builder.EnumerateSources<CloudFoundryServiceBindingConfigurationSource>())
            {
                missingBrokerTypes &= ~existingSource.BrokerTypes;
            }
        }

        return missingBrokerTypes;
    }

    private static void RegisterPostProcessors(CloudFoundryServiceBindingConfigurationSource source, CloudFoundryServiceBrokerTypes brokerTypes,
        ILoggerFactory loggerFactory)
    {
        if (brokerTypes.HasFlag(CloudFoundryServiceBrokerTypes.Eureka))
        {
            ILogger<EurekaCloudFoundryPostProcessor> eurekaLogger = loggerFactory.CreateLogger<EurekaCloudFoundryPostProcessor>();
            source.RegisterPostProcessor(new EurekaCloudFoundryPostProcessor(eurekaLogger));
        }

        if (brokerTypes.HasFlag(CloudFoundryServiceBrokerTypes.Identity))
        {
            ILogger<IdentityCloudFoundryPostProcessor> identityLogger = loggerFactory.CreateLogger<IdentityCloudFoundryPostProcessor>();
            source.RegisterPostProcessor(new IdentityCloudFoundryPostProcessor(identityLogger));
        }

        if (brokerTypes.HasFlag(CloudFoundryServiceBrokerTypes.MongoDb))
        {
            source.RegisterPostProcessor(new MongoDbCloudFoundryPostProcessor());
        }

        if (brokerTypes.HasFlag(CloudFoundryServiceBrokerTypes.MySql))
        {
            source.RegisterPostProcessor(new MySqlCloudFoundryPostProcessor());
        }

        if (brokerTypes.HasFlag(CloudFoundryServiceBrokerTypes.PostgreSql))
        {
            source.RegisterPostProcessor(new PostgreSqlCloudFoundryPostProcessor());
        }

        if (brokerTypes.HasFlag(CloudFoundryServiceBrokerTypes.RabbitMQ))
        {
            source.RegisterPostProcessor(new RabbitMQCloudFoundryPostProcessor());
        }

        if (brokerTypes.HasFlag(CloudFoundryServiceBrokerTypes.Redis))
        {
            source.RegisterPostProcessor(new RedisCloudFoundryPostProcessor());
        }

        if (brokerTypes.HasFlag(CloudFoundryServiceBrokerTypes.SqlServer))
        {
            source.RegisterPostProcessor(new SqlServerCloudFoundryPostProcessor());
        }
    }
}
