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
    public async Task Test()
    {
        GitRepository source = await Workspace.CreateGitRepositoryAsync("source", 1);
        GitRepository shallow = await source.CloneAsShallowAsync("shallow");
        string result = await shallow.TestApp.BuildAsync("-p:GitPropertiesEnableWarnings=false", "-v:normal");
        result.AssertReportedAsInfoOnly("GITPROPS006", "repository is a shallow clone");
    }
}
