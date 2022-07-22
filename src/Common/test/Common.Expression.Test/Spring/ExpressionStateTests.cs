// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Support;
using Steeltoe.Common.Expression.Internal.Spring.TestResources;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Spring;

public class ExpressionStateTests : AbstractExpressionTests
{
    [Fact]
    public void TestConstruction()
    {
        var context = TestScenarioCreator.GetTestEvaluationContext();
        var state = new ExpressionState(context);
        Assert.Equal(context, state.EvaluationContext);
    }

    [Fact]
    public void TestLocalVariables()
    {
        var state = GetState();

        var value = state.LookupLocalVariable("foo");
        Assert.Null(value);

        state.SetLocalVariable("foo", 34);
        value = state.LookupLocalVariable("foo");
        Assert.Equal(34, value);

        state.SetLocalVariable("foo", null);
        value = state.LookupLocalVariable("foo");
        Assert.Null(value);
    }

    [Fact]
    public void TestVariables()
    {
        var state = GetState();
        var typedValue = state.LookupVariable("foo");
        Assert.Equal(TypedValue.NULL, typedValue);

        state.SetVariable("foo", 34);
        typedValue = state.LookupVariable("foo");
        Assert.Equal(34, typedValue.Value);
        Assert.Equal(typeof(int), typedValue.TypeDescriptor);

        state.SetVariable("foo", "abc");
        typedValue = state.LookupVariable("foo");
        Assert.Equal("abc", typedValue.Value);
        Assert.Equal(typeof(string), typedValue.TypeDescriptor);
    }

    [Fact]
    public void TestNoVariableInterference()
    {
        var state = GetState();
        var typedValue = state.LookupVariable("foo");
        Assert.Equal(TypedValue.NULL, typedValue);

        state.SetLocalVariable("foo", 34);
        typedValue = state.LookupVariable("foo");
        Assert.Equal(TypedValue.NULL, typedValue);

        state.SetVariable("goo", "hello");
        Assert.Null(state.LookupLocalVariable("goo"));
    }

    [Fact]
    public void TestLocalVariableNestedScopes()
    {
        var state = GetState();
        Assert.Null(state.LookupLocalVariable("foo"));

        state.SetLocalVariable("foo", 12);
        Assert.Equal(12, state.LookupLocalVariable("foo"));

        state.EnterScope(null);

        // found in upper scope
        Assert.Equal(12, state.LookupLocalVariable("foo"));

        state.SetLocalVariable("foo", "abc");

        // found in nested scope
        Assert.Equal("abc", state.LookupLocalVariable("foo"));

        state.ExitScope();

        // found in nested scope
        Assert.Equal(12, state.LookupLocalVariable("foo"));
    }

    [Fact]
    public void TestRootContextObject()
    {
        var state = GetState();
        Assert.IsType<Inventor>(state.RootContextObject.Value);

        // although the root object is being set on the evaluation context, the value in the 'state' remains what it was when constructed
        ((StandardEvaluationContext)state.EvaluationContext).SetRootObject(null);
        Assert.IsType<Inventor>(state.RootContextObject.Value);

        // assertEquals(null, state.RootContextObject.Value);
        state = new ExpressionState(new StandardEvaluationContext());
        Assert.Equal(TypedValue.NULL, state.RootContextObject);

        ((StandardEvaluationContext)state.EvaluationContext).SetRootObject(null);
        Assert.Null(state.RootContextObject.Value);
    }

    [Fact]
    public void TestActiveContextObject()
    {
        var state = GetState();
        Assert.Equal(state.RootContextObject.Value, state.GetActiveContextObject().Value);

        Assert.Throws<InvalidOperationException>(() => state.PopActiveContextObject());

        state.PushActiveContextObject(new TypedValue(34));
        Assert.Equal(34, state.GetActiveContextObject().Value);

        state.PushActiveContextObject(new TypedValue("hello"));
        Assert.Equal("hello", state.GetActiveContextObject().Value);

        state.PopActiveContextObject();
        Assert.Equal(34, state.GetActiveContextObject().Value);

        state.PopActiveContextObject();
        Assert.Equal(state.RootContextObject.Value, state.GetActiveContextObject().Value);

        state = new ExpressionState(new StandardEvaluationContext());
        Assert.Equal(TypedValue.NULL, state.GetActiveContextObject());
    }

    [Fact]
    public void TestPopulatedNestedScopes()
    {
        var state = GetState();
        Assert.Null(state.LookupLocalVariable("foo"));

        state.EnterScope("foo", 34);
        Assert.Equal(34, state.LookupLocalVariable("foo"));

        state.EnterScope(null);
        state.SetLocalVariable("foo", 12);
        Assert.Equal(12, state.LookupLocalVariable("foo"));

        state.ExitScope();
        Assert.Equal(34, state.LookupLocalVariable("foo"));

        state.ExitScope();
        Assert.Null(state.LookupLocalVariable("goo"));
    }

    [Fact]
    public void TestRootObjectConstructor()
    {
        var ctx = GetContext();

        // TypedValue root = ctx.getRootObject();
        // supplied should override root on context
        var state = new ExpressionState(ctx, new TypedValue("i am a string"));
        var stateRoot = state.RootContextObject;
        Assert.Equal(typeof(string), stateRoot.TypeDescriptor);
        Assert.Equal("i am a string", stateRoot.Value);
    }

    [Fact]
    public void TestPopulatedNestedScopesMap()
    {
        var state = GetState();
        Assert.Null(state.LookupLocalVariable("foo"));
        Assert.Null(state.LookupLocalVariable("goo"));

        var m = new Dictionary<string, object>
        {
            { "foo", 34 },
            { "goo", "abc" }
        };

        state.EnterScope(m);
        Assert.Equal(34, state.LookupLocalVariable("foo"));
        Assert.Equal("abc", state.LookupLocalVariable("goo"));

        state.EnterScope(null);
        state.SetLocalVariable("foo", 12);
        Assert.Equal(12, state.LookupLocalVariable("foo"));
        Assert.Equal("abc", state.LookupLocalVariable("goo"));

        state.ExitScope();
        state.ExitScope();
        Assert.Null(state.LookupLocalVariable("foo"));
        Assert.Null(state.LookupLocalVariable("goo"));
    }

    [Fact]
    public void TestOperators()
    {
        var state = GetState();
        var ex = Assert.Throws<SpelEvaluationException>(() => state.Operate(Operation.ADD, 1, 2));
        Assert.Equal(SpelMessage.OPERATOR_NOT_SUPPORTED_BETWEEN_TYPES, ex.MessageCode);

        ex = Assert.Throws<SpelEvaluationException>(() => state.Operate(Operation.ADD, null, null));
        Assert.Equal(SpelMessage.OPERATOR_NOT_SUPPORTED_BETWEEN_TYPES, ex.MessageCode);
    }

    [Fact]
    public void TestComparator()
    {
        var state = GetState();
        Assert.Equal(state.EvaluationContext.TypeComparator, state.TypeComparator);
    }

    [Fact]
    public void TestTypeLocator()
    {
        var state = GetState();
        Assert.NotNull(state.EvaluationContext.TypeLocator);
        Assert.Equal(typeof(int), state.FindType("System.Int32"));
        var ex = Assert.Throws<SpelEvaluationException>(() => state.FindType("someMadeUpName"));
        Assert.Equal(SpelMessage.TYPE_NOT_FOUND, ex.MessageCode);
    }

    [Fact]
    public void TestTypeConversion()
    {
        var state = GetState();
        var s = (string)state.ConvertValue(34, typeof(string));
        Assert.Equal("34", s);

        s = (string)state.ConvertValue(new TypedValue(34), typeof(string));
        Assert.Equal("34", s);
    }

    [Fact]
    public void TestPropertyAccessors()
    {
        var state = GetState();
        Assert.Equal(state.EvaluationContext.PropertyAccessors, state.PropertyAccessors);
    }

    private ExpressionState GetState()
    {
        var context = TestScenarioCreator.GetTestEvaluationContext();
        var state = new ExpressionState(context);
        return state;
    }

    private IEvaluationContext GetContext()
    {
        return TestScenarioCreator.GetTestEvaluationContext();
    }
}