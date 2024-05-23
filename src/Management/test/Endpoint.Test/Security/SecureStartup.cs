// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;

namespace Steeltoe.Management.Endpoint.Test.Security;

public sealed class SecureStartup : Startup
{
    public override void Configure(IApplicationBuilder app)
    {
        app.UseMiddleware<SetsUserInContextForTestsMiddleware>();
        base.Configure(app);
    }
}
