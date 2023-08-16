// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Steeltoe.Common.Expression.Internal.Spring;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using Xunit;

namespace Steeltoe.Common.Expression.Test.Spring;

public sealed class StandardTypeLocatorTests
{
    [Fact]
    public void TestImports()
    {
        var locator = new StandardTypeLocator();
        Assert.Equal(typeof(int), locator.FindType("System.Int32"));
        Assert.Equal(typeof(string), locator.FindType("System.String"));

        List<string> prefixes = locator.ImportPrefixes;
        Assert.Single(prefixes);
        Assert.Contains("System", prefixes);
        Assert.DoesNotContain("System.Collections", prefixes);

        Assert.Equal(typeof(bool), locator.FindType("Boolean"));

        var ex = Assert.Throws<SpelEvaluationException>(() => locator.FindType("StringBuilder"));
        Assert.Equal(SpelMessage.TypeNotFound, ex.MessageCode);
        locator.RegisterImport("System.Text");
        Assert.Equal(typeof(StringBuilder), locator.FindType("StringBuilder"));
    }
}
