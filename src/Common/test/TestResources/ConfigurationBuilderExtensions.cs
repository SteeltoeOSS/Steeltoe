// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Common.TestResources;

public static class ConfigurationBuilderExtensions
{
    /// <summary>
    /// Builds the configuration and returns it as <see cref="ConfigurationRoot" />, so tests can easily dispose it.
    /// </summary>
    /// <param name="builder">
    /// The configuration builder.
    /// </param>
    /// <remarks>
    /// There are basically two patterns to ensure configuration providers are properly disposed in tests.
    /// <para>
    /// <example>
    /// 1. Simple tests without a service provider in a using statement:<![CDATA[
    /// using var root = new ConfigurationBuilder().Add(...).BuildAsRoot();
    /// ]]>
    /// </example>
    /// </para>
    /// <para>
    /// <example>
    /// 2. Tests where the service provider disposes the configuration by passing a lambda:
    /// <![CDATA[
    /// var root = new ConfigurationBuilder().Add(...).Build();
    /// 
    /// var services = new ServiceCollection();
    /// services.AddSingleton<IConfiguration>(_ => root);
    /// ]]>
    /// </example>
    /// </para>
    /// </remarks>
    public static ConfigurationRoot BuildAsRoot(this IConfigurationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return (ConfigurationRoot)builder.Build();
    }

    /// <summary>
    /// Adds a source that is part of every build of the configuration and fails once it has been built too often. Use this in tests where a regression would
    /// make the configuration rebuild itself endlessly, which would otherwise overflow the stack and crash the test run instead of failing the test.
    /// </summary>
    /// <param name="builder">
    /// The configuration builder.
    /// </param>
    public static void SetRecursionGuard(this IConfigurationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Add(new DisposeCountingConfigurationSource());
    }
}
