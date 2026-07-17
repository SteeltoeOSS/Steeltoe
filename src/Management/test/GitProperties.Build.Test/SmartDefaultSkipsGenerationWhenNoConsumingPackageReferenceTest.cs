// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class SmartDefaultSkipsGenerationWhenNoConsumingPackageReferenceTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// The default case for the overwhelming majority of projects in a large solution (a class library, a test project, anything without a consuming package
    /// anywhere in its resolved dependency graph): generation is skipped entirely, without needing an explicit opt-out, and without breaking the build. A
    /// real git repository is deliberately present here (unlike NoGitWarnsByDefaultTest) to prove the smart default - not "no .git found" - is what causes
    /// the skip.
    /// </summary>
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);
        TestProject testApp = await repository.AddProjectAsync(GitPropertiesTestWorkspace.TestAppProjectName, generateGitProperties: null);

        string result = await testApp.BuildAsync("-v:detailed");
        // Not a numbered GITPROPS0xx code - this is plain internal trace output, not a diagnosable outcome (see the .targets file's own comment on it).
        result.Should().Contain("git.properties generation skipped: no reference to");
        testApp.GitPropertiesGenerated.Should().BeFalse();
    }
}
