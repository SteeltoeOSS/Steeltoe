// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Spring.Standard;

public class PropertiesConversionSpelTests
{
    private static readonly SpelExpressionParser Parser = new();

    [Fact]
    public void Props()
    {
        var props = new Dictionary<string, string>
        {
            { "x", "1" },
            { "y", "2" },
            { "z", "3" }
        };

        IExpression expression = Parser.ParseExpression("Foo(#props)");
        var context = new StandardEvaluationContext();
        context.SetVariable("props", props);
        string result = expression.GetValue<string>(context, new TestBean());
        Assert.Equal("123", result);
    }

    [Fact]
    public void MapWithAllStringValues()
    {
        var map = new Dictionary<string, object>
        {
            { "x", "1" },
            { "y", "2" },
            { "z", "3" }
        };

        IExpression expression = Parser.ParseExpression("Foo(#props)");
        var context = new StandardEvaluationContext();
        context.SetVariable("props", map);
        string result = expression.GetValue<string>(context, new TestBean());
        Assert.Equal("123", result);
    }

    [Fact]
    public void MapWithNonStringValue()
    {
        var map = new Dictionary<string, object>
        {
            { "x", "1" },
            { "y", 2 },
            { "z", "3" },
            { "a", Guid.NewGuid() }
        };

        IExpression expression = Parser.ParseExpression("Foo(#props)");
        var context = new StandardEvaluationContext();
        context.SetVariable("props", map);
        string result = expression.GetValue<string>(context, new TestBean());
        Assert.Equal("123", result);
    }

    [Fact]
    public void CustomMapWithNonStringValue()
    {
        var map = new CustomMap
        {
            { "x", "1" },
            { "y", 2 },
            { "z", "3" }
        };

        IExpression expression = Parser.ParseExpression("Foo(#props)");
        var context = new StandardEvaluationContext();
        context.SetVariable("props", map);
        string result = expression.GetValue<string>(context, new TestBean());
        Assert.Equal("123", result);
    }

    public class TestBean
    {
        public string Foo(IDictionary props)
        {
            return props["x"]?.ToString() + props["y"] + props["z"];
        }
    }

    public class CustomMap : Dictionary<string, object>
    {
    }
}
