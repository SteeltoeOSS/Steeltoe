// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace Steeltoe.Management.GitProperties.Build.Test;

/// <summary>
/// Builds and packs the source project into an isolated local NuGet feed.
/// </summary>
internal static class GitPropertiesSourcePackager
{
    private const string PackageId = "Steeltoe.Management.GitProperties.Build";
    private const string PackageVersionPrefix = "1.2.3-test";

    public static async Task<PackageReference> PackAsync(string nuGetFeedDirectory)
    {
        string repositoryRoot = await ResolveRepositoryRootAsync();
        string sourceDirectory = Path.Combine(repositoryRoot, "src", "Management", "src", "GitProperties.Build");

        string packageVersion = $"{PackageVersionPrefix}.{$"{Guid.NewGuid():N}"[..8]}";
        await ProcessRunner.RunDotNetAsync(sourceDirectory, 0, null, "build", "-c", "Release", $"-p:Version={packageVersion}");

        string packageSourcePath = Path.Combine(sourceDirectory, "bin", "tasks", "netstandard2.0", $"{PackageId}.{packageVersion}.nupkg");
        string packageDestinationPath = Path.Combine(nuGetFeedDirectory, Path.GetFileName(packageSourcePath));
        File.Move(packageSourcePath, packageDestinationPath);

        return new PackageReference(PackageId, packageVersion, null);
    }

    private static async Task<string> ResolveRepositoryRootAsync([CallerFilePath] string sourceFilePath = "")
    {
        string sourceDirectory = Path.GetDirectoryName(sourceFilePath) ?? throw new InvalidOperationException("Could not determine the test source directory.");
        string output = await ProcessRunner.RunGitAsync(sourceDirectory, CancellationToken.None, "rev-parse", "--show-toplevel");
        return output.Trim().Replace('/', Path.DirectorySeparatorChar);
    }
}
