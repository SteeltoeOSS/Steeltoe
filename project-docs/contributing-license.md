# Contributor License Agreement

You must sign a [.NET Foundation Contribution License Agreement (CLA)](https://cla.dotnetfoundation.org) before your Pull Request will be merged. This is a one-time requirement for projects in the .NET Foundation. You can read more about [Contribution License Agreements (CLA)](https://en.wikipedia.org/wiki/Contributor_License_Agreement) on Wikipedia.

The agreement: [net-foundation-contribution-license-agreement.pdf](https://cla.dotnetfoundation.org/cladoc/net-foundation-contribution-license-agreement.pdf)

You don't have to do this up-front. You can simply clone, fork, and submit your pull-request as usual. When your pull-request is created, it is classified by a CLA bot. If the change is trivial (for example, you just fixed a typo), then the PR is labelled with cla-not-required. Otherwise it's classified as cla-required. Once you signed a CLA, the current and all future pull-requests will be labelled as cla-signed.

## Code Copyrights

The Steeltoe project maintains that all code copyrights are held by the original author(s), but licensed back, under an Apache license, to the greater community of users.

## Source License

The Steeltoe project uses the following licenses:

* The  [Apache 2 License](https://opensource.org/licenses/Apache-2.0).

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

Every Steeltoe repository must also contain an Apache 2.0 License in a file called `LICENSE` in the root of the repository.

## Copying Files from Other Projects

At times Steeltoe may use some files from other projects, typically where a binary distribution does not exist or would be inconvenient to use.

The following rules must be followed for any code contributions that include files from another project:

* The license of the file is [permissive](https://en.wikipedia.org/wiki/Permissive_free_software_licence).
* The license of the file is left in-tact.
* The contribution is correctly attributed in the `third party notices`(e.g. NOTICES) file in the repository to which it is being contributed.

## Porting Files from Other Projects

There may be times in which code developed in other languages could benefit the Steeltoe project. The rules for porting say a Java file to C#, are the same as would be used for copying the same file, as described above.
