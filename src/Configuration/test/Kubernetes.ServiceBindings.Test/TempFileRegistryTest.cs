// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.Kubernetes.ServiceBindings.Test;

public sealed class TempFileRegistryTest
{
    [Fact]
    public void Creates_file_with_content()
    {
        string path;

        using (var registry = new TempFileRegistry())
        {
            path = registry.GetOrCreate("secret-content");

            File.ReadAllText(path).Should().Be("secret-content");
        }

        File.Exists(path).Should().BeFalse();
    }

    [Fact]
    public void Reuses_file_for_same_content()
    {
        using var registry = new TempFileRegistry();

        string path1 = registry.GetOrCreate("same-content");
        string path2 = registry.GetOrCreate("same-content");

        path2.Should().Be(path1);
    }

    [Fact]
    public void Creates_different_files_for_different_content()
    {
        using var registry = new TempFileRegistry();

        string path1 = registry.GetOrCreate("first-content");
        string path2 = registry.GetOrCreate("second-content");

        path2.Should().NotBe(path1);

        File.ReadAllText(path1).Should().Be("first-content");
        File.ReadAllText(path2).Should().Be("second-content");
    }

    [Fact]
    public void Creates_file_again_when_it_was_deleted()
    {
        using var registry = new TempFileRegistry();

        string path1 = registry.GetOrCreate("some-content");
        File.Delete(path1);

        string path2 = registry.GetOrCreate("some-content");

        File.ReadAllText(path2).Should().Be("some-content");
    }

    [Fact]
    public void Can_be_used_again_after_dispose()
    {
        var registry = new TempFileRegistry();

        string path1 = registry.GetOrCreate("some-content");
        registry.Dispose();

        File.Exists(path1).Should().BeFalse();

        string path2 = registry.GetOrCreate("some-content");

        try
        {
            File.ReadAllText(path2).Should().Be("some-content");
        }
        finally
        {
            registry.Dispose();
        }

        File.Exists(path2).Should().BeFalse();
    }
}
