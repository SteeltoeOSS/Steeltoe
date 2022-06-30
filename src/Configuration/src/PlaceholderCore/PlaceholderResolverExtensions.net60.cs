// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Extensions.Configuration.Placeholder;

public static partial class PlaceholderResolverExtensions
{
    /// <summary>
    /// Add a placeholder resolver configuration source to the <see cref="ConfigurationBuilder"/>. The placeholder resolver source will capture and wrap all
    /// the existing sources <see cref="IConfigurationSource"/> contained in the builder.  The newly created source will then replace the existing sources
    /// and provide placeholder resolution for the configuration. Typically you will want to add this configuration source as the last one so that you wrap all
    /// of the applications configuration sources with place holder resolution.
    /// </summary>
    /// <param name="applicationBuilder">Your <see cref="WebApplicationBuilder"/>.</param>
    /// <param name="loggerFactory">the log factory to use.</param>
    /// <returns>provided host builder.</returns>
    public static WebApplicationBuilder AddPlaceholderResolver(this WebApplicationBuilder applicationBuilder, ILoggerFactory loggerFactory = null)
    {
        applicationBuilder.Configuration.AddPlaceholderResolver(loggerFactory);
        return applicationBuilder;
    }
}
#endif
