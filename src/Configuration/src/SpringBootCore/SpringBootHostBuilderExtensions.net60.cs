// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER
using System;
using Microsoft.AspNetCore.Builder;

namespace Steeltoe.Extensions.Configuration.SpringBoot;

public static partial class SpringBootHostBuilderExtensions
{
    /// <summary>
    ///  Sets up the configuration provider in spring boot style '.' separated values in CommandLine or as SPRING_APPLICATION_JSON Environment variable.
    /// </summary>
    /// <param name="builder"><see cref="WebApplicationBuilder"/>.</param>
    public static WebApplicationBuilder AddSpringBootConfiguration(this WebApplicationBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Configuration.AddSpringBootEnv();
        builder.Configuration.AddSpringBootCmd(builder.Configuration);
        return builder;
    }
}
#endif
