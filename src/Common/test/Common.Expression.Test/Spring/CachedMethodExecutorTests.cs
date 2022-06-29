// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Spring;

public class CachedMethodExecutorTests
{
    private readonly IExpressionParser _parser = new SpelExpressionParser();
    private readonly StandardEvaluationContext _context = new (new RootObject());

    [Fact]
    public void TestCachedExecutionForParameters()
    {
        var expression = _parser.ParseExpression("Echo(#var)");

        AssertMethodExecution(expression, 42, "int: 42");
        AssertMethodExecution(expression, 42, "int: 42");
        AssertMethodExecution(expression, "Deep Thought", "String: Deep Thought");
        AssertMethodExecution(expression, 42, "int: 42");
    }

    [Fact]
    public void TestCachedExecutionForTarget()
    {
        var expression = _parser.ParseExpression("#var.Echo(42)");

        AssertMethodExecution(expression, new RootObject(), "int: 42");
        AssertMethodExecution(expression, new RootObject(), "int: 42");
        AssertMethodExecution(expression, new BaseObject(), "String: 42");
        AssertMethodExecution(expression, new RootObject(), "int: 42");
    }

    private void AssertMethodExecution(IExpression expression, object var, string expected)
    {
        _context.SetVariable("var", var);
        Assert.Equal(expected, expression.GetValue(_context));
    }

    public class BaseObject
    {
        public string Echo(string value)
        {
            return $"String: {value}";
        }
    }

    public class RootObject : BaseObject
    {
        public string Echo(int value)
        {
            return $"int: {value}";
        }
    }
}
