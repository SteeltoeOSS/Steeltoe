// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

/// <summary>
/// Shared workspace lifecycle for every test in this project. Split one test per class - rather than many <c>[Fact]</c> methods on one shared class -
/// purely for parallelism: xUnit v3 runs different test classes concurrently by default (no configuration or version upgrade needed - verified
/// empirically), but never parallelizes methods within the SAME class. Since every test here is dominated by "dotnet build"/"publish" subprocess time
/// that's mostly I/O/wait-bound, not CPU-bound (measured: 4 concurrent builds complete in ~1.3x one build's time, 8 concurrent in ~1.9x),
/// one-class-per-test lets the whole suite's wall-clock approach its slowest single test instead of the sum of all of them.
/// </summary>
/// <remarks>
/// Public (xUnit only discovers public test classes), but <see cref="Workspace" /> is internal rather than protected: GitPropertiesTestWorkspace is
/// itself internal, and a protected member of a public class can't expose a less-accessible type in its signature (CS0051/CS0052). Internal still works
/// the same way for every derived class here, since they all live in this same assembly. Implements IAsyncLifetime (not a constructor) to create
/// <see cref="Workspace" />: GitPropertiesTestWorkspace.CreateAsync itself awaits a "pwd -P" subprocess on macOS to resolve a symlink-free root path,
/// and a constructor can't await.
/// </remarks>
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
