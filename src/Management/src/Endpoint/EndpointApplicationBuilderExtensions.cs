// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint;

public static class EndpointApplicationBuilderExtensions
{
    public static IApplicationBuilder UseActuators(this IApplicationBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        return builder.UseEndpoints(endpoints => endpoints.MapAllActuators());
    }
}
