// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class NoGitInfoWhenEnableWarningsFalseTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task NoGit_InfoWhenEnableWarningsFalse()
    {
        string projectDirectory = Path.Combine(Workspace.RootDirectory, "proj");
        Directory.CreateDirectory(projectDirectory);
        string testApp = await Workspace.CopyCurrentProjectFilesAsync(projectDirectory);

        string result = await ProcessRunner.RunDotnetAsync(testApp, "build", "-p:GitPropertiesEnableWarnings=false", "-v:normal");
        AssertReportedAsInfoOnly(result, "GITPROPS001", "no usable .git directory found above");
        AssertNoGitPropertiesGenerated(testApp);
    }
}
