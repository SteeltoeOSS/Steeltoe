// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.TestResources;

public enum HostBuilderType
{
    /// <summary>
    /// See <see cref="Microsoft.Extensions.Hosting.IHost" />.
    /// </summary>
    Host,

    /// <summary>
    /// See <see cref="Microsoft.AspNetCore.Hosting.IWebHost" />.
    /// </summary>
    WebHost,

    /// <summary>
    /// See <see cref="Microsoft.AspNetCore.Builder.WebApplication" />.
    /// </summary>
    WebApplication
}
