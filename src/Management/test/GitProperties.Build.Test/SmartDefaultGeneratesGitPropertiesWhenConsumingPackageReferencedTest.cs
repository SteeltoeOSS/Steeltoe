// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class SmartDefaultGeneratesGitPropertiesWhenConsumingPackageReferencedTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// The positive counterpart to <see cref="SmartDefaultSkipsGenerationWhenNoConsumingPackageReferenceTest" />: a project referencing the real default
    /// consuming package ID (Steeltoe.Management.Endpoint) gets git.properties generated with no explicit $(GenerateGitProperties) needed. Uses a minimal
    /// stand-in project with that exact name/PackageId (see WriteDummyDependencyProjectAsync's remarks) rather than the real, large Endpoint project, so
    /// this test stays fast and fully offline.
    /// </summary>
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);
        TestProject dependency = await repository.AddDependencyProjectAsync("Steeltoe.Management.Endpoint");
        TestProject testApp = await repository.AddTestAppReferencingAsync(dependency);
        await testApp.BuildAsync();

        Dictionary<string, string> properties = await testApp.ReadDebugPropertiesAsync();
        string expectedCommitId = await repository.GetCommitIdAsync();
        properties["git.commit.id"].Should().Be(expectedCommitId);
    }
}
