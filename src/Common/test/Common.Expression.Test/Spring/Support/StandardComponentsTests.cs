// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using Xunit;

namespace Steeltoe.Common.Expression.Test.Spring.Support;

public class StandardComponentsTests
{
    [Fact]
    public void TestStandardEvaluationContext()
    {
        var context = new StandardEvaluationContext();
        Assert.NotNull(context.TypeComparator);

        var tc = new StandardTypeComparator();
        context.TypeComparator = tc;
        Assert.Equal(tc, context.TypeComparator);

        var tl = new StandardTypeLocator();
        context.TypeLocator = tl;
        Assert.Equal(tl, context.TypeLocator);
    }

    [Fact]
    public void TestStandardOperatorOverloader()
    {
        var oo = new StandardOperatorOverloader();
        Assert.False(oo.OverridesOperation(Operation.Add, null, null));
        Assert.Throws<EvaluationException>(() => oo.Operate(Operation.Add, 2, 3));
    }

    [Fact]
    public void TestStandardTypeLocator()
    {
        var tl = new StandardTypeLocator();
        List<string> prefixes = tl.ImportPrefixes;
        Assert.Single(prefixes);
        tl.RegisterImport("System.Collections");
        prefixes = tl.ImportPrefixes;
        Assert.Equal(2, prefixes.Count);
        tl.RemoveImport("System.Collections");
        prefixes = tl.ImportPrefixes;
        Assert.Single(prefixes);
    }
}
