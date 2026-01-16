// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Xunit;
using Xunit.v3;

namespace Steeltoe.Common.TestResources;

/// <summary>
/// Skips running the decorated test on the specified platforms.
/// </summary>
/// <remarks>
/// A common reason for skipping tests on macOS is because the ASP.NET dev certificate is not trusted. See
/// https://github.com/dotnet/aspnetcore/issues/42273.
/// </remarks>
[XunitTestCaseDiscoverer(typeof(TheoryDiscoverer))]
[AttributeUsage(AttributeTargets.Method)]
public sealed class TheorySkippedOnPlatformAttribute : TheoryAttribute
{
    public TheorySkippedOnPlatformAttribute(params string[] platformNames)
    {
        foreach (OSPlatform platform in platformNames.Select(OSPlatform.Create))
        {
            if (RuntimeInformation.IsOSPlatform(platform))
            {
                Skip = $"Skipping test on incompatible platform {platform}.";
                break;
            }
        }
    }
}
