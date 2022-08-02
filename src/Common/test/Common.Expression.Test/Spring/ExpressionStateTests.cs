// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Support;
using Steeltoe.Common.Expression.Internal.Spring.TestResources;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Spring;

public class ExpressionStateTests : AbstractExpressionTests
{
    [Fact]
    public void TestConstruction()
    {
        StandardEvaluationContext context = TestScenarioCreator.GetTestEvaluationContext();
        var state = new ExpressionState(context);
        Assert.Equal(context, state.EvaluationContext);
    }

    [Fact]
    public void TestLocalVariables()
    {
        ExpressionState state = GetState();

        object value = state.LookupLocalVariable("foo");
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
        ExpressionState state = GetState();
        ITypedValue typedValue = state.LookupVariable("foo");
        Assert.Equal(TypedValue.Null, typedValue);

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
        ExpressionState state = GetState();
        ITypedValue typedValue = state.LookupVariable("foo");
        Assert.Equal(TypedValue.Null, typedValue);

        state.SetLocalVariable("foo", 34);
        typedValue = state.LookupVariable("foo");
        Assert.Equal(TypedValue.Null, typedValue);

        state.SetVariable("goo", "hello");
        Assert.Null(state.LookupLocalVariable("goo"));
    }

    [Fact]
    public void TestLocalVariableNestedScopes()
    {
        ExpressionState state = GetState();
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
        ExpressionState state = GetState();
        Assert.IsType<Inventor>(state.RootContextObject.Value);

        // although the root object is being set on the evaluation context, the value in the 'state' remains what it was when constructed
        ((StandardEvaluationContext)state.EvaluationContext).SetRootObject(null);
        Assert.IsType<Inventor>(state.RootContextObject.Value);

        // assertEquals(null, state.RootContextObject.Value);
        state = new ExpressionState(new StandardEvaluationContext());
        Assert.Equal(TypedValue.Null, state.RootContextObject);

        ((StandardEvaluationContext)state.EvaluationContext).SetRootObject(null);
        Assert.Null(state.RootContextObject.Value);
    }

    [Fact]
    public void TestActiveContextObject()
    {
        ExpressionState state = GetState();
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
        Assert.Equal(TypedValue.Null, state.GetActiveContextObject());
    }

    [Fact]
    public void TestPopulatedNestedScopes()
    {
        ExpressionState state = GetState();
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
        IEvaluationContext ctx = GetContext();

        // TypedValue root = ctx.getRootObject();
        // supplied should override root on context
        var state = new ExpressionState(ctx, new TypedValue("i am a string"));
        ITypedValue stateRoot = state.RootContextObject;
        Assert.Equal(typeof(string), stateRoot.TypeDescriptor);
        Assert.Equal("i am a string", stateRoot.Value);
    }

    [Fact]
    public void TestPopulatedNestedScopesMap()
    {
        ExpressionState state = GetState();
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
        ExpressionState state = GetState();
        var ex = Assert.Throws<SpelEvaluationException>(() => state.Operate(Operation.Add, 1, 2));
        Assert.Equal(SpelMessage.OperatorNotSupportedBetweenTypes, ex.MessageCode);

        ex = Assert.Throws<SpelEvaluationException>(() => state.Operate(Operation.Add, null, null));
        Assert.Equal(SpelMessage.OperatorNotSupportedBetweenTypes, ex.MessageCode);
    }

    [Fact]
    public void TestComparator()
    {
        ExpressionState state = GetState();
        Assert.Equal(state.EvaluationContext.TypeComparator, state.TypeComparator);
    }

    [Fact]
    public void TestTypeLocator()
    {
        ExpressionState state = GetState();
        Assert.NotNull(state.EvaluationContext.TypeLocator);
        Assert.Equal(typeof(int), state.FindType("System.Int32"));
        var ex = Assert.Throws<SpelEvaluationException>(() => state.FindType("someMadeUpName"));
        Assert.Equal(SpelMessage.TypeNotFound, ex.MessageCode);
    }

    [Fact]
    public void TestTypeConversion()
    {
        ExpressionState state = GetState();
        string s = (string)state.ConvertValue(34, typeof(string));
        Assert.Equal("34", s);

        s = (string)state.ConvertValue(new TypedValue(34), typeof(string));
        Assert.Equal("34", s);
    }

    [Fact]
    public void TestPropertyAccessors()
    {
        ExpressionState state = GetState();
        Assert.Equal(state.EvaluationContext.PropertyAccessors, state.PropertyAccessors);
    }

    private ExpressionState GetState()
    {
        StandardEvaluationContext context = TestScenarioCreator.GetTestEvaluationContext();
        var state = new ExpressionState(context);
        return state;
    }

    private IEvaluationContext GetContext()
    {
        return TestScenarioCreator.GetTestEvaluationContext();
    }
}
