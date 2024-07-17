// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;

namespace Steeltoe.Common.TestResources;

public static class HostingHelpers
{
    public const string TestAppName = "TestApp";

    public static readonly Action<IApplicationBuilder> EmptyAction = _ =>
    {
    };

    public static IHostEnvironment GetHostingEnvironment()
    {
        return GetHostingEnvironment("EnvironmentName");
    }

    public static IHostEnvironment GetHostingEnvironment(string environmentName)
    {
        return new HostingEnvironment
        {
            EnvironmentName = environmentName,
            ApplicationName = TestAppName
        };
    }
}
