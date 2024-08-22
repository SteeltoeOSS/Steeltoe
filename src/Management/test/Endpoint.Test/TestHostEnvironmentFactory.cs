// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;

namespace Steeltoe.Management.Endpoint.Test;

public static class TestHostEnvironmentFactory
{
    private const string TestAppName = "TestApp";

    public static IHostEnvironment Create()
    {
        return Create("Test");
    }

    private static HostingEnvironment Create(string environmentName)
    {
        ArgumentNullException.ThrowIfNull(environmentName);

        return new HostingEnvironment
        {
            EnvironmentName = environmentName,
            ApplicationName = TestAppName
        };
    }
}
