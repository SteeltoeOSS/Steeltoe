// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

internal static class DotNetCommandOutputExtensions
{
    public static DotNetCommandOutputAssertions Should(this DotNetCommandOutput subject)
    {
        return new DotNetCommandOutputAssertions(subject);
    }
}
