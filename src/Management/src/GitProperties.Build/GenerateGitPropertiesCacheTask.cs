// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Steeltoe.Management.GitProperties.Build;

/// <summary>
/// Computes the 17 git.properties fields that are stable across the whole repository (everything except the live working-tree state, the per-project
/// build version, and the current build's own timestamp - see <see cref="ComposeGitPropertiesTask" />) and writes them to a shared cache file. Meant to
/// run at most once per solution build: callers key their own Inputs/Outputs incrementality off this task's <see cref="CacheFile" /> output so 49 of 50
/// projects in a solution reuse it instead of re-invoking git.
/// </summary>
/// <remarks>
/// Forgivable, EnableWarnings-gated situations each get their own diagnostic code so they can be suppressed individually (e.g. via
/// $(MSBuildWarningsAsErrors)/$(NoWarn)) or all at once via EnableWarnings=false: GITPROPS001 (no usable git working tree - the other half of this code,
/// "no .git directory at all", is raised from Steeltoe.Management.GitProperties.Build.targets before this task ever runs), GITPROPS003 (git executable
/// not found/runnable), GITPROPS004 (installed git predates <see cref="MinimumGitVersion" />), GITPROPS005 (repository has zero commits), GITPROPS006 (a
/// shallow clone - unlike the other four, generation still succeeds, just with two fields left empty). GITPROPS002 (".git" is a file, i.e. a
/// worktree/submodule) is also raised from the .targets file, since it's detected before this task runs.
/// </remarks>
// ReSharper disable once UnusedType.Global
public sealed class GenerateGitPropertiesCacheTask : Task
{
    /// <summary>
    /// The oldest git version this task is known to work against. Set by "rev-parse --is-shallow-repository" below (see
    /// <see cref="TryGenerateAndWriteCache" />) - the newest feature this task relies on, added in git 2.15.0 (released 2017-10-30). Every other git command
    /// used here (e.g. "tag --points-at", the "%cI" strict-ISO-8601 pretty-format placeholder) requires an older version than that, so this one flag alone
    /// determines the actual floor.
    /// </summary>
    private static readonly Version MinimumGitVersion = new(2, 15, 0);

    /// <summary>
    /// Matches "git version 2.42.0", "git version 2.42.0.windows.1", and "git version 2.39.5 (Apple Git-154)" alike - capturing only the leading
    /// major.minor[.patch] numbers every real git build's "--version" output starts with, regardless of whatever vendor-specific suffix follows.
    /// </summary>
    private static readonly Regex GitVersionRegex = new(@"^git version (\d+)\.(\d+)(?:\.(\d+))?", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    /// <summary>
    /// The field separator ("%x1f", ASCII Unit Separator) used in <see cref="GetLatestCommitLogEntry" />'s own "--pretty=format:" string - a single-element
    /// array only because <see cref="string.Split(char[], int)" /> requires one, not because there's more than one separator.
    /// </summary>
    private static readonly char[] CommitLogFieldSeparator = [(char)0x1F];

    /// <summary>
    /// Line separators for splitting raw git command output in <see cref="ReadTagsAndTotalCommitCount" /> - both are needed since git's own output uses "\n"
    /// (see <see cref="GitProcessRunner.Run" />'s remarks), but a "git config" value or similar could still legitimately contain a literal "\r".
    /// </summary>
    private static readonly char[] LineSeparators =
    [
        '\r',
        '\n'
    ];

    /// <summary>
    /// CI-provided environment variables consulted by <see cref="ResolveBranch" /> when HEAD is detached - most CI systems check out a specific commit
    /// rather than a branch, so "git rev-parse --abbrev-ref HEAD" alone reports "HEAD", not the branch a human would recognize.
    /// </summary>
    private static readonly string[] BranchEnvironmentVariableNames =
    [
        "GITHUB_HEAD_REF",
        "GITHUB_REF_NAME",
        "BUILD_SOURCEBRANCHNAME",
        "CI_COMMIT_REF_NAME",
        "GIT_BRANCH",
        "CIRCLE_BRANCH",
        "TRAVIS_BRANCH"
    ];

    /// <summary>
    /// Gets or sets the resolved git repository root directory.
    /// </summary>
    [Required]
    public string RepositoryRoot { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the git executable to invoke.
    /// </summary>
    [Required]
    public string GitExecutable { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the shared cache file to write.
    /// </summary>
    [Required]
    public string CacheFile { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the length of the abbreviated commit ID to generate.
    /// </summary>
    [Required]
    public string CommitIdAbbrevLength { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether a forgivable anomaly is reported as a warning (true) or an informational message (false).
    /// </summary>
    [Required]
    public bool EnableWarnings { get; set; }

    /// <inheritdoc />
    public override bool Execute()
    {
        GitVersionStatus versionStatus = CheckGitVersion();

        if (versionStatus == GitVersionStatus.Unknown)
        {
            return false;
        }

        if (versionStatus == GitVersionStatus.Incompatible)
        {
            return true;
        }

        string? commitId = Preflight();

        if (commitId == null)
        {
            return true;
        }

        return TryGenerateAndWriteCache(commitId);
    }

    /// <summary>
    /// Checks the installed git against <see cref="MinimumGitVersion" /> - deliberately the very first git invocation this task makes, before relying on any
    /// command a genuinely old git might reject outright (e.g. "rev-parse --is-shallow-repository" in <see cref="TryGenerateAndWriteCache" /> would fail
    /// with "unknown option" on one).
    /// </summary>
    /// <remarks>
    /// The "too old"/"unparseable" paths are both untested against a real git binary, for the same reason the git-not-runnable-at-all case always has been:
    /// reliably faking a fully working-but-old (or malformed-version) git installation across Windows/Linux/macOS in this suite's plain "spawn a real dotnet
    /// build" test style isn't practical.
    /// </remarks>
    private GitVersionStatus CheckGitVersion()
    {
        string? output = GetGitVersion();

        if (output == null)
        {
            return GitVersionStatus.Incompatible;
        }

        Version? installedVersion = ParseGitVersion(output);

        if (installedVersion == null)
        {
            Log.LogError($"git.properties: could not parse the installed git version from '{GitExecutable} --version' output: '{output}'.");
            return GitVersionStatus.Unknown;
        }

        if (installedVersion < MinimumGitVersion)
        {
            ReportDiagnostic("GITPROPS004",
                $"git.properties generation skipped: installed git version {installedVersion} is older than the minimum supported version " +
                $"({MinimumGitVersion}). Upgrade git to resolve this.");

            return GitVersionStatus.Incompatible;
        }

        return GitVersionStatus.Compatible;
    }

    /// <summary>
    /// Runs "git --version", reporting GITPROPS003 (forgivable) if git can't be invoked at all - either the process fails to start, or it starts and exits
    /// with a non-zero code. Returns the raw output on success, or null in either failure case.
    /// </summary>
    private string? GetGitVersion()
    {
        string output;
        int exitCode;

        try
        {
            exitCode = RunGit("--version", out output, out _);
        }
        catch (Exception exception)
        {
            ReportDiagnostic("GITPROPS003", $"git.properties generation skipped: could not run '{GitExecutable}' ({exception.Message}).");
            return null;
        }

        if (exitCode != 0)
        {
            ReportDiagnostic("GITPROPS003", $"git.properties generation skipped: '{GitExecutable} --version' exited with code {exitCode}.");
            return null;
        }

        return output;
    }

    /// <summary>
    /// Matches "git version 2.42.0", "git version 2.42.0.windows.1", and "git version 2.39.5 (Apple Git-154)" alike - capturing only the leading
    /// major.minor[.patch] numbers every real git build's "--version" output starts with, regardless of whatever vendor-specific suffix follows. Pure string
    /// parsing, no I/O and no MSBuild logging - deliberately kept separate from <see cref="GetGitVersion" /> so this part alone could be unit-tested without
    /// a real git process, if this project ever adds that kind of test.
    /// </summary>
    private static Version? ParseGitVersion(string output)
    {
        Match match = GitVersionRegex.Match(output);

        if (!match.Success)
        {
            return null;
        }

        int major = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        int minor = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        int build = match.Groups[3].Success ? int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture) : 0;

        return new Version(major, minor, build);
    }

    /// <summary>
    /// Forgivable checks: the repository must be a usable work tree, and it must have at least one commit - git's own runnability and version were already
    /// checked in <see cref="CheckGitVersion" />, before this ever runs. Returns null when either check fails - the anomaly is already reported via
    /// <see cref="ReportDiagnostic" /> (not as a build error), and generation simply skips, per the class remarks. Returns the resolved commit ID otherwise.
    /// </summary>
    private string? Preflight()
    {
        int exitCode = RunGit("rev-parse --is-inside-work-tree", out string stdout, out _);

        if (exitCode != 0 || stdout != "true")
        {
            ReportDiagnostic("GITPROPS001", $"git.properties generation skipped: '{RepositoryRoot}' is not inside a usable git working tree.");
            return null;
        }

        exitCode = RunGit("rev-parse HEAD", out stdout, out _);

        if (exitCode != 0)
        {
            ReportDiagnostic("GITPROPS005", "git.properties generation skipped: repository has no commits yet.");
            return null;
        }

        return stdout;
    }

    /// <summary>
    /// From here on, any failure is unexpected and fatal - git and the repository are already known-good (see <see cref="Preflight" />).
    /// </summary>
    /// <remarks>
    /// Wrapped in a cross-process lock (see <see cref="GitPropertiesFileWriter.TryAcquireExclusiveLock" />): MSBuild's own Inputs/Outputs staleness check
    /// (in Steeltoe.Management.GitProperties.Build.targets) runs independently per project/TFM, with no coordination between them - so when multiple
    /// projects or TFMs of the same multi-targeted project build concurrently (MSBuild's default), more than one can see the shared cache as stale and
    /// decide to invoke this task at the same time, before either has written a fresh one. Deliberately does NOT try to validate whether the existing cache
    /// content is itself still correct/up to date - that's the target-level Inputs/Outputs check's job, and it has already decided we need to run. The only
    /// thing checked here is whether the file was rewritten by someone else WHILE this call was waiting for the lock: only then is skipping safe, because a
    /// concurrent write during that narrow window must be reacting to the exact same staleness trigger, in the same build, against the same repository
    /// state. (An early, wrong attempt at this compared the cache's stored commit ID to the current one - but tagging an existing commit, for example,
    /// invalidates the cache without changing the commit ID, so that check silently skipped writes that were actually needed.) This is purely a "thundering
    /// herd" optimization, not a correctness fix - <see cref="GitPropertiesFileWriter.WriteAtomic" /> already guarantees no reader ever observes a
    /// torn/partial file even without it, so failing to acquire the lock (or an environment where locking isn't possible at all) safely falls back to doing
    /// the work anyway, same as before this existed.
    /// </remarks>
    private bool TryGenerateAndWriteCache(string commitId)
    {
        DateTime? cacheWriteTimeBeforeLock = File.Exists(CacheFile) ? File.GetLastWriteTimeUtc(CacheFile) : null;

        // The ".lock" file itself is deliberately never deleted, only closed (releasing the OS-level lock, not the
        // file) - it lives next to $(GitPropertiesCacheFile) under obj\_GitProperties\, an already-gitignored,
        // `dotnet clean`-swept intermediate directory, so leaving it there costs nothing. Deleting it after use
        // would actually be less safe: a concurrent builder could open (or create) the very same path a moment
        // later, and "delete, then someone else recreates the same path" is the classic TOCTOU race that breaks a
        // file-based mutex's mutual exclusion guarantee - simplest to just never delete it and let every build reuse
        // the same, already-existing lock file.
        using FileStream? cacheLock = GitPropertiesFileWriter.TryAcquireExclusiveLock($"{CacheFile}.lock", TimeSpan.FromSeconds(30));

        if (cacheLock != null && WasCacheRewrittenWhileWaitingForLock(cacheWriteTimeBeforeLock))
        {
            Log.LogMessage(
                "git.properties: shared cache at '{0}' was rewritten by another concurrently-building project or target framework while waiting for " +
                "the lock - skipping.", CacheFile);

            return true;
        }

        Log.LogMessage("git.properties: generating shared cache at '{0}'.", CacheFile);

        int exitCode = RunGit("rev-parse --is-shallow-repository", out string stdout, out string stderr);

        if (exitCode != 0)
        {
            Log.LogError("git.properties: failed to determine shallow-clone status: {0}", stderr);
            return false;
        }

        bool isShallow = stdout == "true";

        if (isShallow)
        {
            ReportDiagnostic("GITPROPS006",
                "git.properties: repository is a shallow clone - git.total.commit.count and git.closest.tag.commit.count will be left empty. Run " +
                "'git fetch --unshallow' to fetch full history, or configure your CI checkout for full depth (e.g. GitHub Actions: fetch-depth: 0).");
        }

        CommitLogEntry? logEntry = GetLatestCommitLogEntry();

        if (logEntry == null)
        {
            return false;
        }

        TagDescription tagDescription = DescribeClosestTag(isShallow);
        TagsAndCommitCount? tagsAndCommitCount = ReadTagsAndTotalCommitCount(isShallow);

        if (tagsAndCommitCount == null)
        {
            return false;
        }

        GitConfig? config = ReadConfig();

        if (config == null)
        {
            return false;
        }

        string branch = ResolveBranch();
        string buildHost = Environment.MachineName;

        List<string> lines =
        [
            $"git.branch={GitPropertiesFileWriter.EscapeLineBreaks(branch)}",
            $"git.commit.id={GitPropertiesFileWriter.EscapeLineBreaks(commitId)}",
            $"git.commit.id.abbrev={GitPropertiesFileWriter.EscapeLineBreaks(logEntry.AbbrevId)}",
            $"{GitPropertiesFileWriter.CommitIdDescribeKey}={GitPropertiesFileWriter.EscapeLineBreaks(tagDescription.BaseDescribe)}",
            $"git.commit.time={GitPropertiesFileWriter.EscapeLineBreaks(logEntry.CommitTime)}",
            $"git.commit.message.short={GitPropertiesFileWriter.EscapeLineBreaks(logEntry.ShortMessage)}",
            $"git.commit.message.full={GitPropertiesFileWriter.EscapeLineBreaks(logEntry.FullMessage)}",
            $"git.commit.user.name={GitPropertiesFileWriter.EscapeLineBreaks(logEntry.AuthorName)}",
            $"git.commit.user.email={GitPropertiesFileWriter.EscapeLineBreaks(logEntry.AuthorEmail)}",
            $"git.build.host={GitPropertiesFileWriter.EscapeLineBreaks(buildHost)}",
            $"git.build.user.name={GitPropertiesFileWriter.EscapeLineBreaks(config.UserName)}",
            $"git.build.user.email={GitPropertiesFileWriter.EscapeLineBreaks(config.UserEmail)}",
            $"git.tags={GitPropertiesFileWriter.EscapeLineBreaks(tagsAndCommitCount.Tags)}",
            $"git.closest.tag.name={GitPropertiesFileWriter.EscapeLineBreaks(tagDescription.ClosestTagName)}",
            $"git.closest.tag.commit.count={GitPropertiesFileWriter.EscapeLineBreaks(tagDescription.ClosestTagCommitCount)}",
            $"git.remote.origin.url={GitPropertiesFileWriter.EscapeLineBreaks(config.RemoteUrl)}",
            $"git.total.commit.count={GitPropertiesFileWriter.EscapeLineBreaks(tagsAndCommitCount.TotalCommitCount)}"
        ];

        try
        {
            GitPropertiesFileWriter.WriteAtomic(CacheFile, lines);
        }
        catch (IOException exception)
        {
            Log.LogError($"git.properties: failed to write {CacheFile}: {exception.Message}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// See the "Deliberately does NOT try to validate..." remarks on <see cref="TryGenerateAndWriteCache" /> for why this checks only for a rewrite during
    /// the lock wait, not the cache's own correctness.
    /// </summary>
    private bool WasCacheRewrittenWhileWaitingForLock(DateTime? writeTimeBeforeLock)
    {
        if (!File.Exists(CacheFile))
        {
            return false;
        }

        return writeTimeBeforeLock == null || File.GetLastWriteTimeUtc(CacheFile) > writeTimeBeforeLock.Value;
    }

    private CommitLogEntry? GetLatestCommitLogEntry()
    {
        int exitCode = RunGit($"log -1 --abbrev={CommitIdAbbrevLength} --pretty=format:%h%x1f%an%x1f%ae%x1f%cI%x1f%s%x1f%B", out string stdout,
            out string stderr);

        if (exitCode != 0)
        {
            Log.LogError("git.properties: failed to read commit metadata: {0}", stderr);
            return null;
        }

        string[] logFields = stdout.Split(CommitLogFieldSeparator, 6);

        return new CommitLogEntry(logFields.Length > 0 ? logFields[0] : string.Empty, logFields.Length > 1 ? logFields[1] : string.Empty,
            logFields.Length > 2 ? logFields[2] : string.Empty, logFields.Length > 3 ? logFields[3] : string.Empty,
            logFields.Length > 4 ? logFields[4] : string.Empty, logFields.Length > 5 ? logFields[5] : string.Empty);
    }

    /// <summary>
    /// Parses the single "describe --tags --long --always" call for its three possible shapes: exactly-on-tag ("tag-0-gsha"), N-commits-ahead
    /// ("tag-N-gsha"), and no-tags-at-all (a bare "--always" fallback SHA, with no dashes). Failure here is not fatal - it degrades to empty/fallback
    /// values, same as "no tags exist".
    /// </summary>
    private TagDescription DescribeClosestTag(bool isShallow)
    {
        int exitCode = RunGit("describe --tags --long --always", out string stdout, out _);
        string baseDescribe = string.Empty;
        string closestTagName = string.Empty;
        string closestTagCommitCount = string.Empty;

        if (exitCode == 0 && !string.IsNullOrEmpty(stdout))
        {
            int lastDashIndex = stdout.LastIndexOf('-');
            int secondLastDash = lastDashIndex >= 0 ? stdout.LastIndexOf('-', lastDashIndex - 1) : -1;
            bool hasTagPrefix = lastDashIndex >= 0 && secondLastDash >= 0 && stdout.Substring(lastDashIndex + 1).StartsWith("g", StringComparison.Ordinal);

            if (!hasTagPrefix)
            {
                // No tags reachable at all - "--always" fallback is a bare abbreviated SHA.
                baseDescribe = stdout;
            }
            else
            {
                closestTagName = stdout.Substring(0, secondLastDash);
                closestTagCommitCount = stdout.Substring(secondLastDash + 1, lastDashIndex - secondLastDash - 1);
                baseDescribe = closestTagCommitCount == "0" ? closestTagName : $"{closestTagName}-{closestTagCommitCount}";
            }
        }

        if (isShallow)
        {
            // Ancestry walk is truncated on a shallow clone - a "count" here would be silently wrong.
            closestTagCommitCount = string.Empty;
        }

        return new TagDescription(baseDescribe, closestTagName, closestTagCommitCount);
    }

    private TagsAndCommitCount? ReadTagsAndTotalCommitCount(bool isShallow)
    {
        int exitCode = RunGit("tag --points-at HEAD", out string stdout, out string stderr);

        if (exitCode != 0)
        {
            Log.LogError("git.properties: failed to list tags: {0}", stderr);
            return null;
        }

        string tags = string.Join(",", stdout.Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries));

        string totalCommitCount = string.Empty;

        if (!isShallow)
        {
            exitCode = RunGit("rev-list --count HEAD", out stdout, out stderr);

            if (exitCode != 0)
            {
                Log.LogError("git.properties: failed to count commits: {0}", stderr);
                return null;
            }

            totalCommitCount = stdout;
        }

        return new TagsAndCommitCount(tags, totalCommitCount);
    }

    private GitConfig? ReadConfig()
    {
        int exitCode = RunGit("config --list", out string stdout, out string stderr);

        if (exitCode != 0)
        {
            Log.LogError("git.properties: failed to read git config: {0}", stderr);
            return null;
        }

        string userName = string.Empty;
        string userEmail = string.Empty;
        string remoteUrl = string.Empty;

        foreach (string line in stdout.Split('\n'))
        {
            int equalsIndex = line.IndexOf('=');

            if (equalsIndex < 0)
            {
                continue;
            }

            string key = line.Substring(0, equalsIndex).Trim();
            string value = line.Substring(equalsIndex + 1).Trim();

            if (string.Equals(key, "user.name", StringComparison.OrdinalIgnoreCase))
            {
                userName = value;
            }
            else if (string.Equals(key, "user.email", StringComparison.OrdinalIgnoreCase))
            {
                userEmail = value;
            }
            else if (string.Equals(key, "remote.origin.url", StringComparison.OrdinalIgnoreCase))
            {
                remoteUrl = value;
            }
        }

        return new GitConfig(userName, userEmail, StripUserInfo(remoteUrl));
    }

    /// <summary>
    /// Falls back to common CI environment variables when HEAD is detached (e.g. most CI checkouts).
    /// </summary>
    private string ResolveBranch()
    {
        string branch = string.Empty;
        int exitCode = RunGit("rev-parse --abbrev-ref HEAD", out string stdout, out _);

        if (exitCode == 0)
        {
            branch = stdout;
        }

        if (string.IsNullOrEmpty(branch) || branch == "HEAD")
        {
            foreach (string name in BranchEnvironmentVariableNames)
            {
                string? value = Environment.GetEnvironmentVariable(name);

                if (!string.IsNullOrEmpty(value))
                {
                    branch = value;
                    break;
                }
            }
        }

        return branch;
    }

    private int RunGit(string arguments, out string stdout, out string stderr)
    {
        return GitProcessRunner.Run(GitExecutable, RepositoryRoot, arguments, out stdout, out stderr);
    }

    /// <summary>
    /// Reports a forgivable anomaly - either a full skip (GITPROPS001/003/004/005, where the caller has already decided to return false) or a
    /// degraded-but-successful outcome (GITPROPS006, where generation still proceeds) - as a warning when <see cref="EnableWarnings" /> is true, or a plain
    /// informational message otherwise.
    /// </summary>
    /// <remarks>
    /// The downgraded message carries no code at all: a code only has a purpose when something is suppressible (via $(NoWarn)/ $(MSBuildWarningsAsErrors)),
    /// which only applies to warnings - attaching one to a plain message just to look consistent added no real value, and an earlier version of this method
    /// that manually embedded "{code}: " in the message text on top of also passing it as the structured Log.LogMessage code parameter actually
    /// double-printed it (the console logger already renders a message's code automatically when one is supplied). Left at LogMessage's own default
    /// importance (Normal), not High: consumers who set EnableWarnings=false have said this is routine and don't want it in their default build output, but
    /// it's still one "-v:normal" away if they want to check.
    /// </remarks>
    private void ReportDiagnostic(string code, string message)
    {
        if (EnableWarnings)
        {
            Log.LogWarning(null, code, null, null, 0, 0, 0, 0, message);
        }
        else
        {
            Log.LogMessage(message);
        }
    }

    private static string StripUserInfo(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return url;
        }

        try
        {
            var uri = new Uri(url);

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                var builder = new UriBuilder(uri)
                {
                    UserName = string.Empty,
                    Password = string.Empty
                };

                return builder.Uri.ToString();
            }
        }
        catch (UriFormatException)
        {
            // Not a parseable absolute URL (e.g. SCP-like "git@host:org/repo.git") - nothing to strip.
        }

        return url;
    }

    /// <summary>
    /// Whether the installed git is new enough to use, as determined by <see cref="CheckGitVersion" />.
    /// </summary>
    private enum GitVersionStatus
    {
        /// <summary>
        /// Git ran and its version satisfies <see cref="MinimumGitVersion" /> - safe to proceed.
        /// </summary>
        Compatible,

        /// <summary>
        /// Git either couldn't be run at all (GITPROPS003) or is older than <see cref="MinimumGitVersion" /> (GITPROPS004) - both forgivable, already reported
        /// via <see cref="ReportDiagnostic" />, generation simply skips.
        /// </summary>
        Incompatible,

        /// <summary>
        /// Git ran, but its "--version" output couldn't be parsed at all - already reported as a hard error via Log.LogError, since unlike "genuinely too old",
        /// there's no safe default to fall back to here.
        /// </summary>
        Unknown
    }

    /// <summary>
    /// The fields read from a single "log -1 --pretty=format:..." call - see <see cref="GenerateGitPropertiesCacheTask.GetLatestCommitLogEntry" />.
    /// </summary>
    /// <remarks>
    /// A plain class with a primary constructor, not a record: records need "init" accessors under the hood, which require
    /// System.Runtime.CompilerServices.IsExternalInit - not present on netstandard2.0 (this project's TargetFramework - see its own csproj remarks for why)
    /// and not worth polyfilling just for four small, write-once data holders.
    /// </remarks>
    private sealed class CommitLogEntry(string abbrevId, string authorName, string authorEmail, string commitTime, string shortMessage, string fullMessage)
    {
        public string AbbrevId { get; } = abbrevId;
        public string AuthorName { get; } = authorName;
        public string AuthorEmail { get; } = authorEmail;
        public string CommitTime { get; } = commitTime;
        public string ShortMessage { get; } = shortMessage;
        public string FullMessage { get; } = fullMessage;
    }

    /// <summary>
    /// The fields derived from a single "describe --tags --long --always" call - see <see cref="DescribeClosestTag" />.
    /// </summary>
    private sealed class TagDescription(string baseDescribe, string closestTagName, string closestTagCommitCount)
    {
        public string BaseDescribe { get; } = baseDescribe;
        public string ClosestTagName { get; } = closestTagName;
        public string ClosestTagCommitCount { get; } = closestTagCommitCount;
    }

    /// <summary>
    /// The fields read from "tag --points-at HEAD" and (unless shallow) "rev-list --count HEAD" - see <see cref="ReadTagsAndTotalCommitCount" />.
    /// </summary>
    private sealed class TagsAndCommitCount(string tags, string totalCommitCount)
    {
        public string Tags { get; } = tags;
        public string TotalCommitCount { get; } = totalCommitCount;
    }

    /// <summary>
    /// The fields read from a single "config --list" call - see <see cref="ReadConfig" />.
    /// </summary>
    private sealed class GitConfig(string userName, string userEmail, string remoteUrl)
    {
        public string UserName { get; } = userName;
        public string UserEmail { get; } = userEmail;
        public string RemoteUrl { get; } = remoteUrl;
    }
}
