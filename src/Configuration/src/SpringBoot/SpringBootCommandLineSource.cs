// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration.SpringBoot;

/// <summary>
/// Configuration source used in creating a <see cref="SpringBootCommandLineProvider" /> that translates spring-style command-line arguments to .NET.
/// </summary>
internal sealed class SpringBootCommandLineSource : IConfigurationSource
{
    internal IConfiguration Configuration { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpringBootCommandLineSource" /> class.
    /// </summary>
    /// <param name="configuration">
    /// The <see cref="IConfiguration" /> to read application settings from.
    /// </param>
    public SpringBootCommandLineSource(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        Configuration = configuration;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new SpringBootCommandLineProvider(Configuration);
    }
}
