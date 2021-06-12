// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
            var tempFile = new TempFile();
            Assert.True(File.Exists(tempFile.FullPath));
            tempFile.Dispose();
            Assert.False(File.Exists(tempFile.FullPath));
        }

        [Fact]
        public void TempFileCanBePrefixed()
        {
            const string prefix = "XXX-";
            using var tempFile = new TempFile(prefix);
            Assert.StartsWith(prefix, tempFile.Name);
        }
    }
}
