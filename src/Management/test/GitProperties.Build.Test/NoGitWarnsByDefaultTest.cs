// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class NoGitWarnsByDefaultTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Test()
    {
        TestProject testApp = await Workspace.CreateProjectDirectoryAsync("test-project");
        string result = await testApp.BuildAsync();
        result.AssertWarned("GITPROPS001");
        testApp.GitPropertiesGenerated.Should().BeFalse();
    }
}
