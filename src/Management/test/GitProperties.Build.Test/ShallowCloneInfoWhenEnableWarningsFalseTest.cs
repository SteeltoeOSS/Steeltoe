// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class ShallowCloneInfoWhenEnableWarningsFalseTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// GITPROPS006 (unlike GITPROPS001-005) never blocks generation - the shallow clone is still fully usable, just with two fields left empty (see
    /// <see cref="ShallowCloneLeavesCommitCountsEmptyTest" />). Confirms $(GitPropertiesEnableWarnings) downgrades it to an informational message the same
    /// way it does for the others.
    /// </summary>
    [Fact]
    public async Task ShallowClone_InfoWhenEnableWarningsFalse()
    {
        string source = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "source"), 1);

        string shallow = Path.Combine(Workspace.RootDirectory, "shallow");
        ProcessResult cloneResult = await ProcessRunner.RunGitAsync(Path.GetTempPath(), "clone", "--quiet", "--no-local", "--depth", "1", source, shallow);
        cloneResult.ExitCode.Should().Be(0, "shallow clone should succeed.");

        string testApp = await Workspace.CopyCurrentProjectFilesAsync(shallow);

        ProcessResult result = await ProcessRunner.RunDotnetAsync(testApp, "build", "-p:GitPropertiesEnableWarnings=false", "-v:normal");
        AssertBuildSucceeded(result, "build");
        AssertReportedAsInfoOnly(result, "GITPROPS006", "repository is a shallow clone");
    }
}
