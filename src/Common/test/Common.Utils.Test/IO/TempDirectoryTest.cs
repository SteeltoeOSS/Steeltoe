// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Steeltoe.Common.Utils.IO;
using System.IO;
using Xunit;

namespace Steeltoe.Common.Utils.Test.IO;

public class TempDirectoryTest
{
    [Fact]
    public void TempDirectoryRemovesItself()
    {
        var tempDir = new TempDirectory();

        Directory.Exists(tempDir.FullPath).Should().BeTrue();

        File.Create(Path.Join(tempDir.FullPath, "foo")).Dispose();
        tempDir.Dispose();

        Directory.Exists(tempDir.FullPath).Should().BeFalse();
    }

    [Fact]
    public void TempDirectoryCanSetPrefix()
    {
        const string prefix = "MyPrefix-";
        using var tempDir = new TempDirectory(prefix);

        tempDir.Name.Should().StartWith(prefix);
    }
}