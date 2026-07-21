// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class PublishIncludesGitPropertiesTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);
        await repository.TestApp.PublishAsync();

        Dictionary<string, string> properties = await repository.TestApp.ReadReleasePublishPropertiesAsync();
        string expectedCommitId = await repository.GetCommitIdAsync();
        properties["git.commit.id"].Should().Be(expectedCommitId);
    }
}
