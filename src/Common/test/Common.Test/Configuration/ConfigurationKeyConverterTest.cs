// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Steeltoe.Common.Configuration;
using Xunit;

namespace Steeltoe.Common.Test.Configuration;

public sealed class ConfigurationKeyConverterTest
{
    [Theory]
    [InlineData("foobar", "foobar")]
    [InlineData("foo.bar", "foo:bar")]
    [InlineData("foo__bar__baz", "foo:bar:baz")]
    [InlineData("foo__bar__baz_", "foo:bar:baz_")]
    [InlineData("foo__bar__baz__", "foo:bar:baz__")]
    [InlineData("foo[bar]", "foo[bar]")]
    [InlineData("foobar[1234]", "foobar:1234")]
    [InlineData("foobar[1234][5678]", "foobar:1234:5678")]
    [InlineData("foobar[1234][5678]barbar", "foobar[1234][5678]barbar")]
    [InlineData("a.b.foobar[1234][5678].barfoo.boo[123]", "a:b:foobar:1234:5678:barfoo:boo:123")]
    [InlineData(@"one\.four\\.seven", @"one.four\:seven")]
    public void AsDotNetConfigurationKey_ProducesExpected(string input, string expectedOutput)
    {
        _ = ConfigurationKeyConverter.AsDotNetConfigurationKey(input).Should().BeEquivalentTo(expectedOutput);
    }
}
