// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common;
using Steeltoe.Common.Hosting;

namespace Steeltoe.Bootstrap.AutoConfiguration;

public static class HostBuilderExtensions
{
    private static readonly IReadOnlySet<string> EmptySet = ImmutableHashSet<string>.Empty;

    /// <summary>
    /// Automatically configures Steeltoe packages that have been added to your project as NuGet references.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IHostBuilder AddSteeltoe(this IHostBuilder builder)
    {
        return AddSteeltoe(builder, EmptySet, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Automatically configures Steeltoe packages that have been added to your project as NuGet references.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostBuilder" /> to configure.
    /// </param>
    /// <param name="assemblyNamesToExclude">
    /// The set of assembly names to exclude from auto-configuration. For ease of use, select from the constants in <see cref="SteeltoeAssemblyNames" />.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IHostBuilder AddSteeltoe(this IHostBuilder builder, IReadOnlySet<string> assemblyNamesToExclude)
    {
        return AddSteeltoe(builder, assemblyNamesToExclude, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Automatically configures Steeltoe packages that have been added to your project as NuGet references.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostBuilder" /> to configure.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IHostBuilder AddSteeltoe(this IHostBuilder builder, ILoggerFactory loggerFactory)
    {
        return AddSteeltoe(builder, EmptySet, loggerFactory);
    }

    /// <summary>
    /// Automatically configures Steeltoe packages that have been added to your project as NuGet references.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostBuilder" /> to configure.
    /// </param>
    /// <param name="assemblyNamesToExclude">
    /// The set of assembly names to exclude from auto-configuration. For ease of use, select from the constants in <see cref="SteeltoeAssemblyNames" />.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IHostBuilder AddSteeltoe(this IHostBuilder builder, IReadOnlySet<string> assemblyNamesToExclude, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(assemblyNamesToExclude);
        ArgumentGuard.NotNull(loggerFactory);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);

        var scanner = new BootstrapScanner(wrapper, assemblyNamesToExclude, loggerFactory);
        scanner.ConfigureSteeltoe();

        return builder;
    }
}
