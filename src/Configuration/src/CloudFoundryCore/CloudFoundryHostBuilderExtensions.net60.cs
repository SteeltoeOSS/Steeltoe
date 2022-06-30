// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Builder;

namespace Steeltoe.Extensions.Configuration.CloudFoundry;

public static partial class CloudFoundryHostBuilderExtensions
{
    /// <summary>
    /// Add Cloud Foundry Configuration Provider.
    /// </summary>
    /// <param name="applicationBuilder">Your <see cref="WebApplicationBuilder"/>.</param>
    public static WebApplicationBuilder AddCloudFoundryConfiguration(this WebApplicationBuilder applicationBuilder)
    {
        applicationBuilder.Configuration.AddCloudFoundry();
        applicationBuilder.Services.RegisterCloudFoundryApplicationInstanceInfo();
        return applicationBuilder;
    }
}
#endif
