// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.IO;
using Xunit;

namespace Steeltoe.Common.IO.Test
{
    public class TempFileTest
    {
        [Fact]
        public void TempFileRemovesItself()
        {
            var tempFile = new TempFile();
            Assert.True(File.Exists(tempFile.FullPath));
            tempFile.Dispose();
            Assert.False(File.Exists(tempFile.FullPath));
        }

        [Fact]
        public void TempFileCanBeNamed()
        {
            using var tempFile = new TempFile("Jack");
            Assert.Equal("Jack", tempFile.Name);
        }
    }
}