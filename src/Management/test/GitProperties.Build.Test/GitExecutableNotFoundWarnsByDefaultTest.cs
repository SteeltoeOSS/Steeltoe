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
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);
        string defaultResult = await repository.TestApp.BuildAsync($"-p:GitExecutable={BogusGitExecutable}");
        defaultResult.AssertWarned("GITPROPS003");

        string enableWarningsFalseResult =
            await repository.TestApp.BuildAsync($"-p:GitExecutable={BogusGitExecutable}", "-p:GitPropertiesEnableWarnings=false", "-v:normal");

        enableWarningsFalseResult.AssertReportedAsInfoOnly("GITPROPS003", "could not run");

        string featureOffResult = await repository.TestApp.BuildAsync($"-p:GitExecutable={BogusGitExecutable}", "-p:GenerateGitProperties=false");
        featureOffResult.Should().NotContain("GITPROPS003");

        repository.TestApp.GitPropertiesGenerated.Should().BeFalse();
    }
}
