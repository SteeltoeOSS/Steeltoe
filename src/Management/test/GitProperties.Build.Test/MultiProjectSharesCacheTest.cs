// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class MultiProjectSharesCacheTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 2);

        // Two independent projects at the repo root, each with their own ProjectReference/Import
        // pointing at the SAME sibling Steeltoe.Management.GitProperties.Build copy (CreateSyntheticRepository
        // already placed TestApp there; reuse that exact relative layout for ProjectA/ProjectB by placing them
        // at the repo root too, siblings of TestApp and "src").
        TestProject projectA = await repository.AddProjectAsync("ProjectA");
        TestProject projectB = await repository.AddProjectAsync("ProjectB");

        string resultA = await projectA.BuildAsync("-v:detailed");

        resultA.Should().Contain("git.properties: generating shared cache",
            "ProjectA (first to build) should be the one that actually generates the shared cache.");

        repository.SharedCacheExists.Should().BeTrue("ProjectA's build should have generated the shared cache.");

        string resultB = await projectB.BuildAsync("-v:detailed");

        // "did not log generating the cache" is itself the deterministic, sufficient proof ProjectB
        // reused ProjectA's cache instead of rewriting it - no last-write-time comparison (and the
        // sleep it would otherwise need) is needed on top of it.
        resultB.Should().NotContain("git.properties: generating shared cache", "ProjectB should reuse ProjectA's cache instead of regenerating it.");

        Dictionary<string, string> propertiesA = await projectA.ReadDebugPropertiesAsync();
        Dictionary<string, string> propertiesB = await projectB.ReadDebugPropertiesAsync();
        propertiesB["git.commit.id"].Should().Be(propertiesA["git.commit.id"]);
    }
}
