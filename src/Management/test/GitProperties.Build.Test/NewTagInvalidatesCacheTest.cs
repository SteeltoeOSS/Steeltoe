// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class NewTagInvalidatesCacheTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// Also folds in coverage for the trickiest shape ParseDescribeOutput's dash-splitting has to get right - a tag name that itself contains a dash
    /// ("release-1.0"), combined with a nonzero commits-ahead count - rather than spinning up a dedicated test just for that. Tagging an ANCESTOR of HEAD,
    /// not HEAD itself, serves both purposes at once: it produces that nonzero count, and it keeps HEAD's own commit ID unchanged, which is the actual point
    /// of this test's name - proving a new tag ref alone still invalidates the shared cache (see the regression this guards against in
    /// GenerateGitPropertiesCacheTask.TryGenerateAndWriteCache's own remarks).
    /// </summary>
    [Fact]
    public async Task NewTag_InvalidatesCache()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        await ProcessRunner.RunDotnetAsync(testApp, "build");
        Dictionary<string, string> propertiesBefore = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(testApp));
        propertiesBefore["git.tags"].Should().BeEmpty();

        string ancestorCommitId = await ProcessRunner.GetGitOutputAsync(repository, "rev-parse", "HEAD~1");
        await ProcessRunner.RunGitAsync(repository, "tag", "release-1.0", ancestorCommitId);

        await ProcessRunner.RunDotnetAsync(testApp, "build");
        Dictionary<string, string> propertiesAfter = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(testApp));

        propertiesAfter["git.tags"].Should().BeEmpty("the tag points at an ancestor, not HEAD, so it must not show up in git.tags.");
        propertiesAfter["git.closest.tag.name"].Should().Be("release-1.0");
        propertiesAfter["git.closest.tag.commit.count"].Should().Be("1", "HEAD is exactly one commit ahead of the tagged ancestor.");

        // "release-1.0-1", not the raw "git describe" output ("release-1.0-1-g<sha>"):
        // git.commit.id.describe deliberately omits the abbreviated SHA - see
        // GenerateGitPropertiesCacheTask.ParseDescribeOutput's own BaseDescribe reconstruction.
        propertiesAfter["git.commit.id.describe"].Should().Be("release-1.0-1");
    }
}
