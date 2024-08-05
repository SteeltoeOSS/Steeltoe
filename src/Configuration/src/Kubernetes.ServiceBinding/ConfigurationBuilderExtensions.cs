// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Configuration.Kubernetes.ServiceBinding.PostProcessors;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding;

/// <summary>
/// Extension methods for registering Kubernetes <see cref="KubernetesServiceBindingConfigurationProvider" /> with <see cref="IConfigurationBuilder" />.
/// </summary>
public static class ConfigurationBuilderExtensions
{
    private const bool DefaultOptional = true;
    private const bool DefaultReloadOnChange = false;
    private static readonly Predicate<string> DefaultIgnoreKeyPredicate = _ => false;
    private static readonly IServiceBindingsReader DefaultReader = new EnvironmentServiceBindingsReader();

    /// <summary>
    /// Adds configuration using files from the directory path specified by the environment variable "SERVICE_BINDING_ROOT". File name and directory paths
    /// are used as the key, and the file contents are used as the values.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add to.
    /// </param>
    /// <returns>
    /// The <see cref="IConfigurationBuilder" />.
    /// </returns>
    public static IConfigurationBuilder AddKubernetesServiceBindings(this IConfigurationBuilder builder)
    {
        return builder.AddKubernetesServiceBindings(DefaultOptional, DefaultReloadOnChange, DefaultIgnoreKeyPredicate, DefaultReader,
            NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds configuration using files from the directory path specified by the environment variable "SERVICE_BINDING_ROOT". File name and directory paths
    /// are used as the key, and the file contents are used as the values.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add to.
    /// </param>
    /// <param name="serviceBindingsReader">
    /// The source to read Kubernetes secret files on disk from.
    /// </param>
    /// <returns>
    /// The <see cref="IConfigurationBuilder" />.
    /// </returns>
    public static IConfigurationBuilder AddKubernetesServiceBindings(this IConfigurationBuilder builder, IServiceBindingsReader serviceBindingsReader)
    {
        return builder.AddKubernetesServiceBindings(DefaultOptional, DefaultReloadOnChange, DefaultIgnoreKeyPredicate, serviceBindingsReader,
            NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds configuration using files from the directory path specified by the environment variable "SERVICE_BINDING_ROOT". File name and directory paths
    /// are used as the key, and the file contents are used as the values.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add to.
    /// </param>
    /// <param name="optional">
    /// Whether the directory path is optional.
    /// </param>
    /// <param name="reloadOnChange">
    /// Whether the configuration should be reloaded if the files are changed, added or removed.
    /// </param>
    /// <param name="ignoreKeyPredicate">
    /// A predicate which is called before adding a key to the configuration. If it returns false, the key will be ignored.
    /// </param>
    /// <param name="serviceBindingsReader">
    /// The source to read Kubernetes secret files on disk from.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    /// <returns>
    /// The <see cref="IConfigurationBuilder" />.
    /// </returns>
    public static IConfigurationBuilder AddKubernetesServiceBindings(this IConfigurationBuilder builder, bool optional, bool reloadOnChange,
        Predicate<string> ignoreKeyPredicate, IServiceBindingsReader serviceBindingsReader, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(ignoreKeyPredicate);
        ArgumentNullException.ThrowIfNull(serviceBindingsReader);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        if (!builder.Sources.OfType<KubernetesServiceBindingConfigurationSource>().Any())
        {
            var source = new KubernetesServiceBindingConfigurationSource(serviceBindingsReader)
            {
                Optional = optional,
                ReloadOnChange = reloadOnChange,
                IgnoreKeyPredicate = ignoreKeyPredicate
            };

            // All post-processors must be registered *before* the configuration source is added to the builder. When adding the source,
            // WebApplicationBuilder immediately builds the configuration provider and loads it, which executes the post-processors.
            // Therefore, adding post-processors afterward is a no-op.

            RegisterPostProcessors(source);
            builder.Add(source);
        }

        return builder;
    }

    private static void RegisterPostProcessors(KubernetesServiceBindingConfigurationSource source)
    {
        source.RegisterPostProcessor(new ApplicationConfigurationServicePostProcessor());
        source.RegisterPostProcessor(new MongoDbKubernetesPostProcessor());
        source.RegisterPostProcessor(new MySqlKubernetesPostProcessor());
        source.RegisterPostProcessor(new PostgreSqlKubernetesPostProcessor());
        source.RegisterPostProcessor(new RabbitMQKubernetesPostProcessor());
        source.RegisterPostProcessor(new RedisKubernetesPostProcessor());
    }
}
