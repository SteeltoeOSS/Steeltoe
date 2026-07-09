// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Steeltoe.Management.GitProperties.Build.Test;

/// <summary>
/// Resolves paths to the CURRENT source of Steeltoe.Management.GitProperties.Build (not a stale git-tracked copy), so every test exercises whatever is
/// on disk right now. Resolved once per test run via the test source file's own location, so these tests work regardless of where the test assembly's
/// bin/ output lives.
/// </summary>
internal static partial class TestPaths
{
    /// <summary>
    /// The relative path (from a project sitting at the root of a test workspace) back to the copied Steeltoe.Management.GitProperties.Build source - see
    /// <see cref="GitPropertiesTestWorkspace" />. Mirrors the real repository's own "src/Management/src/GitProperties.Build" layout, since
    /// Steeltoe.Management.GitProperties.Build.csproj's own "shared.props" import depends on being at that exact relative depth from the repository root.
    /// </summary>
    public const string GitPropertiesBuildRelativePath = "src/Management/src/GitProperties.Build";

    /// <summary>
    /// The TargetFramework every generated test-app .csproj (TestApp, ProjA/ProjB, the PackageReference consumer) is written with - read from this test
    /// assembly's own $(TargetFramework) via the AssemblyMetadata item in Steeltoe.Management.GitProperties.Build.Test.csproj, so bumping that one property
    /// is the only change needed when a new TFM becomes current.
    /// </summary>
    public static readonly string TestAppTargetFramework = ResolveTestAppTargetFramework();

    /// <summary>
    /// A semicolon-separated "&lt;TargetFrameworks&gt;" value covering the current TFM (<see cref="TestAppTargetFramework" />) plus the one immediately
    /// before it, in that order - e.g. "net10.0;net9.0". Computed rather than hardcoded, so this stays current automatically as the repository's own
    /// baseline TFM moves forward. Used only by tests that specifically need a multi-targeted consumer project (MSBuild builds a multi-targeted project's
    /// inner TFMs concurrently by default), rather than every generated test-app project.
    /// </summary>
    public static readonly string MultiTargetTestFrameworks = BuildMultiTargetTestFrameworks();

    public static readonly string RepositoryRoot = ResolveRepositoryRoot();

    public static readonly string GitPropertiesBuildDirectory = Path.Combine(RepositoryRoot, "src", "Management", "src", "GitProperties.Build");
    public static readonly string GitPropertiesBuildProjectFile = Path.Combine(GitPropertiesBuildDirectory, "Steeltoe.Management.GitProperties.Build.csproj");
    public static readonly string TargetsFile = Path.Combine(GitPropertiesBuildDirectory, "build", "Steeltoe.Management.GitProperties.Build.targets");
    public static readonly string SourceCheckoutMarkerFile = Path.Combine(GitPropertiesBuildDirectory, "SourceCheckout.txt");

    /// <summary>
    /// The NuGet package/assembly ID, derived from the real .csproj's own file name rather than retyped, so a future rename only needs to happen once.
    /// </summary>
    public static readonly string PackageId = Path.GetFileNameWithoutExtension(GitPropertiesBuildProjectFile);

    /// <summary>
    /// Steeltoe.Management.GitProperties.Build.csproj's own &lt;TargetFramework&gt; (netstandard2.0, as of this writing) - parsed from the real .csproj
    /// rather than hardcoded, so a future retargeting can't silently desync this from the test that packs and consumes the compiled task assembly.
    /// </summary>
    public static readonly string GitPropertiesBuildTargetFramework = ResolveGitPropertiesBuildTargetFramework();

    /// <summary>
    /// Root-level Steeltoe build infrastructure files that Steeltoe.Management.GitProperties.Build.csproj's own "shared.props" import (and that file's own
    /// imports) needs to resolve. Copied verbatim into every synthetic test repo, at the repository root, so a project built from this source outside the
    /// real Steeltoe checkout still evaluates identically (analyzers, versioning, packaging properties, etc.).
    /// </summary>
    public static readonly string[] SharedBuildInfrastructureFiles =
    [
        "shared.props",
        "shared-package.props",
        "shared-project.props",
        "versions.props",
        "Directory.Build.targets",
        "stylecop.json",
        "PackageIcon.png",
        "PackageReadme.md",
        "Steeltoe.Debug.ruleset",
        "Steeltoe.Release.ruleset"
    ];

    private static string ResolveGitPropertiesBuildTargetFramework()
    {
        string projectContent = File.ReadAllText(GitPropertiesBuildProjectFile);
        Match match = TargetFrameworkRegex().Match(projectContent);

        if (!match.Success)
        {
            throw new InvalidOperationException($"Could not find <TargetFramework> in {GitPropertiesBuildProjectFile}.");
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

    private static string ResolveRepositoryRoot([CallerFilePath] string sourceFilePath = "")
    {
        string sourceDirectory = Path.GetDirectoryName(sourceFilePath) ?? throw new InvalidOperationException("Could not determine the test source directory.");
        ProcessResult result = ProcessRunner.Run(ProcessRunner.RealGitExecutable, sourceDirectory, "rev-parse", "--show-toplevel");

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Could not resolve the repository root from {sourceDirectory}.");
        }

        return result.Output.Trim().Replace('/', Path.DirectorySeparatorChar);
    }
}
