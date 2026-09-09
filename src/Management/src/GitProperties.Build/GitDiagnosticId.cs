// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build;

internal enum GitDiagnosticId
{
    GitRepositoryNotFound = 1,
    GitRepositoryInvalid = 2,
    GitExecutableNotFound = 3,
    IncompatibleGitVersion = 4,
    GitRepositoryHasNoCommits = 5,
    GitRepositoryIsShallowClone = 6,
    GitDirtyStateUnknown = 7
}
