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
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1);
        const string customPackageId = "Contoso.Actuators";
        await GitPropertiesTestWorkspace.WriteDummyDependencyProjectAsync(repository, customPackageId);

        string testApp = await GitPropertiesTestWorkspace.WriteAppProjectAsync(repository, GitPropertiesTestWorkspace.TestAppProjectName,
            generateGitProperties: null, extraItemGroupContent: $"""<ProjectReference Include="..\{customPackageId}\{customPackageId}.csproj" />""");

        await ProcessRunner.RunDotnetAsync(testApp, "build", $"-p:GitPropertiesConsumingPackageIds={customPackageId}");

        Dictionary<string, string> properties = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(testApp));
        string expectedCommitId = await ProcessRunner.GetGitOutputAsync(repository, "rev-parse", "HEAD");
        properties["git.commit.id"].Should().Be(expectedCommitId);
    }
}
