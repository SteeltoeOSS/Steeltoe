// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class SmartDefaultSkipsGenerationWhenNoConsumingPackageReferenceTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);
        TestProject testApp = await repository.AddProjectAsync(GitPropertiesTestWorkspace.TestAppProjectName, generateGitProperties: null);
        DotNetCommandOutput output = await testApp.BuildAsync("-v:detailed");
        output.Value.Should().Contain("git.properties generation skipped: no reference to");
        testApp.GitPropertiesGenerated.Should().BeFalse();
    }
}
