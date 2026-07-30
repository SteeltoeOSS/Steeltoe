# Steeltoe.Management.GitProperties.Build

Generates a `git.properties` file at build time, compatible with the [Spring Boot Actuator `git.properties`](https://docs.spring.io/spring-boot/reference/actuator/endpoints.html#actuator.endpoints.info.git-commit-information) format. When used together with Steeltoe's `Info` actuator endpoint, the information in this file (commit ID, branch, tags, whether the repository was "dirty" at build time, etc.) is automatically exposed at runtime.

## Getting started

```console
dotnet add package Steeltoe.Management.GitProperties.Build
```

No other setup is required for a project that references `Steeltoe.Management.Endpoint` and lives inside a Git repository. The next time you build that project, a `git.properties` file is generated and copied into your build (and publish) output automatically. Steeltoe's `Info` actuator endpoint then picks it up automatically at runtime.

## Example output

A generated `git.properties` file looks like this:

```properties
git.branch=main
git.commit.id=1a2b3c4d5e6f7a8b9c0d1e2f3a4b5c6d7e8f9a0b
git.commit.id.abbrev=1a2b3c4
git.commit.id.describe=v1.4.0-3-g1a2b3c4
git.commit.time=2026-06-18T09:42:11+00:00
git.commit.message.short=Fix null reference in health check
git.commit.message.full=Fix null reference in health check\nAdds a null check before calling Ping().
git.commit.user.name=Jane Doe
git.commit.user.email=jane.doe@example.com
git.build.host=build-agent-03
git.build.user.name=Jane Doe
git.build.user.email=jane.doe@example.com
git.tags=
git.closest.tag.name=v1.4.0
git.closest.tag.commit.count=3
git.remote.origin.url=https://github.com/example-org/example-app.git
git.total.commit.count=482
git.dirty=false
git.build.version=1.4.0
git.build.time=2026-07-09T14:32:10-06:00
```

When Steeltoe's `Info` actuator endpoint is enabled, all of these values are automatically surfaced under the `git` key of that endpoint's response. You don't need to read this file yourself.

## Shared cache

Computing repository-wide information (commit ID, branch, tags, and so on) is relatively expensive, especially when repeated for every project and target framework in a large solution. This package computes it at most once per build instead, and stores the result in a shared cache file, `obj/git.properties.cache`, at the root of your Git repository. Every project that generates `git.properties` reuses that shared cache.

The cache refreshes automatically whenever something changes in your Git repository, for example after pull, checkout, commit/tag, or merge/rebase. If you ever need to force a refresh yourself, delete the `git.properties.cache` file and rebuild.

## Configuration

All settings are optional MSBuild properties, set in your project file (or a `Directory.Build.props` file):

| Property | Default | Description |
|---|---|---|
| `GenerateGitProperties` | `auto` | Generates only when the project has a direct or indirect reference to one of `GitPropertiesConsumingPackageIds`. Set explicitly to `true` or `false` to always generate or always skip. |
| `GitPropertiesWriteToProjectDirectory` | `false` | Also writes a durable copy of `git.properties` directly next to your project file, so a remote build with no Git repository available can still find it. |
| `GitPropertiesEnableWarnings` | `true` | Whether the situations listed under [Diagnostics](#diagnostics) are reported as MSBuild warnings. |
| `GitPropertiesReportFileWrites` | `true` | Whether to report when the [shared cache](#shared-cache), `git.properties`, and its fallback copy are (re)written. Set to `false` to silence these. |
| `GitPropertiesConsumingPackageIds` | `Steeltoe.Management.Endpoint` | Semicolon-separated package IDs that trigger the `auto` default above. |
| `GitExecutable` | `git` | The git executable to invoke. Override this if `git` isn't on the `PATH` in your build environment. |
| `GitCommitIdAbbrevLength` | `7` | Number of characters used for the abbreviated commit ID. |

## Diagnostics

This package may log one of the following codes:

| Code | Meaning |
|---|---|
| `GITPROPS001` | No `.git` file or directory was found. |
| `GITPROPS002` | The resolved `.git` directory is invalid. |
| `GITPROPS003` | The configured Git executable (see `GitExecutable`) could not be run. It may not be installed, or not on the `PATH`. |
| `GITPROPS004` | The installed Git version is older than 2.15.0, the minimum version this package requires. |
| `GITPROPS005` | A Git repository was found, but it has no commits yet. |
| `GITPROPS006` | The repository is a shallow clone, so `git.total.commit.count` and `git.closest.tag.commit.count` are left empty. |
| `GITPROPS007` | The repository's dirty state could not be determined, so `git.dirty` is omitted. |

## Deploying without access to your Git repository

By default, `git.properties` is generated using live information read directly from your local `.git` directory. It only ends up in your build or publish output directory. This works well when the system that builds or publishes your application also has access to that same `.git` directory.

Some deployment methods don't give the build step access to your `.git` directory at all. For example, pushing your application's source code straight to Cloud Foundry (`cf push`) does not include your `.git` directory, so no `git.properties` can be produced.

To work around this, run the following command locally before every push. Your `.git` directory must be available when you run it:

```shell
dotnet build -t:WriteGitPropertiesFallbackFile
```

This command writes an extra copy of `git.properties` directly next to your project file without running a full build.

> [!IMPORTANT]
> **You must add `git.properties` to your `.gitignore` file.** This file is a generated build artifact, not source code. It changes on every single build. If it isn't ignored, Git will consider your working directory to have uncommitted changes after every build, even when you haven't changed anything yourself.
>
> Add the following line to your `.gitignore` file:
>
> ```gitignore
> git.properties
> ```
>
> This isn't just a tidiness recommendation. If you skip it, the `git.dirty` value inside the generated `git.properties` file will start reporting `true` on every build from then on. That happens because Git genuinely does see an uncommitted change: the file that keeps getting regenerated. This defeats the purpose of `git.dirty`, which is meant to tell you whether *your own* changes were committed, not whether this generated file was rewritten.
>
> If you deploy by pushing your source code directly, rather than a pre-built or published output (for example with Cloud Foundry's `cf push`), be careful not to *also* exclude `git.properties` from whatever gets pushed or deployed. For Cloud Foundry, that means leaving it out of `.cfignore`. `git.properties` must stay out of Git through `.gitignore`, but it still needs to be present on disk and travel along with your source code.

## Good to know

- **Build-time only.** This package doesn't add any runtime dependency to your application. It never flows transitively to anything that references your project.
- **Cross-platform.** Works the same way on Windows, Linux, and macOS.
- **Skips cleanly for anticipated Git issues.** If a Git repository can't be found or read for one of the reasons listed in [Diagnostics](#diagnostics), generation is skipped with a message you can suppress (see `GitPropertiesEnableWarnings`), instead of failing your build. This makes it safe to add this package to projects that aren't always built inside a Git checkout, such as a Docker image build stage.
- **Git worktrees and submodules are supported.** A build running from a worktree writes `git.properties` for *that worktree's* own checked-out branch and commit, with its own independent [shared cache](#shared-cache). A submodule is treated as the fully independent repository it is, with its own commit history separate from the repository it's nested in.
- **Shallow clones are supported.** `git.total.commit.count` and `git.closest.tag.commit.count` are left empty, because a shallow clone doesn't have the full commit history needed to count them. This is reported via `GITPROPS006` (see [Diagnostics](#diagnostics)), so it's never silently incomplete.
- **Detached HEAD is supported.** When building for a pull request, such as in GitHub Actions, Azure DevOps, or Jenkins, well-known environment variables are used to determine the branch name.
- **Git v2.15.0 or later must be installed.** The `git` command must be runnable during your build, either on the `PATH` or at a location you configure with `GitExecutable`.
- **Performance impact.** This package executes real `git` commands, which has a small but real cost. Adding it to every project in a large solution is not recommended. Setting `GenerateGitProperties` to `true` unconditionally, so it always runs, is not recommended either. Add this package only to the projects that actually need `git.properties`, typically your actuator-hosting host apps.
- **An IDE build might not notice a new commit, branch, or tag.** Visual Studio and similar IDEs can skip invoking a real build for a project when none of the files they track (source code, project file, references) have changed, even if you've committed, switched branches, or created a tag since the last build. If `git.properties` looks out of date, force a full rebuild, or build from the command line with `dotnet build`.
- **File deletions inside `.git` aren't tracked.** The [shared cache](#shared-cache) only refreshes when a file is added or changed, not when one is deleted. For example, deleting a tag (`git tag -d`) or turning a shallow clone into a full one (`git fetch --unshallow`) doesn't invalidate the cache by itself. It catches up automatically the next time something else in git changes.
- **Changing your global Git username or email isn't tracked.** The [shared cache](#shared-cache) doesn't notice when you update your name or email in Git's global settings. It catches up automatically the next time something else changes.
