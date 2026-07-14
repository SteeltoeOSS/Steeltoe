// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build;

/// <summary>
/// Rules specific to the content/shape of a git.properties file.
/// </summary>
internal static class GitPropertiesFormat
{
    /// <summary>
    /// The git.properties key that <see cref="GenerateGitPropertiesCacheTask" /> writes and <see cref="ComposeGitPropertiesTask" /> later searches for (to
    /// append the "-dirty" suffix) - shared so the two can never silently drift out of sync with each other.
    /// </summary>
    public const string CommitIdDescribeKey = "git.commit.id.describe";

    /// <summary>
    /// Collapses real newlines to a literal "\n" so a value can never span multiple physical lines (Steeltoe's GitInfoContributor reads this file with
    /// File.ReadAllLinesAsync and would silently truncate a value at the first embedded newline otherwise). Colons are deliberately left unescaped -
    /// Steeltoe only unescapes "\:" back to ":", so leaving real colons alone (timestamps, URLs) is what round-trips correctly.
    /// </summary>
    public static string EscapeLineBreaks(string? value)
    {
        return value is null or "" ? string.Empty : value.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", "\\n");
    }
}
