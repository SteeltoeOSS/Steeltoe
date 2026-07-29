// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build;

internal sealed class TagDescription(string baseDescribe, string closestTagName, string closestTagCommitCount)
{
    public static TagDescription Empty { get; } = new(string.Empty, string.Empty, string.Empty);

    public string BaseDescribe { get; } = baseDescribe;
    public string ClosestTagName { get; } = closestTagName;
    public string ClosestTagCommitCount { get; } = closestTagCommitCount;
}
