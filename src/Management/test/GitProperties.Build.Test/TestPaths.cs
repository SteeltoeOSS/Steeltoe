// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Steeltoe.Management.GitProperties.Build.Test;

internal static partial class TestPaths
{
    public const string GitPropertiesBuildRelativePath = "src/Management/src/GitProperties.Build";

    public static readonly string TestAppTargetFramework = ResolveTestAppTargetFramework();
    public static readonly string MultiTargetTestFrameworks = BuildMultiTargetTestFrameworks();

    public static readonly string[] SharedBuildInfrastructureFiles =
    [
        "shared.props",
        "shared-package.props",
        "shared-project.props",
        "versions.props",
        "stylecop.json",
        "PackageIcon.png",
        "PackageReadme.md",
        "Steeltoe.Debug.ruleset",
        "Steeltoe.Release.ruleset"
    ];

    private static readonly Task<string> RepositoryRootTask = ResolveRepositoryRootAsync();

    public static Task<string> GetRepositoryRootAsync()
    {
        return RepositoryRootTask;
    }

    public static async Task<string> GetGitPropertiesBuildDirectoryAsync()
    {
        string repositoryRoot = await RepositoryRootTask;
        return Path.Combine(repositoryRoot, "src", "Management", "src", "GitProperties.Build");
    }

    public static async Task<string> GetGitPropertiesBuildProjectFileAsync()
    {
        string directory = await GetGitPropertiesBuildDirectoryAsync();
        return Path.Combine(directory, "Steeltoe.Management.GitProperties.Build.csproj");
    }

    public static async Task<string> GetTargetsFileAsync()
    {
        string directory = await GetGitPropertiesBuildDirectoryAsync();
        return Path.Combine(directory, "build", "Steeltoe.Management.GitProperties.Build.targets");
    }

    public static async Task<string> GetSourceCheckoutMarkerFileAsync()
    {
        string directory = await GetGitPropertiesBuildDirectoryAsync();
        return Path.Combine(directory, "SourceCheckout.txt");
    }

    public static async Task<string> GetPackageIdAsync()
    {
        string projectFile = await GetGitPropertiesBuildProjectFileAsync();
        return Path.GetFileNameWithoutExtension(projectFile);
    }

    public static async Task<string> GetGitPropertiesBuildTargetFrameworkAsync()
    {
        string projectFile = await GetGitPropertiesBuildProjectFileAsync();
        string projectContent = await File.ReadAllTextAsync(projectFile, TestContext.Current.CancellationToken);
        Match match = TargetFrameworkRegex().Match(projectContent);

        if (!match.Success)
        {
            throw new InvalidOperationException($"Could not find <TargetFramework> in {projectFile}.");
        }

        return match.Groups[1].Value;
    }

    [GeneratedRegex("<TargetFramework>(.+?)</TargetFramework>")]
    private static partial Regex TargetFrameworkRegex();

    private static string BuildMultiTargetTestFrameworks()
    {
        Match match = NetTfmRegex().Match(TestAppTargetFramework);

        if (!match.Success)
        {
            throw new InvalidOperationException($"Could not parse a 'netX.0'-style TFM from '{TestAppTargetFramework}'.");
        }

        int majorVersion = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        return $"{TestAppTargetFramework};net{majorVersion - 1}.0";
    }

    [GeneratedRegex(@"^net(\d+)\.0$")]
    private static partial Regex NetTfmRegex();

    private static string ResolveTestAppTargetFramework()
    {
        AssemblyMetadataAttribute? attribute = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(candidate => candidate.Key == "TargetFramework");

        return attribute?.Value ?? throw new InvalidOperationException("Could not resolve this test assembly's own TargetFramework from its AssemblyMetadata.");
    }

    private static async Task<string> ResolveRepositoryRootAsync([CallerFilePath] string sourceFilePath = "")
    {
        string sourceDirectory = Path.GetDirectoryName(sourceFilePath) ?? throw new InvalidOperationException("Could not determine the test source directory.");
        string output = await ProcessRunner.RunGitAsync(sourceDirectory, CancellationToken.None, "rev-parse", "--show-toplevel");
        return output.Trim().Replace('/', Path.DirectorySeparatorChar);
    }
}
