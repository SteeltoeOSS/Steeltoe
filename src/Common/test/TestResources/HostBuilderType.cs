// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Steeltoe.Common.TestResources;

public enum HostBuilderType
{
    /// <summary>
    /// Represents <see cref="HostBuilder" />, which builds an <see cref="IHost" />. Introduced in .NET Core 1.0.
    /// </summary>
    Host,

    /// <summary>
    /// Represents <see cref="WebHostBuilder" />, which builds an <see cref="IWebHost" />. Introduced in .NET Core 1.0.
    /// </summary>
    WebHost,

    /// <summary>
    /// Represents <see cref="WebApplicationBuilder" />, which builds a <see cref="WebApplication" />. Introduced in .NET 6.
    /// </summary>
    WebApplication,

    /// <summary>
    /// Represents <see cref="HostApplicationBuilder" />, which builds an <see cref="IHost" />. Introduced in .NET 6.
    /// </summary>
    HostApplication
}
