// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Spring;

public class MapAccessTests : AbstractExpressionTests
{
    [Fact(Skip = "No Get() method on dictionary")]
    public void TestSimpleMapAccess01()
    {
        Evaluate("TestDictionary.get('monday')", "montag", typeof(string));
    }

    [Fact]
    public void TestMapAccessThroughIndexer()
    {
        Evaluate("TestDictionary['monday']", "montag", typeof(string));
    }

    [Fact]
    public void TestCustomMapAccessor()
    {
        var parser = new SpelExpressionParser();
        var ctx = TestScenarioCreator.GetTestEvaluationContext();
        ctx.AddPropertyAccessor(new MapAccessor());

        var expr = parser.ParseExpression("TestDictionary.monday");
        var value = expr.GetValue(ctx, typeof(string));
        Assert.Equal("montag", value);
    }

    [Fact]
    public void TestVariableMapAccess()
    {
        var parser = new SpelExpressionParser();
        var ctx = TestScenarioCreator.GetTestEvaluationContext();
        ctx.SetVariable("day", "saturday");

        var expr = parser.ParseExpression("TestDictionary[#day]");
        var value = expr.GetValue(ctx, typeof(string));
        Assert.Equal("samstag", value);
    }

    [Fact]
    public void TestGetValue()
    {
        var props1 = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" },
            { "key3", "value3" }
        };

        object bean = new TestBean("name1", new TestBean("name2", null, "Description 2", 15, props1), "description 1", 6, props1);

        var parser = new SpelExpressionParser();
        var expr = parser.ParseExpression("TestService.Properties['key2']");
        Assert.Equal("value2", expr.GetValue(bean));
    }

    [Fact]
    public void TestGetValueFromRootMap()
    {
        var map = new Dictionary<string, string>
        {
            { "key", "value" }
        };

        var spelExpressionParser = new SpelExpressionParser();
        var expr = spelExpressionParser.ParseExpression("#root['key']");
        Assert.Equal("value", expr.GetValue(map));
    }

    [Fact(Skip = "Time sensitive test, sometimes fails on CI")]

    public void TestGetValuePerformance()
    {
        var map = new Dictionary<string, string>
        {
            { "key", "value" }
        };
        var context = new StandardEvaluationContext(map);

        var spelExpressionParser = new SpelExpressionParser();
        var expr = spelExpressionParser.ParseExpression("#root['key']");

        var s = new Stopwatch();
        s.Start();
        for (var i = 0; i < 10000; i++)
        {
            expr.GetValue(context);
        }

        s.Stop();
        Assert.True(s.ElapsedMilliseconds < 200L);
    }

    public class TestBean
    {
        public TestBean(string name, TestBean testBean, string description, int priority, Dictionary<string, string> props)
        {
            Name = name;
            TestService = testBean;
            Description = description;
            Priority = priority;
            Properties = props;
        }

        public string Name { get; set; }

        public TestBean TestService { get; set; }

        public string Description { get; set; }

        public int Priority { get; set; }

        public Dictionary<string, string> Properties { get; set; }
    }

    public class MapAccessor : IPropertyAccessor
    {
        public bool CanRead(IEvaluationContext context, object target, string name)
        {
            return ((IDictionary)target).Contains(name);
        }

        public ITypedValue Read(IEvaluationContext context, object target, string name)
        {
            return new TypedValue(((IDictionary)target)[name]);
        }

        public bool CanWrite(IEvaluationContext context, object target, string name)
        {
            return true;
        }

        public void Write(IEvaluationContext context, object target, string name, object newValue)
        {
            ((IDictionary)target).Add(name, newValue);
        }

        public IList<Type> GetSpecificTargetClasses()
        {
            return new List<Type>() { typeof(IDictionary) };
        }
    }
}