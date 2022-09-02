// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Common.Converter;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Spring;

public class ExpressionWithConversionTests : AbstractExpressionTests
{
    private static readonly List<string> ListOfString = new();
    private static readonly List<int> ListOfInteger = new();
    private static Type _typeDescriptorForListOfInteger;
    private static Type _typeDescriptorForListOfString;

    static ExpressionWithConversionTests()
    {
        ListOfString.Add("1");
        ListOfString.Add("2");
        ListOfString.Add("3");
        ListOfInteger.Add(4);
        ListOfInteger.Add(5);
        ListOfInteger.Add(6);
    }

    public ExpressionWithConversionTests()
    {
        _typeDescriptorForListOfString = typeof(ExpressionWithConversionTests).GetField("ListOfString", BindingFlags.NonPublic | BindingFlags.Static).FieldType;

        _typeDescriptorForListOfInteger =
            typeof(ExpressionWithConversionTests).GetField("ListOfInteger", BindingFlags.NonPublic | BindingFlags.Static).FieldType;
    }

    [Fact]
    public void TestConversionsAvailable()
    {
        var tcs = new TypeConvertorUsingConversionService();

        // ArrayList containing List<Integer> to List<String>
        Type type = _typeDescriptorForListOfString.GetGenericArguments()[0];
        Assert.Equal(typeof(string), type);
        var l = tcs.ConvertValue(ListOfInteger, ListOfInteger.GetType(), _typeDescriptorForListOfString) as List<string>;
        Assert.NotNull(l);

        // ArrayList containing List<String> to List<Integer>
        type = _typeDescriptorForListOfInteger.GetGenericArguments()[0];
        Assert.Equal(typeof(int), type);

        var l2 = tcs.ConvertValue(ListOfString, ListOfString.GetType(), _typeDescriptorForListOfString) as List<string>;
        Assert.NotNull(l2);
    }

    [Fact]
    public void TestSetParameterizedList()
    {
        StandardEvaluationContext context = TestScenarioCreator.GetTestEvaluationContext();
        IExpression e = Parser.ParseExpression("ListOfInteger.Count");
        Assert.Equal(0, e.GetValue(context, typeof(int)));
        context.TypeConverter = new TypeConvertorUsingConversionService();

        // Assign a List<String> to the List<Integer> field - the component elements should be converted
        Parser.ParseExpression("ListOfInteger").SetValue(context, ListOfString);

        // size now 3
        Assert.Equal(3, e.GetValue(context, typeof(int)));

        // element type correctly Integer
        object objectType = Parser.ParseExpression("ListOfInteger[1].GetType()").GetValue(context, typeof(Type));
        Assert.Equal(typeof(int), objectType);
    }

    [Fact]
    public void TestCoercionToCollectionOfPrimitive()
    {
        var evaluationContext = new StandardEvaluationContext();

        Type collectionType = typeof(TestTarget).GetMethod(nameof(TestTarget.Sum)).GetParameters()[0].ParameterType;

        // The type conversion is possible
        Assert.True(evaluationContext.TypeConverter.CanConvert(typeof(string), collectionType));

        // ... and it can be done successfully
        object result = evaluationContext.TypeConverter.ConvertValue("1,2,3,4", typeof(string), collectionType);
        var asList = result as ICollection<int>;
        Assert.NotNull(asList);
        Assert.Equal(4, asList.Count);

        evaluationContext.SetVariable("target", new TestTarget());

        // OK up to here, so the evaluation should be fine...
        // ... but this fails
        int result2 = (int)Parser.ParseExpression("#target.Sum(#root)").GetValue(evaluationContext, "1,2,3,4");
        Assert.Equal(10, result2);
    }

    [Fact]
    public void TestConvert()
    {
        var root = new Foo("bar");

        ICollection<string> foos = new List<string>
        {
            "baz"
        };

        var context = new StandardEvaluationContext(root);

        // property access
        IExpression expression = Parser.ParseExpression("Foos");
        expression.SetValue(context, foos);
        Foo baz = root.Foos.Single();
        Assert.Equal("baz", baz.Value);

        // method call
        expression = Parser.ParseExpression("Foos=#foos");
        context.SetVariable("foos", foos);
        expression.GetValue(context);
        baz = root.Foos.Single();
        Assert.Equal("baz", baz.Value);

        // method call with result from method call
        expression = Parser.ParseExpression("Foos=FoosAsStrings");
        expression.GetValue(context);
        baz = root.Foos.Single();
        Assert.Equal("baz", baz.Value);

        // method call with result from method call
        expression = Parser.ParseExpression("Foos=FoosAsObjects");
        expression.GetValue(context);
        baz = root.Foos.Single();
        Assert.Equal("baz", baz.Value);
    }

    public class TestTarget
    {
        public int Sum(ICollection<int> numbers)
        {
            int total = 0;

            foreach (int i in numbers)
            {
                total += i;
            }

            return total;
        }
    }

    public class TypeConvertorUsingConversionService : ITypeConverter
    {
        public IConversionService ConversionService { get; set; } = new DefaultConversionService();

        public bool CanConvert(Type sourceType, Type targetType)
        {
            return ConversionService.CanConvert(sourceType, targetType);
        }

        public object ConvertValue(object value, Type sourceType, Type targetType)
        {
            return ConversionService.Convert(value, sourceType, targetType);
        }
    }

    public class Foo
    {
        public string Value { get; }

#pragma warning disable S4004 // Collection properties should be readonly
        public ICollection<Foo> Foos { get; set; }
#pragma warning restore S4004 // Collection properties should be readonly

        public ICollection<string> FoosAsStrings =>
            new List<string>
            {
                "baz"
            };

        public ICollection<object> FoosAsObjects =>
            new List<object>
            {
                "baz"
            };

        public Foo(string value)
        {
            Value = value;
        }
    }
}
