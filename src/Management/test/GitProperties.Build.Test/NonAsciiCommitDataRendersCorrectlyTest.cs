// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class NonAsciiCommitDataRendersCorrectlyTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// Also folds in coverage for GitPropertiesFormat.EscapeLineBreaks, rather than paying for a dedicated build just to exercise it: a commit message with
    /// a body (subject + blank line + body, as illustrated in PackageReadme.md's own example output) contains real embedded newlines in git's raw "%B"
    /// output, so this same commit also proves those get collapsed to a literal "\n" in the file that reaches disk - otherwise a multi-line message could
    /// desynchronize the line-based "git.&lt;key&gt;=&lt;value&gt;" format by spanning more than one physical line.
    /// </summary>
    [Fact]
    public async Task Test()
    {
        EmptyGitRepository emptyRepository = await Workspace.CreateEmptyRepositoryAsync("repo");

        // \u-escaped rather than literal, so this source file itself stays plain ASCII: renders as accented Latin-1
        // supplement letters plus the trailing three characters of "commit", spelled out in Japanese (CJK).
        const string nonAsciiUserName = "\u00DCn\u00EFc\u00F6d\u00E9 T\u00EBst";
        const string nonAsciiCommitSubject = "\u00DCn\u00EFc\u00F6d\u00E9 t\u00EBst commit \u65E5\u672C\u8A9E";
        const string commitBody = "Adds a null check before calling Ping().";

        await emptyRepository.RunGitAsync("config", "user.name", nonAsciiUserName);
        await emptyRepository.RunGitAsync("config", "user.email", "test@example.com");
        await File.WriteAllTextAsync(Path.Combine(emptyRepository.RootDirectory, ".gitignore"), "bin/\r\nobj/\r\n", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(emptyRepository.RootDirectory, "file.txt"), "content", TestContext.Current.CancellationToken);
        await emptyRepository.CommitAllAsync(nonAsciiCommitSubject, commitBody);

        GitRepository repository = await emptyRepository.AddTestAppAsync();
        await repository.TestApp.BuildAsync();

        Dictionary<string, string> properties = await repository.TestApp.ReadDebugPropertiesAsync();
        properties["git.commit.user.name"].Should().Be(nonAsciiUserName);
        properties["git.commit.message.short"].Should().Be(nonAsciiCommitSubject, "the short/subject line is never multi-line to begin with.");

        properties["git.commit.message.full"].Should().Be($@"{nonAsciiCommitSubject}\n\n{commitBody}",
            "a real embedded newline must be escaped to a literal backslash-n, matching PackageReadme.md's own example output.");
    }
}
