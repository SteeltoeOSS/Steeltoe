// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Steeltoe.Common.Utils.IO;
using System.IO;
using Xunit;

namespace Steeltoe.Common.Utils.Test.IO
{
    public class SandboxTest
    {
        [Fact]
        public void DefaultSandboxShouldUseDefaultPrefix()
        {
            // Arrange
            using var sandbox = new Sandbox();

            // Act

            // Assert
            Sandbox.DefaultPrefix.Should().Be("Sandbox-");
            sandbox.Name.Should().StartWith(Sandbox.DefaultPrefix);
        }

        [Fact]
        public void SandboxShouldResolvePaths()
        {
            // Arrange
            using var sandbox = new Sandbox();

            // Act
            var path = sandbox.ResolvePath("some/path");

            // Assert
            path.Should().Be(Path.Join(sandbox.FullPath, "some/path"));
        }

        [Fact]
        public void SandboxShouldCreateDirectories()
        {
            // Arrange
            using var sandbox = new Sandbox();

            // Act
            var path = sandbox.CreateDirectory("path/to/dir");

            // Assert
            Directory.Exists(path).Should().BeTrue();
        }

        [Fact]
        public void SandboxShouldCreateFiles()
        {
            // Arrange
            using var sandbox = new Sandbox();

            // Act
            var path = sandbox.CreateFile("path/to/file");

            // Assert
            File.Exists(path).Should().BeTrue();
        }

        [Fact]
        public void SandboxShouldCreateFilesWithText()
        {
            // Arrange
            using var sandbox = new Sandbox();

            // Act
            var path = sandbox.CreateFile("path/to/file", "mytext");

            // Assert
            File.ReadAllText(path).Should().Be("mytext");
        }
    }
}