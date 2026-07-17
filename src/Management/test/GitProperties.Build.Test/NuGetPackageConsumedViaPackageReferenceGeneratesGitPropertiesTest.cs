// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class NuGetPackageConsumedViaPackageReferenceGeneratesGitPropertiesTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// Every other test here consumes Steeltoe.Management.GitProperties.Build straight from source (ProjectReference + Import) - this is the only one that
    /// goes through a real, packed .nupkg via &lt;PackageReference&gt;, the way an actual external user of the package would. That exercises the NuGet
    /// "build\{PackageId}.targets" auto-import convention end-to-end (no explicit &lt;Import&gt; anywhere in the consumer project) and the in-process
    /// (non-dev-loop) task-loading branch (SourceCheckout.txt is never packed, so it's absent in this layout - see $(GitPropertiesTaskHost) in
    /// Steeltoe.Management.GitProperties.Build.targets). Isolated per andrewlock.net's "Creating a source generator, part 3" approach: a local folder feed
    /// (just our own freshly-packed .nupkg, via a nuget.config with &lt;clear/&gt;) and a per-test RestorePackagesPath, so this never touches - or gets a
    /// stale result from - the machine-wide global-packages cache at %userprofile%\.nuget\packages.
    /// </summary>
    [Fact]
    public async Task NuGetPackage_ConsumedViaPackageReference_GeneratesGitProperties()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1);

        string feedDirectory = await Workspace.PackGitPropertiesBuildToFeedAsync();
        string packageId = await TestPaths.GetPackageIdAsync();

        string[] nuPkgFiles = Directory.GetFiles(feedDirectory, $"{packageId}.*.nupkg");
        nuPkgFiles.Should().ContainSingle("packing should produce exactly one .nupkg.");

        var nuPkgVersionRegex = new Regex($@"^{Regex.Escape(packageId)}\.(.+)\.nupkg$", RegexOptions.None, TimeSpan.FromSeconds(1));
        Match versionMatch = nuPkgVersionRegex.Match(Path.GetFileName(nuPkgFiles[0]));
        versionMatch.Success.Should().BeTrue("the .nupkg file name should embed the package version.");
        string packageVersion = versionMatch.Groups[1].Value;

        string consumerDirectory = Path.Combine(repository, "Consumer");
        await GitPropertiesTestWorkspace.CreatePackageConsumerProjectAsync(consumerDirectory, packageVersion);
        await GitPropertiesTestWorkspace.WriteIsolatedNuGetConfigAsync(Path.Combine(consumerDirectory, "nuget.config"), feedDirectory);

        string isolatedPackagesPath = Path.Combine(Workspace.RootDirectory, "isolated-packages");
        string result = await ProcessRunner.RunDotnetAsync(consumerDirectory, "build", $"-p:RestorePackagesPath={isolatedPackagesPath}");
        result.Should().Contain("0 Warning(s)", "a real package consumer should see no in-process task-loading fallback warning or any other diagnostic.");

        // NuGet always lowercases the package ID for the on-disk global-packages-folder layout - this isn't
        // an arbitrary case normalization, so ToUpperInvariant() (as generally preferred) would look here for
        // a folder that NuGet never creates.
#pragma warning disable S4040
        string lowerCasePackageId = packageId.ToLowerInvariant();
#pragma warning restore S4040

        Directory.Exists(Path.Combine(isolatedPackagesPath, lowerCasePackageId, packageVersion)).Should().BeTrue(
            "the package should restore into the isolated path, never the machine-wide global-packages cache.");

        Dictionary<string, string> properties = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(consumerDirectory));
        string expectedCommitId = await ProcessRunner.GetGitOutputAsync(repository, "rev-parse", "HEAD");
        properties["git.commit.id"].Should().Be(expectedCommitId);
    }
}
