// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Steeltoe.Management.Endpoint.RazorPagesTestWebApp.Pages;
using Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes;

namespace Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings;

public sealed class RazorPagesWebApplicationFactory : WebApplicationFactory<IndexModel>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        string? testAssemblyName = typeof(RazorPagesExternalAppTest).Assembly.GetName().Name;
        string? appAssemblyName = typeof(IndexModel).Assembly.GetName().Name;

        string absoluteContentRoot = System.Environment.CurrentDirectory;
        absoluteContentRoot = absoluteContentRoot.Replace($"/{testAssemblyName}/", $"/{appAssemblyName}/", StringComparison.Ordinal);
        absoluteContentRoot = absoluteContentRoot.Replace($@"\{testAssemblyName}\", $@"\{appAssemblyName}\", StringComparison.Ordinal);

        // Workaround for https://github.com/dotnet/aspnetcore/issues/55867.
        builder.UseContentRoot(absoluteContentRoot);

        // Workaround for https://github.com/dotnet/aspnetcore/issues/55867#issuecomment-3046941805.
        builder.UseEnvironment("Production");
    }
}
