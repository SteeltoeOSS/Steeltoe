// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class GitExecutableNotFoundWarnsByDefaultTest : GitPropertiesBuildTestBase
{
    private const string BogusGitExecutable = "this-executable-definitely-does-not-exist-anywhere";

    /// <summary>
    /// $(GitExecutable) itself failing to run - the most likely real-world reason git.properties would ever be skipped at all: git simply isn't installed,
    /// or isn't on PATH - is otherwise untested (see GenerateGitPropertiesCacheTask.CheckGitVersion's own remarks on why the "too old"/"unparseable version
    /// string" siblings of this same check can't reasonably be exercised this way). Unlike those two, this one needs no real-but-fake git binary: pointing
    /// $(GitExecutable) at a name that can never resolve on any platform's PATH reliably reproduces "could not run git at all" through a real build.
    /// </summary>
    [Fact]
    public async Task GitExecutableNotFound_WarnsByDefault()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        string defaultResult = await ProcessRunner.RunDotnetAsync(testApp, "build", $"-p:GitExecutable={BogusGitExecutable}");
        AssertWarned(defaultResult, "GITPROPS003");
        AssertNoGitPropertiesGenerated(testApp);

        string enableWarningsFalseResult = await ProcessRunner.RunDotnetAsync(testApp, "build", $"-p:GitExecutable={BogusGitExecutable}",
            "-p:GitPropertiesEnableWarnings=false", "-v:normal");

        AssertReportedAsInfoOnly(enableWarningsFalseResult, "GITPROPS003", "could not run");

        string featureOffResult = await ProcessRunner.RunDotnetAsync(testApp, "build", $"-p:GitExecutable={BogusGitExecutable}",
            "-p:GenerateGitProperties=false");

        featureOffResult.Should().NotContain("GITPROPS003");
    }
}
