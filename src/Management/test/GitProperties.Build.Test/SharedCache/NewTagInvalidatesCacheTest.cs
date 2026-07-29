// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test.SharedCache;

public sealed class NewTagInvalidatesCacheTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);
        await repository.TestApp.BuildAsync();
        Dictionary<string, string> propertiesBefore = await repository.TestApp.ReadDebugPropertiesAsync();
        propertiesBefore["git.tags"].Should().BeEmpty();

        string ancestorCommitId = await repository.RunGitAsync("rev-parse", "HEAD~1");
        await repository.TagAsync("release-1.0", ancestorCommitId);
        await repository.TestApp.BuildAsync();
        Dictionary<string, string> propertiesAfter = await repository.TestApp.ReadDebugPropertiesAsync();
        propertiesAfter["git.tags"].Should().BeEmpty();
        propertiesAfter["git.closest.tag.name"].Should().Be("release-1.0");
        propertiesAfter["git.closest.tag.commit.count"].Should().Be("1");
        propertiesAfter["git.commit.id.describe"].Should().Be("release-1.0-1");
    }
}
