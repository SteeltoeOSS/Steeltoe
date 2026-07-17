// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class SmartDefaultOverrideEmptyPackageIdsViaGlobalPropertySkipsGenerationGracefullyTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// Guards against a regression where MSBuild's required-parameter check for a Task string parameter treats an empty string the same as "not supplied":
    /// setting $(GitPropertiesConsumingPackageIds) to blank via a global property (e.g. "-p:GitPropertiesConsumingPackageIds=") reaches
    /// DetectConsumingPackageReferenceTask.PackageIds unchanged (global properties can't be reassigned by the project's own conditional default at
    /// ResolveGitPropertiesPaths above), so PackageIds must NOT be [Required] - it must instead behave exactly like "no configured ID happens to match",
    /// i.e. skip generation gracefully rather than fail the build with MSB4044.
    /// </summary>
    [Fact]
    public async Task SmartDefault_Override_EmptyPackageIdsViaGlobalProperty_SkipsGenerationGracefully()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);
        TestProject dependency = await repository.AddDependencyProjectAsync("Steeltoe.Management.Endpoint");

        TestProject testApp = await repository.AddProjectAsync(GitPropertiesTestWorkspace.TestAppProjectName, generateGitProperties: null,
            extraItemGroupContent: dependency.ToProjectReferenceXml());

        await testApp.BuildAsync("-p:GitPropertiesConsumingPackageIds=");
        testApp.GitPropertiesGenerated.Should().BeFalse();
    }
}
