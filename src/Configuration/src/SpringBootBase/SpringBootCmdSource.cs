// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.CommandLine;

namespace Steeltoe.Extensions.Configuration.SpringBoot;

/// <summary>
/// Configuration source used in creating a <see cref="SpringBootCmdProvider" /> that translates spring style CommandLine arguments to .NET.
/// </summary>
public class SpringBootCmdSource : IConfigurationSource
{
    internal IConfiguration Config;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpringBootCmdSource" /> class.
    /// </summary>
    /// <param name="configuration">
    /// The <see cref="CommandLineConfigurationProvider" /> provider by the framework.
    /// </param>
    public SpringBootCmdSource(IConfiguration configuration)
    {
        Config = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new SpringBootCmdProvider(Config);
    }
}
