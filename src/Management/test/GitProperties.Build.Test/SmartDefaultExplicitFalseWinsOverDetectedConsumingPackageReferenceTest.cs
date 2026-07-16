// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class SmartDefaultExplicitFalseWinsOverDetectedConsumingPackageReferenceTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// A consumer's explicit choice must never be second-guessed by the smart default, in either direction - the negative direction (no reference, but
    /// explicitly forced on) is already exercised by every other test in this file, which all set $(GenerateGitProperties)=true explicitly via
    /// WriteAppProject's default. This covers the other direction: a consuming-package reference IS present (the smart default would say "generate"), but
    /// the consumer explicitly opted out anyway.
    /// </summary>
    [Fact]
    public async Task SmartDefault_ExplicitFalse_WinsOverDetectedConsumingPackageReference()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1);
        const string consumingPackageStandInName = "Steeltoe.Management.Endpoint";
        await GitPropertiesTestWorkspace.WriteDummyDependencyProjectAsync(repository, consumingPackageStandInName);

        string testApp = await GitPropertiesTestWorkspace.WriteAppProjectAsync(repository, GitPropertiesTestWorkspace.TestAppProjectName,
            generateGitProperties: null,
            extraItemGroupContent: $"""<ProjectReference Include="..\{consumingPackageStandInName}\{consumingPackageStandInName}.csproj" />""");

        ProcessResult result = await ProcessRunner.RunDotnetAsync(testApp, "build", "-p:GenerateGitProperties=false");
        AssertBuildSucceeded(result, "build with GenerateGitProperties explicitly set to false despite a consuming-package reference being present");
        AssertNoGitPropertiesGenerated(testApp);
    }
}
