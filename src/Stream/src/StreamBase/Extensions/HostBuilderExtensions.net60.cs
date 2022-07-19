// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Extensions.Configuration.SpringBoot;
using Steeltoe.Stream.StreamHost;

namespace Steeltoe.Stream.Extensions;

public static partial class HostBuilderExtensions
{
    public static WebApplicationBuilder AddStreamServices<T>(this WebApplicationBuilder builder)
    {
        builder.AddSpringBootConfiguration();
        builder.Services.AddStreamServices<T>(builder.Configuration);
        builder.Services.AddHostedService<StreamLifeCycleService>();
        return builder;
    }
}
#endif
