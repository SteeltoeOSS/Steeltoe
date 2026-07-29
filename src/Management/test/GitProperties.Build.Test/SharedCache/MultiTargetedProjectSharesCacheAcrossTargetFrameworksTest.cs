// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test.SharedCache;

public sealed class MultiTargetedProjectSharesCacheAcrossTargetFrameworksTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);
        TestProject testApp = await repository.AddProjectAsync("MultiTargetApp", TestAppTargetFramework.Multiple);
        DotNetCommandOutput buildOutputBefore = await testApp.BuildAsync();
        buildOutputBefore.Value.Should().Contain("git.properties: generating shared cache", Exactly.Once());

        string expectedCommitId = await repository.GetCommitIdAsync();
        List<Dictionary<string, string>> propertiesBefore = await testApp.ReadDebugPropertiesPerTargetFrameworkAsync(TestAppTargetFramework.Multiple);

        foreach (Dictionary<string, string> properties in propertiesBefore)
        {
            properties["git.commit.id"].Should().Be(expectedCommitId);
            properties["git.tags"].Should().BeEmpty();
        }

        await repository.TagAsync("v1.0.0");
        DotNetCommandOutput buildOutputAfter = await testApp.BuildAsync();
        buildOutputAfter.Value.Should().Contain("git.properties: generating shared cache", Exactly.Once());
        List<Dictionary<string, string>> propertiesAfter = await testApp.ReadDebugPropertiesPerTargetFrameworkAsync(TestAppTargetFramework.Multiple);

        foreach (Dictionary<string, string> properties in propertiesAfter)
        {
            properties["git.tags"].Should().Be("v1.0.0");
        }
    }
}
