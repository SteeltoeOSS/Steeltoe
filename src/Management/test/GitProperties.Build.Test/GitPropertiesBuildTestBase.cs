// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

/// <summary>
/// Shared workspace lifecycle for every test in this project. Deliberately one test per class rather than many <c>[Fact]</c> methods on one shared
/// class: xUnit v3 parallelizes across test classes but never across methods within the same class, and every test here is dominated by "dotnet
/// build"/"publish" subprocess time, so this lets the suite's wall-clock approach its slowest single test instead of the sum of all of them.
/// </summary>
[Trait("Category", "GitProperties")]
public abstract class GitPropertiesBuildTestBase : IAsyncLifetime
{
    internal GitPropertiesTestWorkspace Workspace { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        Workspace = await GitPropertiesTestWorkspace.CreateAsync();
    }

    public ValueTask DisposeAsync()
    {
        Workspace.Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
