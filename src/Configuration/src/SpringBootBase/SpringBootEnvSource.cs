// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Extensions.Configuration.SpringBoot;

/// <summary>
/// Configuration source used in creating a <see cref="SpringBootEnvProvider"/>.
/// </summary>
public class SpringBootEnvSource : IConfigurationSource
{
    /// <summary>
    /// Builds a <see cref="SpringBootEnvProvider"/> from the sources.
    /// </summary>
    /// <param name="builder">the provided builder.</param>
    /// <returns>the SpringBootEnv provider.</returns>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new SpringBootEnvProvider();
    }
}
