// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding;

/// <summary>
/// Extension methods for registering Kubernetes <see cref="ServiceBindingConfigurationProvider" /> with <see cref="IConfigurationBuilder" />.
/// </summary>
public static class ConfigurationBuilderExtensions
{
    private const bool DefaultOptional = true;
    private const bool DefaultReloadOnChange = false;
    private static readonly Predicate<string> DefaultIgnoreKeyPredicate = _ => false;

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
        return builder.AddKubernetesServiceBindings(DefaultOptional, DefaultReloadOnChange, DefaultIgnoreKeyPredicate);
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
    /// <returns>
    /// The <see cref="IConfigurationBuilder" />.
    /// </returns>
    public static IConfigurationBuilder AddKubernetesServiceBindings(this IConfigurationBuilder builder, bool optional)
    {
        return builder.AddKubernetesServiceBindings(optional, DefaultReloadOnChange, DefaultIgnoreKeyPredicate);
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
    /// <returns>
    /// The <see cref="IConfigurationBuilder" />.
    /// </returns>
    public static IConfigurationBuilder AddKubernetesServiceBindings(this IConfigurationBuilder builder, bool optional, bool reloadOnChange)
    {
        return builder.AddKubernetesServiceBindings(optional, reloadOnChange, DefaultIgnoreKeyPredicate);
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
    /// <returns>
    /// The <see cref="IConfigurationBuilder" />.
    /// </returns>
    public static IConfigurationBuilder AddKubernetesServiceBindings(this IConfigurationBuilder builder, bool optional, bool reloadOnChange,
        Predicate<string> ignoreKeyPredicate)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(ignoreKeyPredicate);

        var source = new ServiceBindingConfigurationSource
        {
            Optional = optional,
            ReloadOnChange = reloadOnChange,
            IgnoreKeyPredicate = ignoreKeyPredicate
        };

        return RegisterPostProcessors(builder, source);
    }

    private static IConfigurationBuilder RegisterPostProcessors(IConfigurationBuilder builder, ServiceBindingConfigurationSource source)
    {
        source.RegisterPostProcessor(new MySqlPostProcessor());
        source.RegisterPostProcessor(new PostgreSqlPostProcessor());
        source.RegisterPostProcessor(new RabbitMQPostProcessor());
        source.RegisterPostProcessor(new RedisPostProcessor());

        builder.Add(source);
        return builder;
    }
}
