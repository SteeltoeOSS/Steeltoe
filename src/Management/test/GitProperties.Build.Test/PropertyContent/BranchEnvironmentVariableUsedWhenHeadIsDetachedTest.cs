// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test.PropertyContent;

public sealed class BranchEnvironmentVariableUsedWhenHeadIsDetachedTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);
        string commitId = await repository.GetCommitIdAsync();
        await repository.RunGitAsync("checkout", "--quiet", commitId);

        var environmentVariables = new Dictionary<string, string>
        {
            ["GITHUB_HEAD_REF"] = "refs/heads/feature/from-ci"
        };

        await repository.TestApp.BuildAsync(0, environmentVariables);

        Dictionary<string, string> properties = await repository.TestApp.ReadDebugPropertiesAsync();
        properties["git.branch"].Should().Be("feature/from-ci");
    }
}
