// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text.RegularExpressions;

namespace Steeltoe.Management.GitProperties.Build;

internal static class GitOutputParser
{
    /// <summary>
    /// Matches "git version 2.42.0", "git version 2.42.0.windows.1", and "git version 2.39.5 (Apple Git-154)" alike, capturing only the leading
    /// major/minor/patch numbers every real git build's "--version" output starts with, regardless of whatever vendor-specific suffix follows.
    /// </summary>
    private static readonly Regex GitVersionRegex = new(@"^git version (\d+)\.(\d+)(?:\.(\d+))?", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    /// <summary>
    /// Parses the leading major/minor/patch numbers out of "git --version" output.
    /// </summary>
    public static Version? ParseGitVersion(string output)
    {
        Match match = GitVersionRegex.Match(output);

        if (!match.Success)
        {
            return null;
        }

        int major = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        int minor = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        // Git versions before 2006 did not always include the patch version. And even today, versions built from source can look like "2.44-rc0".
        int build = match.Groups[3].Success ? int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture) : 0;

        return new Version(major, minor, build);
    }

    /// <summary>
    /// Parses "git describe --tags --long --always" output for its three possible shapes: exactly-on-tag ("tag-0-gsha"), N-commits-ahead ("tag-N-gsha"), and
    /// no-tags-at-all (a bare "--always" fallback SHA, with no dashes). An empty or unrecognized shape yields all-empty fields, same as "no tags exist".
    /// </summary>
    public static TagDescription ParseTagDescribe(string describeOutput)
    {
        string baseDescribe = string.Empty;
        string closestTagName = string.Empty;
        string closestTagCommitCount = string.Empty;

        if (!string.IsNullOrEmpty(describeOutput))
        {
            int lastDashIndex = describeOutput.LastIndexOf('-');
            int secondLastDash = lastDashIndex >= 0 ? describeOutput.LastIndexOf('-', lastDashIndex - 1) : -1;

            bool hasTagPrefix = lastDashIndex >= 0 && secondLastDash >= 0 &&
                describeOutput.Substring(lastDashIndex + 1).StartsWith("g", StringComparison.Ordinal);

            if (!hasTagPrefix)
            {
                // No tags reachable at all. The "--always" fallback is a bare abbreviated SHA.
                baseDescribe = describeOutput;
            }
            else
            {
                closestTagName = describeOutput.Substring(0, secondLastDash);
                closestTagCommitCount = describeOutput.Substring(secondLastDash + 1, lastDashIndex - secondLastDash - 1);
                baseDescribe = closestTagCommitCount == "0" ? closestTagName : $"{closestTagName}-{closestTagCommitCount}";
            }
        }

        return new TagDescription(baseDescribe, closestTagName, closestTagCommitCount);
    }

    /// <summary>
    /// Parses "git config --list" output for the keys we care about, stripping any embedded credentials from the remote URL.
    /// </summary>
    public static GitConfig ParseConfig(string configListOutput)
    {
        string userName = string.Empty;
        string userEmail = string.Empty;
        string remoteUrl = string.Empty;

        foreach (string line in configListOutput.Split('\n'))
        {
            int equalsIndex = line.IndexOf('=');

            if (equalsIndex >= 0)
            {
                string key = line.Substring(0, equalsIndex).Trim();
                string value = line.Substring(equalsIndex + 1).Trim();

                if (string.Equals(key, "user.name", StringComparison.OrdinalIgnoreCase))
                {
                    userName = value;
                }
                else if (string.Equals(key, "user.email", StringComparison.OrdinalIgnoreCase))
                {
                    userEmail = value;
                }
                else if (string.Equals(key, "remote.origin.url", StringComparison.OrdinalIgnoreCase))
                {
                    remoteUrl = value;
                }
            }
        }

        string safeRemoteUrl = StripUserInfo(remoteUrl);
        return new GitConfig(userName, userEmail, safeRemoteUrl);
    }

    private static string StripUserInfo(string url)
    {
        if (!string.IsNullOrEmpty(url))
        {
            try
            {
                var uri = new Uri(url);

                if (!string.IsNullOrEmpty(uri.UserInfo))
                {
                    var builder = new UriBuilder(uri)
                    {
                        UserName = string.Empty,
                        Password = string.Empty
                    };

                    return builder.Uri.ToString();
                }
            }
            catch (UriFormatException)
            {
                // Not a parseable absolute URL (e.g. SCP-like "git@host:org/repo.git"), so there is nothing to strip.
            }
        }

        return url;
    }
}
