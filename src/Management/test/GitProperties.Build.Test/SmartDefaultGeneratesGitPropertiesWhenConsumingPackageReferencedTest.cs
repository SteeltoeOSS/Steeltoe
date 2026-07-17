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
    public async Task SmartDefault_GeneratesGitProperties_WhenConsumingPackageReferenced()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1);
        const string consumingPackageStandInName = "Steeltoe.Management.Endpoint";
        await GitPropertiesTestWorkspace.WriteDummyDependencyProjectAsync(repository, consumingPackageStandInName);

        string testApp = await GitPropertiesTestWorkspace.WriteAppProjectAsync(repository, GitPropertiesTestWorkspace.TestAppProjectName,
            generateGitProperties: null,
            extraItemGroupContent: $"""<ProjectReference Include="..\{consumingPackageStandInName}\{consumingPackageStandInName}.csproj" />""");

        await ProcessRunner.RunDotnetAsync(testApp, "build");

        Dictionary<string, string> properties = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(testApp));
        string expectedCommitId = await ProcessRunner.GetGitOutputAsync(repository, "rev-parse", "HEAD");
        properties["git.commit.id"].Should().Be(expectedCommitId);
    }
}
