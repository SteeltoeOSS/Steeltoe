// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Steeltoe.Common.Utils.IO;
using System.IO;
using Xunit;

namespace Steeltoe.Common.Utils.Test.IO
{
    public class TempDirectoryTest
    {
        [Fact]
        public void TempDirectoryRemovesItself()
        {
            // Arrange
            var tempDir = new TempDirectory();

            // Act

            // Assert
            Directory.Exists(tempDir.FullPath).Should().BeTrue();

            // Act
            File.Create(Path.Join(tempDir.FullPath, "foo")).Dispose();
            tempDir.Dispose();

            // Assert
            Directory.Exists(tempDir.FullPath).Should().BeFalse();
        }

        [Fact]
        public void TempDirectoryCanSetPrefix()
        {
            // Arrange
            const string prefix = "MyPrefix-";
            using var tempDir = new TempDirectory(prefix);

            // Act

            // Assert
            tempDir.Name.Should().StartWith(prefix);
        }
    }
}