// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Extensions.Configuration.SpringBoot;

/// <summary>
/// Configuration source used in creating a <see cref="SpringBootEnvironmentVariableProvider" />.
/// </summary>
public sealed class SpringBootEnvironmentVariableSource : IConfigurationSource
{
    /// <summary>
    /// Builds a <see cref="SpringBootEnvironmentVariableProvider" /> from the sources.
    /// </summary>
    /// <param name="builder">
    /// The configuration builder.
    /// </param>
    /// <returns>
    /// <see cref="SpringBootEnvironmentVariableProvider" />.
    /// </returns>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new SpringBootEnvironmentVariableProvider();
    }
}
