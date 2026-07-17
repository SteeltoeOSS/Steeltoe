// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class ShallowCloneLeavesCommitCountsEmptyTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository source = await Workspace.CreateGitRepositoryAsync("source", 3);
        await source.TagAsync("v1.0.0");
        GitRepository shallow = await source.CloneAsShallowAsync("shallow");
        string isShallowRepository = await shallow.RunGitAsync("rev-parse", "--is-shallow-repository");
        isShallowRepository.Should().Be("true");

        string result = await shallow.TestApp.BuildAsync();
        result.Should().NotContain("GITPROPS001");
        result.Should().NotContain("GITPROPS002");
        result.AssertWarned("GITPROPS006");

        Dictionary<string, string> properties = await shallow.TestApp.ReadDebugPropertiesAsync();
        properties["git.total.commit.count"].Should().BeEmpty();
        properties["git.closest.tag.commit.count"].Should().BeEmpty();
    }
}
