// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class NuGetPackageConsumedViaPackageReferenceGeneratesGitPropertiesTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);
        string feedDirectory = await Workspace.PackGitPropertiesBuildToFeedAsync();
        string packageId = await TestPaths.GetPackageIdAsync();
        string[] nuPkgFiles = Directory.GetFiles(feedDirectory, $"{packageId}.*.nupkg");
        nuPkgFiles.Should().ContainSingle();

        var nuPkgVersionRegex = new Regex($@"^{Regex.Escape(packageId)}\.(.+)\.nupkg$", RegexOptions.None, TimeSpan.FromSeconds(1));
        Match versionMatch = nuPkgVersionRegex.Match(Path.GetFileName(nuPkgFiles[0]));
        versionMatch.Success.Should().BeTrue();

        string packageVersion = versionMatch.Groups[1].Value;
        TestProject consumer = await repository.AddPackageConsumerProjectAsync("Consumer", packageVersion);
        await Workspace.WriteIsolatedNuGetConfigAsync(consumer, feedDirectory);
        string isolatedPackagesPath = Workspace.GetPath("isolated-packages");
        DotNetCommandOutput output = await consumer.BuildAsync($"-p:RestorePackagesPath={isolatedPackagesPath}");
        output.Value.Should().Contain("0 Warning(s)");

#pragma warning disable S4040
        // Justification: NuGet always lowercases the package ID for the on-disk global-packages-folder layout.
        string lowerCasePackageId = packageId.ToLowerInvariant();
#pragma warning restore S4040
        Directory.Exists(Path.Combine(isolatedPackagesPath, lowerCasePackageId, packageVersion)).Should().BeTrue();

        Dictionary<string, string> properties = await consumer.ReadDebugPropertiesAsync();
        string expectedCommitId = await repository.GetCommitIdAsync();
        properties["git.commit.id"].Should().Be(expectedCommitId);
    }
}
