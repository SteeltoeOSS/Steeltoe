// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Steeltoe.Common.Utils.IO;
using Xunit;

namespace Steeltoe.Common.Utils.Test.IO;

public class TempFileTest
{
    [Fact]
    public void TempFileRemovesItself()
    {
        var tempFile = new TempFile();

        File.Exists(tempFile.FullPath).Should().BeTrue();

        tempFile.Dispose();

        File.Exists(tempFile.FullPath).Should().BeFalse();
    }

    [Fact]
    public void TempFileCanSetPrefix()
    {
        const string prefix = "MyPrefix-";
        using var tempFile = new TempFile(prefix);

        tempFile.Name.Should().StartWith(prefix);
    }
}
