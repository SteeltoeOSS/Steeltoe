// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class WriteToProjectDirectoryCreatesFallbackFileOnPublishTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// "dotnet publish" runs its own compile/composition steps internally regardless of whether "dotnet build" ran first - this guards against the fallback
    /// file only being written along the "build" target chain and silently never firing when publish is the very first command run against a fresh checkout
    /// (a common real-world pattern: `dotnet publish` directly, without a separate build step). Runs with $(GitPropertiesEnableWarnings) at its default
    /// (enabled) setting to confirm nothing about the fallback-writing path implicitly depends on warnings being suppressed - since a real .git repository
    /// is available here, nothing should be skipped (and no GITPROPS0xx code should appear) regardless of that setting.
    /// </summary>
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1, true);
        string result = await repository.TestApp.PublishAsync("-p:GitPropertiesWriteToProjectDirectory=true", "-p:GitPropertiesEnableWarnings=true");
        result.Should().NotContain("GITPROPS0", "nothing should be skipped, and no fallback should be needed, when a real .git repository is available.");

        repository.TestApp.FallbackGitPropertiesGenerated.Should().BeTrue(
            "the fallback file should have been written next to the .csproj, even for a bare publish.");

        Dictionary<string, string> fallbackProperties = await repository.TestApp.ReadFallbackPropertiesAsync();
        Dictionary<string, string> publishedProperties = await repository.TestApp.ReadReleasePublishPropertiesAsync();
        fallbackProperties.Should().BeEquivalentTo(publishedProperties, "the fallback file must carry the exact same content as the published output.");

        bool isDirty = await repository.IsDirtyAsync();
        isDirty.Should().BeFalse("the fallback file is gitignored, so it must not show up as an untracked change.");
    }
}
