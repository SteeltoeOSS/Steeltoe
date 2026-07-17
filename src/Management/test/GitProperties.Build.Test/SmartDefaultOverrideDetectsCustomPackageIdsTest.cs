// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class SmartDefaultOverrideDetectsCustomPackageIdsTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// Proves $(GitPropertiesConsumingPackageIds) is genuinely overridable - for consumers of this package who don't use Steeltoe.Management.Endpoint at all
    /// (e.g. a hand-rolled /info endpoint reading git.properties directly), so the smart default isn't hardcoded away from them.
    /// </summary>
    [Fact]
    public async Task SmartDefault_Override_DetectsCustomPackageIds()
    {
        const string customPackageId = "Example.Package.Name";

        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);
        TestProject dependency = await repository.AddDependencyProjectAsync(customPackageId);

        TestProject testApp = await repository.AddProjectAsync(GitPropertiesTestWorkspace.TestAppProjectName, generateGitProperties: null,
            extraItemGroupContent: dependency.ToProjectReferenceXml());

        await testApp.BuildAsync($"-p:GitPropertiesConsumingPackageIds={customPackageId}");

        Dictionary<string, string> properties = await testApp.ReadDebugPropertiesAsync();
        string expectedCommitId = await repository.GetCommitIdAsync();
        properties["git.commit.id"].Should().Be(expectedCommitId);
    }
}
