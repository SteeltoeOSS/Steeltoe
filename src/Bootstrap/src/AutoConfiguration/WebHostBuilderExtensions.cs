// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common;
using Steeltoe.Common.Hosting;
using Steeltoe.Common.Logging;

namespace Steeltoe.Bootstrap.AutoConfiguration;

public static class WebHostBuilderExtensions
{
    private static readonly IReadOnlySet<string> EmptySet = ImmutableHashSet<string>.Empty;
    private static readonly ILoggerFactory DefaultLoggerFactory = BootstrapLoggerFactory.CreateConsole();

    /// <summary>
    /// Automatically configures Steeltoe packages that have been added to your project as NuGet references.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddSteeltoe(this IWebHostBuilder builder)
    {
        return AddSteeltoe(builder, EmptySet, DefaultLoggerFactory);
    }

    /// <summary>
    /// Automatically configures Steeltoe packages that have been added to your project as NuGet references.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <param name="assemblyNamesToExclude">
    /// The set of assembly names to exclude from autoconfiguration. For ease of use, select from the constants in <see cref="SteeltoeAssemblyNames" />.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddSteeltoe(this IWebHostBuilder builder, IReadOnlySet<string> assemblyNamesToExclude)
    {
        return AddSteeltoe(builder, assemblyNamesToExclude, DefaultLoggerFactory);
    }

    /// <summary>
    /// Automatically configures Steeltoe packages that have been added to your project as NuGet references.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging, or use <see cref="BootstrapLoggerFactory" /> to write
    /// only to the console until logging is fully initialized.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddSteeltoe(this IWebHostBuilder builder, ILoggerFactory loggerFactory)
    {
        return AddSteeltoe(builder, EmptySet, loggerFactory);
    }

    /// <summary>
    /// Automatically configures Steeltoe packages that have been added to your project as NuGet references.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <param name="assemblyNamesToExclude">
    /// The set of assembly names to exclude from autoconfiguration. For ease of use, select from the constants in <see cref="SteeltoeAssemblyNames" />.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging, or use <see cref="BootstrapLoggerFactory" /> to write
    /// only to the console until logging is fully initialized.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddSteeltoe(this IWebHostBuilder builder, IReadOnlySet<string> assemblyNamesToExclude, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(assemblyNamesToExclude);
        ArgumentGuard.ElementsNotNullOrWhiteSpace(assemblyNamesToExclude);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);

        var scanner = new BootstrapScanner(wrapper, assemblyNamesToExclude, loggerFactory);
        scanner.ConfigureSteeltoe();

        return builder;
    }
}
