// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class MultiProjectSharesCacheTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task MultiProject_SharesCache()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 2);

        // Two independent projects at the repo root, each with their own ProjectReference/Import
        // pointing at the SAME sibling Steeltoe.Management.GitProperties.Build copy (CreateSyntheticRepo
        // already placed TestApp there; reuse that exact relative layout for ProjectA/ProjectB by placing them
        // at the repo root too, siblings of TestApp and "src").
        await TestProjectWriter.WriteAppProjectAsync(repository, "ProjectA");
        await TestProjectWriter.WriteAppProjectAsync(repository, "ProjectB");

        string projectA = Path.Combine(repository, "ProjectA");
        string projectB = Path.Combine(repository, "ProjectB");

        string resultA = await ProcessRunner.RunDotnetAsync(projectA, "build", "-v:detailed");

        resultA.Should().Contain("git.properties: generating shared cache",
            "ProjectA (first to build) should be the one that actually generates the shared cache.");

        string cacheFile = Path.Combine(repository, "obj", "_GitProperties", "git.properties.cache");
        File.Exists(cacheFile).Should().BeTrue("ProjectA's build should have generated the shared cache.");

        string resultB = await ProcessRunner.RunDotnetAsync(projectB, "build", "-v:detailed");

        // "did not log generating the cache" is itself the deterministic, sufficient proof ProjectB
        // reused ProjectA's cache instead of rewriting it - no last-write-time comparison (and the
        // sleep it would otherwise need) is needed on top of it.
        resultB.Should().NotContain("git.properties: generating shared cache", "ProjectB should reuse ProjectA's cache instead of regenerating it.");

        Dictionary<string, string> propertiesA = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(projectA));
        Dictionary<string, string> propertiesB = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(projectB));
        propertiesB["git.commit.id"].Should().Be(propertiesA["git.commit.id"]);
    }
}
