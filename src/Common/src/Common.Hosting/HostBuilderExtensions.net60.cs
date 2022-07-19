// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER
using System;
using Microsoft.AspNetCore.Builder;

namespace Steeltoe.Common.Hosting;

public static partial class HostBuilderExtensions
{
    /// <summary>
    /// Configure the application to listen on port(s) provided by the environment at runtime. Defaults to port 8080.
    /// </summary>
    /// <param name="webApplicationBuilder">Your <see cref="WebApplicationBuilder"/>.</param>
    /// <param name="runLocalHttpPort">Set the Http port number with code so you don't need to set environment variables locally.</param>
    /// <param name="runLocalHttpsPort">Set the Https port number with code so you don't need to set environment variables locally.</param>
    /// <returns>Your <see cref="WebApplicationBuilder"/>, now listening on port(s) found in the environment or passed in.</returns>
    /// <remarks>
    /// runLocalPort parameter will not be used if an environment variable PORT is found<br /><br />
    /// THIS EXTENSION IS NOT COMPATIBLE WITH IIS EXPRESS.
    /// </remarks>
    public static WebApplicationBuilder UseCloudHosting(this WebApplicationBuilder webApplicationBuilder, int? runLocalHttpPort = null, int? runLocalHttpsPort = null)
    {
        if (webApplicationBuilder == null)
        {
            throw new ArgumentNullException(nameof(webApplicationBuilder));
        }

        webApplicationBuilder.WebHost.BindToPorts(runLocalHttpPort, runLocalHttpsPort);
        return webApplicationBuilder;
    }
}
#endif
