# Steeltoe Code Management

## File Header - Copyright and License

All source code files (i.e. `src/**/*.cs and test/**/*.cs`) require this header:

```csharp
// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
```

Every repository also needs the Apache 2.0 License in a file called `LICENSE` in the root of the repository.

## Copying Files from Other Projects

At times Steeltoe may use some files from other projects, typically where a binary distribution does not exist or would be inconvenient to use.

The following rules must be followed for any code contributions that include files from another project:

* The license of the file is [permissive](https://en.wikipedia.org/wiki/Permissive_free_software_licence).
* The license of the file is left in-tact.
* The contribution is correctly attributed in the `third party notices`(e.g. NOTICES) file in the repository to which it is being contributed.

## Building the Code

Included in each Steeltoe repository are instructions for how to build the code and run unit tests.

Please ensure any contributions you wish to make follow these basic set of instructions. If you make need to make modifications please ensure you update the instructions in the `README.md`.

## Porting Files from Other Projects

There may be times in which code developed in other languages could benefit the Steeltoe project. The rules for porting a Java file to C#, are the same as would be used for copying the same file, as described above.

## External Dependencies

Here we are describing the dependencies on projects (i.e. NuGet packages) outside of the Steeltoe repositories. It is important that we carefully manage our dependencies properly. If you need to add or update any external dependency, it should be discussed with the project lead(s) or project maintainer(s) first.

## Code Reviews

To help ensure that only the highest quality code makes its way into the project, please submit all your code contributions to GitHub as PRs. This includes runtime code changes, unit test updates, and updates to official samples.

The advantages are numerous, including improving code quality, more visibility on changes and their potential impact, avoiding duplication of effort, and creating general awareness of progress being made in various areas.

The code maintainer for that project repository will review your contribution and sign off and merge it once it has been reviewed.

## Repository Structure

Steeltoe repositories are organized by the functional area they serve (e.g. Configuration, Discovery, CircuitBreaker, etc.).  Within each repository, the structure of its contents follow pretty much the same pattern as below.  If you are contributing code or a new project please try to follow these guidelines.

```text
\
    config\     - configuration files
    scripts\    - CI build scripts
    src\        - one or more projects and related source code
    test\       - corresponding unit test projects and related source code
```

Within each `src` and `test` folder you will find one or more of the projects that pertain to the functional area. The general naming pattern that is followed is `Steeltoe.<area>.<subarea>` or `Steeltoe.Extensions.<area>.<subarea>` and `Steeltoe.<area>.<subarea>.Test` or `Steeltoe.Extensions.<area>.<subarea>.Test`.

Here is an example from the `Configuration` repository, in which we have two projects along with the corresponding test projects:

```text
config\
scripts\
src\
    Steeltoe.Extensions.Configuration.CloudFoundry\
        A.cs
        B.cs
        Steeltoe.Extensions.Configuration.CloudFoundry.csproj
    Steeltoe.Extensions.Configuration.ConfigServer\
        A.cs
        B.cs
        Steeltoe.Extensions.Configuration.ConfigServer.csproj
test\
    Steeltoe.Extensions.Configuration.CloudFoundry.Test\
        ATest.cs
        BTest.cs
        Steeltoe.Extensions.Configuration.CloudFoundry.Test.csproj
    Steeltoe.Extensions.Configuration.ConfigServe.Test\
        ATest.cs
        BTest.cs
        Steeltoe.Extensions.Configuration.ConfigServer.Test.csproj
```

All solution files go in the repository root folder.

Solution names should typically match repository names (e.g. `Configuration.sln` in the `Configuration` repository).

The solutions need to contain solution folders that match the physical folders (src, test, etc.).

## New Repositories

If your contribution requires a new repository to be created in the `SteeltoeOSS` organization, contact the project lead(s) or a project maintainer to have that done.

## Project Branch Strategy

In general, all new development is done on the `dev` branch in each Steeltoe repository. Consider any code on the `dev` branch to be less stable and in flux or changing.

Periodically, when project maintainers feel the code on the `dev` branch has reached a level of maturity, they will merge the dev branch into `master`. You should consider code on the `master` branch to be much more stable.

All releases and release candidates are tagged off the `master` branch.

## Unit Tests

Steeltoe uses [xUnit](https://xunit.github.io/) for all unit testing.
