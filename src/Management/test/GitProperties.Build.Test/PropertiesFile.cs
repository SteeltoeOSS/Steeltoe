// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace Steeltoe.Management.GitProperties.Build.Test;

internal static class PropertiesFile
{
    public static async Task<Dictionary<string, string>> ReadAsync(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"git.properties not found at: {path}");
        }

        var map = new Dictionary<string, string>();

        foreach (string line in await File.ReadAllLinesAsync(path, Encoding.UTF8, TestContext.Current.CancellationToken))
        {
            if (!line.StartsWith("git.", StringComparison.Ordinal))
            {
                continue;
            }

            int equalsIndex = line.IndexOf('=');

            if (equalsIndex < 0)
            {
                continue;
            }

            map[line[..equalsIndex]] = line[(equalsIndex + 1)..];
        }

        return map;
    }
}
