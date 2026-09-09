// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build;

/// <summary>
/// Rules specific to the content/shape of a git.properties file.
/// </summary>
internal static class GitPropertiesFormat
{
    public const string CommitIdDescribeKey = "git.commit.id.describe";

    /// <summary>
    /// Collapses line breaks to a literal "\n" so a value can never span multiple physical lines. GitInfoContributor can't handle multiline values.
    /// </summary>
    public static string EscapeLineBreaks(string? value)
    {
        return value is null or "" ? string.Empty : value.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", "\\n");
    }
}
