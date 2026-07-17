// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class NoCommitsWarnsByDefaultTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// A freshly-initialized repository (real ".git", so GITPROPS001/002 don't fire instead) with zero commits yet - "git rev-parse HEAD" itself fails in
    /// this state, which GenerateGitPropertiesCacheTask.Preflight treats as a routine, forgivable precondition rather than an unexpected failure.
    /// </summary>
    [Fact]
    public async Task NoCommits_WarnsByDefault()
    {
        string repository = Path.Combine(Workspace.RootDirectory, "repo");
        Directory.CreateDirectory(repository);
        await ProcessRunner.RunGitAsync(repository, "init", "--quiet", "--initial-branch=main", ".");
        string testApp = await Workspace.CopyCurrentProjectFilesAsync(repository);

        string defaultResult = await ProcessRunner.RunDotnetAsync(testApp, "build");
        AssertWarned(defaultResult, "GITPROPS005");
        AssertNoGitPropertiesGenerated(testApp);

        string enableWarningsFalseResult = await ProcessRunner.RunDotnetAsync(testApp, "build", "-p:GitPropertiesEnableWarnings=false", "-v:normal");
        AssertReportedAsInfoOnly(enableWarningsFalseResult, "GITPROPS005", "no commits yet");

        string featureOffResult = await ProcessRunner.RunDotnetAsync(testApp, "build", "-p:GenerateGitProperties=false");
        featureOffResult.Should().NotContain("GITPROPS005");
    }
}
