// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

internal sealed class RemotePushProjectTree(string rootDirectory, TestProject testApp)
{
    public string RootDirectory { get; } = rootDirectory;
    public TestProject TestApp { get; } = testApp;

    public bool HasGitDirectory => Directory.Exists(Path.Combine(RootDirectory, ".git"));
}
