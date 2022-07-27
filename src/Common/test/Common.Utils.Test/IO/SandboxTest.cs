// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Steeltoe.Common.Utils.IO;
using System.IO;
using Xunit;

namespace Steeltoe.Common.Utils.Test.IO;

public class SandboxTest
{
    [Fact]
    public void DefaultSandboxShouldUseDefaultPrefix()
    {
        using var sandbox = new Sandbox();

        Sandbox.DefaultPrefix.Should().Be("Sandbox-");
        sandbox.Name.Should().StartWith(Sandbox.DefaultPrefix);
    }

    [Fact]
    public void SandboxShouldResolvePaths()
    {
        using var sandbox = new Sandbox();

        var path = sandbox.ResolvePath("some/path");

        path.Should().Be(Path.Join(sandbox.FullPath, "some/path"));
    }

    [Fact]
    public void SandboxShouldCreateDirectories()
    {
        using var sandbox = new Sandbox();

        var path = sandbox.CreateDirectory("path/to/dir");

        Directory.Exists(path).Should().BeTrue();
    }

    [Fact]
    public void SandboxShouldCreateFiles()
    {
        using var sandbox = new Sandbox();

        var path = sandbox.CreateFile("path/to/file");

        File.Exists(path).Should().BeTrue();
    }

    [Fact]
    public void SandboxShouldCreateFilesWithText()
    {
        using var sandbox = new Sandbox();

        var path = sandbox.CreateFile("path/to/file", "mytext");

        File.ReadAllText(path).Should().Be("mytext");
    }
}