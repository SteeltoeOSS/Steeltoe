// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class NonAsciiCommitDataRendersCorrectlyTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task NonAscii_CommitDataRendersCorrectly()
    {
        string repository = Path.Combine(Workspace.RootDirectory, "repo");
        Directory.CreateDirectory(repository);
        await ProcessRunner.RunGitAsync(repository, "init", "--quiet", "--initial-branch=main", ".");
        // \u-escaped rather than literal, so this source file itself stays plain ASCII: renders as accented Latin-1
        // supplement letters plus the trailing three characters of "commit", spelled out in Japanese (CJK).
        const string nonAsciiUserName = "\u00DCn\u00EFc\u00F6d\u00E9 T\u00EBst";
        const string nonAsciiCommitMessage = "\u00DCn\u00EFc\u00F6d\u00E9 t\u00EBst commit \u65E5\u672C\u8A9E";

        await ProcessRunner.RunGitAsync(repository, "config", "user.name", nonAsciiUserName);
        await ProcessRunner.RunGitAsync(repository, "config", "user.email", "test@example.com");
        await File.WriteAllTextAsync(Path.Combine(repository, ".gitignore"), "bin/\r\nobj/\r\n", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(repository, "file.txt"), "content", TestContext.Current.CancellationToken);
        await ProcessRunner.RunGitAsync(repository, "add", "-A");
        await ProcessRunner.RunGitAsync(repository, "commit", "--quiet", "-m", nonAsciiCommitMessage);

        string testApp = await Workspace.CopyCurrentProjectFilesAsync(repository);

        ProcessResult result = await ProcessRunner.RunDotnetAsync(testApp, "build");
        AssertBuildSucceeded(result, "build");

        Dictionary<string, string> properties = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(testApp));
        properties["git.commit.user.name"].Should().Be(nonAsciiUserName);
        properties["git.commit.message.short"].Should().Be(nonAsciiCommitMessage);
    }
}
