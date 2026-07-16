// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class SmartDefaultOverrideDoesNotMatchPackageIdAsPrefixTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// Guards against a regression to a naive substring match (e.g. "IndexOf(id + "/")" without also requiring the match to be a whole library key) - a
    /// project referencing only "Some2" (never "Some" itself) must NOT be detected when $(GitPropertiesConsumingPackageIds) is configured as "Some", even
    /// though "Some2" starts with "Some". Proves DetectConsumingPackageReferenceTask compares whole package IDs, not prefixes.
    /// </summary>
    [Fact]
    public async Task SmartDefault_Override_DoesNotMatchPackageIdAsPrefix()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1);
        const string longerPackageId = "Some2";
        await GitPropertiesTestWorkspace.WriteDummyDependencyProjectAsync(repository, longerPackageId);

        string testApp = await GitPropertiesTestWorkspace.WriteAppProjectAsync(repository, GitPropertiesTestWorkspace.TestAppProjectName,
            generateGitProperties: null, extraItemGroupContent: $"""<ProjectReference Include="..\{longerPackageId}\{longerPackageId}.csproj" />""");

        ProcessResult result = await ProcessRunner.RunDotnetAsync(testApp, "build", "-p:GitPropertiesConsumingPackageIds=Some");
        AssertBuildSucceeded(result, "build with a referenced package ('Some2') that is a superstring, not a match, of the configured ID ('Some')");
        AssertNoGitPropertiesGenerated(testApp);
    }
}
