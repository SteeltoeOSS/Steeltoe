// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Steeltoe.Common.Utils.IO;
using System.IO;
using Xunit;

namespace Steeltoe.Common.Utils.Test.IO
{
    public class TempFileTest
    {
        [Fact]
        public void TempFileRemovesItself()
        {
            // Arrange
            var tempFile = new TempFile();

            // Act

            // Assert
            File.Exists(tempFile.FullPath).Should().BeTrue();

            // Act
            tempFile.Dispose();

            // Assert
            File.Exists(tempFile.FullPath).Should().BeFalse();
        }

        [Fact]
        public void TempFileCanSetPrefix()
        {
            // Arrange
            const string prefix = "MyPrefix-";
            using var tempFile = new TempFile(prefix);

            // Act
            // Assert
            tempFile.Name.Should().StartWith(prefix);
        }
    }
}