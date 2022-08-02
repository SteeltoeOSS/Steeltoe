// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Stream.Binding;

public class InvalidBindingConfigurationTest : AbstractTest
{
    [Fact]
    public void TestDuplicateBindingConfig()
    {
        List<string> searchDirectories = GetSearchDirectories("MockBinder");

        Assert.Throws<InvalidOperationException>(() => CreateStreamsContainerWithBinding(
            searchDirectories, typeof(ITestInvalidBinding), "spring.cloud.stream.defaultbinder=mock"));
    }
}
