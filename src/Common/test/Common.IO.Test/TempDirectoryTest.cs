// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.IO;
using Xunit;

namespace Steeltoe.Common.IO.Test
{
    public class TempDirectoryTest
    {
        [Fact]
        public void TempDirectoryRemovesItself()
        {
            var tempDir = new TempDirectory();
            Assert.True(Directory.Exists(tempDir.FullPath));
            File.Create(Path.Join(tempDir.FullPath, "foo")).Dispose();
            tempDir.Dispose();
            Assert.False(Directory.Exists(tempDir.FullPath));
        }

        [Fact]
        public void TempDirectoryCanBeNamed()
        {
            using var tempDir = new TempDirectory("Jill");
            Assert.Equal("Jill", tempDir.Name);
        }
    }
}