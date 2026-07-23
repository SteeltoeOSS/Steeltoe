// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Steeltoe.Management.GitProperties.Build.Test;

internal static partial class TestAppTargetFramework
{
    public static readonly string Default = Resolve();
    public static readonly string[] Multiple = ResolveMultiple();

    private static string Resolve()
    {
        AssemblyMetadataAttribute? attribute = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(candidate => candidate.Key == "TargetFramework");

        return attribute?.Value ?? throw new InvalidOperationException("Could not resolve this test assembly's own TargetFramework from its AssemblyMetadata.");
    }

    private static string[] ResolveMultiple()
    {
        Match match = NetTfmRegex().Match(Default);

        if (!match.Success)
        {
            throw new InvalidOperationException($"Could not parse a 'netX.0'-style TFM from '{Default}'.");
        }

        int majorVersion = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);

        return
        [
            Default,
            $"net{majorVersion - 1}.0"
        ];
    }

    [GeneratedRegex(@"^net(\d+)\.0$")]
    private static partial Regex NetTfmRegex();
}
