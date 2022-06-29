// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Standard;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Spring.Support;

public class ReflectionHelperTests : AbstractExpressionTests
{
    [Fact]
    public void TestUtilities()
    {
        var expr = _parser.ParseExpression("3+4+5+6+7-2") as SpelExpression;
        var ps = new StringWriter();
        SpelUtilities.PrintAbstractSyntaxTree(ps, expr);
        ps.Flush();
        var s = ps.ToString();

        // ===> Expression '3+4+5+6+7-2' - AST start
        //      OperatorMinus  value:(((((3 + 4) + 5) + 6) + 7) - 2)  #children:2
        //        OperatorPlus  value:((((3 + 4) + 5) + 6) + 7)  #children:2
        //          OperatorPlus  value:(((3 + 4) + 5) + 6)  #children:2
        //            OperatorPlus  value:((3 + 4) + 5)  #children:2
        //              OperatorPlus  value:(3 + 4)  #children:2
        //                CompoundExpression  value:3
        //                  IntLiteral  value:3
        //                CompoundExpression  value:4
        //                  IntLiteral  value:4
        //              CompoundExpression  value:5
        //                IntLiteral  value:5
        //            CompoundExpression  value:6
        //              IntLiteral  value:6
        //          CompoundExpression  value:7
        //            IntLiteral  value:7
        //        CompoundExpression  value:2
        //          IntLiteral  value:2
        //      ===> Expression '3+4+5+6+7-2' - AST end
        Assert.Contains("===> Expression '3+4+5+6+7-2' - AST start", s);
        Assert.Contains(" OpPlus  value:((((3 + 4) + 5) + 6) + 7)  #children:2", s);
    }

    [Fact]
    public void TestTypedValue()
    {
        var tv1 = new TypedValue("hello");
        var tv2 = new TypedValue("hello");
        var tv3 = new TypedValue("bye");
        Assert.Equal(typeof(string), tv1.TypeDescriptor);
        Assert.Equal("TypedValue: 'hello' of [System.String]", tv1.ToString());
        Assert.Equal(tv1, tv2);
        Assert.Equal(tv2, tv1);
        Assert.NotEqual(tv1, tv3);
        Assert.NotEqual(tv2, tv3);
        Assert.NotEqual(tv3, tv1);
        Assert.NotEqual(tv3, tv2);
        Assert.Equal(tv1.GetHashCode(), tv2.GetHashCode());
        Assert.NotEqual(tv1.GetHashCode(), tv3.GetHashCode());
        Assert.NotEqual(tv2.GetHashCode(), tv3.GetHashCode());
    }

    [Fact]
    public void TestReflectionHelperCompareArguments_ExactMatching()
    {
        var tc = new StandardTypeConverter();

        // Calling foo(String) with (String) is exact match
        CheckMatch(new[] { typeof(string) }, new[] { typeof(string) }, tc, ArgumentsMatchKind.EXACT);

        // Calling foo(String,Integer) with (String,Integer) is exact match
        CheckMatch(new[] { typeof(string), typeof(int) }, new[] { typeof(string), typeof(int) }, tc, ArgumentsMatchKind.EXACT);
    }

    [Fact]
    public void TestReflectionHelperCompareArguments_CloseMatching()
    {
        var tc = new StandardTypeConverter();

        // Calling foo(List) with (ArrayList) is close match (no conversion required)
        CheckMatch(new[] { typeof(ArrayList) }, new[] { typeof(IList) }, tc, ArgumentsMatchKind.CLOSE);

        // Passing (Sub,String) on call to foo(Super,String) is close match
        CheckMatch(new[] { typeof(Sub), typeof(string) }, new[] { typeof(Super), typeof(string) }, tc, ArgumentsMatchKind.CLOSE);

        // Passing (String,Sub) on call to foo(String,Super) is close match
        CheckMatch(new[] { typeof(string), typeof(Sub) }, new[] { typeof(string), typeof(Super) }, tc, ArgumentsMatchKind.CLOSE);
    }

    [Fact]
    public void TestReflectionHelperCompareArguments_RequiresConversionMatching()
    {
        var tc = new StandardTypeConverter();

        // Calling foo(String,int) with (String,Integer) requires boxing conversion of argument one
        CheckMatch(new[] { typeof(string), typeof(int) }, new[] { typeof(string), typeof(object) }, tc, ArgumentsMatchKind.CLOSE);

        // Passing (int,String) on call to foo(Integer,String) requires boxing conversion of argument zero
        CheckMatch(new[] { typeof(int), typeof(string) }, new[] { typeof(object), typeof(string) }, tc, ArgumentsMatchKind.CLOSE);

        // Passing (int,Sub) on call to foo(Integer,Super) requires boxing conversion of argument zero
        CheckMatch(new[] { typeof(int), typeof(Sub) }, new[] { typeof(object), typeof(Super) }, tc, ArgumentsMatchKind.CLOSE);

        // Passing (int,Sub,boolean) on call to foo(Integer,Super,Boolean) requires boxing conversion of arguments zero and two
        // TODO CheckMatch(new Type[] {Integer.TYPE, typeof(Sub), Boolean.TYPE}, new Type[] {typeof(int), typeof(Super), Boolean.class}, tc, ArgsMatchKind.REQUIRES_CONVERSION);
    }

    [Fact]
    public void TestReflectionHelperCompareArguments_NotAMatch()
    {
        var typeConverter = new StandardTypeConverter();

        // Passing (Super,String) on call to foo(Sub,String) is not a match
        CheckMatch(new[] { typeof(Super), typeof(string) }, new[] { typeof(Sub), typeof(string) }, typeConverter, null);
    }

    [Fact]
    public void TestReflectionHelperCompareArguments_Varargs_ExactMatching()
    {
        var tc = new StandardTypeConverter();

        // Passing (String[]) on call to (String[]) is exact match
        CheckMatch2(new[] { typeof(string[]) }, new[] { typeof(string[]) }, tc, ArgumentsMatchKind.EXACT);

        // Passing (Integer, String[]) on call to (Integer, String[]) is exact match
        CheckMatch2(new[] { typeof(int), typeof(string[]) }, new[] { typeof(int), typeof(string[]) }, tc, ArgumentsMatchKind.EXACT);

        // Passing (String, Integer, String[]) on call to (String, String, String[]) is exact match
        CheckMatch2(new[] { typeof(string), typeof(int), typeof(string[]) }, new[] { typeof(string), typeof(int), typeof(string[]) }, tc, ArgumentsMatchKind.EXACT);

        // Passing (Sub, String[]) on call to (Super, String[]) is exact match
        CheckMatch2(new[] { typeof(Sub), typeof(string[]) }, new[] { typeof(Super), typeof(string[]) }, tc, ArgumentsMatchKind.CLOSE);

        // Passing (Integer, String[]) on call to (String, String[]) is exact match
        CheckMatch2(new[] { typeof(int), typeof(string[]) }, new[] { typeof(string), typeof(string[]) }, tc, ArgumentsMatchKind.REQUIRES_CONVERSION);

        // Passing (Integer, Sub, String[]) on call to (String, Super, String[]) is exact match
        CheckMatch2(new[] { typeof(int), typeof(Sub), typeof(string[]) }, new[] { typeof(string), typeof(Super), typeof(string[]) }, tc, ArgumentsMatchKind.REQUIRES_CONVERSION);

        // Passing (String) on call to (String[]) is exact match
        CheckMatch2(new[] { typeof(string) }, new[] { typeof(string[]) }, tc, ArgumentsMatchKind.EXACT);

        // Passing (Integer,String) on call to (Integer,String[]) is exact match
        CheckMatch2(new[] { typeof(int), typeof(string) }, new[] { typeof(int), typeof(string[]) }, tc, ArgumentsMatchKind.EXACT);

        // Passing (String) on call to (Integer[]) is conversion match (String to Integer)
        CheckMatch2(new[] { typeof(string) }, new[] { typeof(int[]) }, tc, ArgumentsMatchKind.REQUIRES_CONVERSION);

        // Passing (Sub) on call to (Super[]) is close match
        CheckMatch2(new[] { typeof(Sub) }, new[] { typeof(Super[]) }, tc, ArgumentsMatchKind.CLOSE);

        // Passing (Super) on call to (Sub[]) is not a match
        CheckMatch2(new[] { typeof(Super) }, new[] { typeof(Sub[]) }, tc, null);

        CheckMatch2(new[] { typeof(Unconvertable), typeof(string) }, new[] { typeof(Sub), typeof(Super[]) }, tc, null);

        CheckMatch2(new[] { typeof(int), typeof(int), typeof(string) }, new[] { typeof(string), typeof(string), typeof(Super[]) }, tc, null);

        CheckMatch2(new[] { typeof(Unconvertable), typeof(string) }, new[] { typeof(Sub), typeof(Super[]) }, tc, null);

        CheckMatch2(new[] { typeof(int), typeof(int), typeof(string) }, new[] { typeof(string), typeof(string), typeof(Super[]) }, tc, null);

        CheckMatch2(new[] { typeof(int), typeof(int), typeof(Sub) }, new[] { typeof(string), typeof(string), typeof(Super[]) }, tc, ArgumentsMatchKind.REQUIRES_CONVERSION);

        CheckMatch2(new[] { typeof(int), typeof(int), typeof(int) }, new[] { typeof(int), typeof(string[]) }, tc, ArgumentsMatchKind.REQUIRES_CONVERSION);

        // what happens on (Integer,String) passed to (Integer[]) ?
    }

    [Fact]
    public void TestConvertArguments()
    {
        var tc = new StandardTypeConverter();
        var oneArg = typeof(ITestInterface).GetMethod(nameof(ITestInterface.OneArg), new[] { typeof(string) });
        var twoArg = typeof(ITestInterface).GetMethod(nameof(ITestInterface.TwoArg), new[] { typeof(string), typeof(string[]) });

        // basic conversion int>String
        var args = new object[] { 3 };
        ReflectionHelper.ConvertArguments(tc, args, oneArg, null);
        CheckArguments(args, "3");

        // varargs but nothing to convert
        args = new object[] { 3 };
        ReflectionHelper.ConvertArguments(tc, args, twoArg, 1);
        CheckArguments(args, "3");

        // varargs with nothing needing conversion
        args = new object[] { 3, "abc", "abc" };
        ReflectionHelper.ConvertArguments(tc, args, twoArg, 1);
        CheckArguments(args, "3", "abc", "abc");

        // varargs with conversion required
        args = new object[] { 3, false, 3.0d };
        ReflectionHelper.ConvertArguments(tc, args, twoArg, 1);
        CheckArguments(args, "3", "False", "3");
    }

    [Fact]
    public void TestConvertArguments2()
    {
        var tc = new StandardTypeConverter();
        var oneArg = typeof(ITestInterface).GetMethod(nameof(ITestInterface.OneArg), new[] { typeof(string) });
        var twoArg = typeof(ITestInterface).GetMethod(nameof(ITestInterface.TwoArg), new[] { typeof(string), typeof(string[]) });

        // Simple conversion: int to string
        var args = new object[] { 3 };
        ReflectionHelper.ConvertAllArguments(tc, args, oneArg);
        CheckArguments(args, "3");

        // varargs conversion
        args = new object[] { 3, false, 3.0f };
        ReflectionHelper.ConvertAllArguments(tc, args, twoArg);
        CheckArguments(args, "3", "False", "3");

        // varargs conversion but no varargs
        args = new object[] { 3 };
        ReflectionHelper.ConvertAllArguments(tc, args, twoArg);
        CheckArguments(args, "3");

        // null value
        args = new object[] { 3, null, 3.0f };
        ReflectionHelper.ConvertAllArguments(tc, args, twoArg);
        CheckArguments(args, "3", null, "3");
    }

    [Fact]
    public void TestSetupArguments()
    {
        var newArray = ReflectionHelper.SetupArgumentsForVarargsInvocation(new[] { typeof(string[]) }, "a", "b", "c");

        Assert.Single(newArray);
        var firstParam = newArray[0];
        Assert.Equal(typeof(string), firstParam.GetType().GetElementType());
        var firstParamArray = (object[])firstParam;
        Assert.Equal(3, firstParamArray.Length);
        Assert.Equal("a", firstParamArray[0]);
        Assert.Equal("b", firstParamArray[1]);
        Assert.Equal("c", firstParamArray[2]);
    }

    [Fact]
    public void TestReflectivePropertyAccessor()
    {
        var rpa = new ReflectivePropertyAccessor();
        var t = new Tester
        {
            Property = "hello"
        };
        var ctx = new StandardEvaluationContext(t);
        Assert.True(rpa.CanRead(ctx, t, "Property"));
        Assert.Equal("hello", rpa.Read(ctx, t, "Property").Value);

        // cached accessor used
        Assert.Equal("hello", rpa.Read(ctx, t, "Property").Value);

        Assert.True(rpa.CanRead(ctx, t, "Field"));
        Assert.Equal(3, rpa.Read(ctx, t, "Field").Value);

        // cached accessor used
        Assert.Equal(3, rpa.Read(ctx, t, "Field").Value);

        Assert.True(rpa.CanWrite(ctx, t, "Property"));
        rpa.Write(ctx, t, "Property", "goodbye");
        rpa.Write(ctx, t, "Property", "goodbye"); // cached accessor used

        Assert.True(rpa.CanWrite(ctx, t, "Field"));
        rpa.Write(ctx, t, "Field", 12);
        rpa.Write(ctx, t, "Field", 12);

        // Attempted Write as first activity on this field and property to drive testing
        // of populating type descriptor cache
        rpa.Write(ctx, t, "Field2", 3);
        rpa.Write(ctx, t, "Property2", "doodoo");
        Assert.Equal(3, rpa.Read(ctx, t, "Field2").Value);

        // Attempted Read as first activity on this field and property (no CanRead before them)
        Assert.Equal(0, rpa.Read(ctx, t, "Field3").Value);
        Assert.Equal("doodoo", rpa.Read(ctx, t, "Property3").Value);

        // Access through is method
        Assert.Equal(0, rpa.Read(ctx, t, "Field3").Value);
        Assert.False((bool)rpa.Read(ctx, t, "Property4").Value);
        Assert.True(rpa.CanRead(ctx, t, "Property4"));

        // repro SPR-9123, ReflectivePropertyAccessor JavaBean property names compliance tests
        Assert.Equal("iD", rpa.Read(ctx, t, "iD").Value);
        Assert.True(rpa.CanRead(ctx, t, "iD"));
        Assert.Equal("id", rpa.Read(ctx, t, "Id").Value);
        Assert.True(rpa.CanRead(ctx, t, "Id"));
        Assert.Equal("ID", rpa.Read(ctx, t, "ID").Value);
        Assert.True(rpa.CanRead(ctx, t, "ID"));

        // note: "Id" is not a valid JavaBean name, nevertheless it is treated as "id"
        Assert.Equal("id", rpa.Read(ctx, t, "Id").Value);
        Assert.True(rpa.CanRead(ctx, t, "Id"));

        // repro SPR-10994
        Assert.Equal("xyZ", rpa.Read(ctx, t, "XyZ").Value);
        Assert.True(rpa.CanRead(ctx, t, "XyZ"));
        Assert.Equal("xY", rpa.Read(ctx, t, "XY").Value);
        Assert.True(rpa.CanRead(ctx, t, "XY"));

        // SPR-10122, ReflectivePropertyAccessor JavaBean property names compliance tests - setters
        rpa.Write(ctx, t, "pEBS", "Test String");
        Assert.Equal("Test String", rpa.Read(ctx, t, "pEBS").Value);
    }

    [Fact]
    public void TestOptimalReflectivePropertyAccessor()
    {
        var reflective = new ReflectivePropertyAccessor();
        var tester = new Tester
        {
            Property = "hello"
        };
        var ctx = new StandardEvaluationContext(tester);
        Assert.True(reflective.CanRead(ctx, tester, "Property"));
        Assert.Equal("hello", reflective.Read(ctx, tester, "Property").Value);

        // cached accessor used
        Assert.Equal("hello", reflective.Read(ctx, tester, "Property").Value);

        var property = reflective.CreateOptimalAccessor(ctx, tester, "Property");
        Assert.True(property.CanRead(ctx, tester, "Property"));
        Assert.False(property.CanRead(ctx, tester, "Property2"));
        Assert.Throws<InvalidOperationException>(() => property.CanWrite(ctx, tester, "Property"));
        Assert.Throws<InvalidOperationException>(() => property.CanWrite(ctx, tester, "Property2"));
        Assert.Equal("hello", property.Read(ctx, tester, "Property").Value);

        // cached accessor used
        Assert.Equal("hello", property.Read(ctx, tester, "Property").Value);
        Assert.Throws<InvalidOperationException>(() => property.GetSpecificTargetClasses());
        Assert.Throws<InvalidOperationException>(() => property.Write(ctx, tester, "Property", null));

        var field = reflective.CreateOptimalAccessor(ctx, tester, "Field");
        Assert.True(field.CanRead(ctx, tester, "Field"));
        Assert.False(field.CanRead(ctx, tester, "Field2"));
        Assert.Throws<InvalidOperationException>(() => field.CanWrite(ctx, tester, "Field"));
        Assert.Throws<InvalidOperationException>(() => field.CanWrite(ctx, tester, "Field2"));
        Assert.Equal(3, field.Read(ctx, tester, "Field").Value);

        // cached accessor used
        Assert.Equal(3, field.Read(ctx, tester, "Field").Value);
        Assert.Throws<InvalidOperationException>(() => field.GetSpecificTargetClasses());
        Assert.Throws<InvalidOperationException>(() => field.Write(ctx, tester, "field", null));
    }

    private void CheckMatch(Type[] inputTypes, Type[] expectedTypes, StandardTypeConverter typeConverter, ArgumentsMatchKind? expectedMatchKind)
    {
        var matchInfo = ReflectionHelper.CompareArguments(GetTypeDescriptors(expectedTypes), GetTypeDescriptors(inputTypes), typeConverter);
        if (expectedMatchKind == null)
        {
            Assert.Null(matchInfo);
            return;
        }
        else
        {
            Assert.NotNull(matchInfo);
        }

        if (expectedMatchKind.Value == ArgumentsMatchKind.EXACT)
        {
            Assert.True(matchInfo.IsExactMatch);
        }
        else if (expectedMatchKind.Value == ArgumentsMatchKind.CLOSE)
        {
            Assert.True(matchInfo.IsCloseMatch);
        }
        else if (expectedMatchKind.Value == ArgumentsMatchKind.REQUIRES_CONVERSION)
        {
            Assert.True(matchInfo.IsMatchRequiringConversion);
        }
    }

    private void CheckMatch2(Type[] inputTypes, Type[] expectedTypes, StandardTypeConverter typeConverter, ArgumentsMatchKind? expectedMatchKind)
    {
        var matchInfo = ReflectionHelper.CompareArgumentsVarargs(GetTypeDescriptors(expectedTypes), GetTypeDescriptors(inputTypes), typeConverter);
        if (expectedMatchKind == null)
        {
            Assert.Null(matchInfo);
            return;
        }
        else
        {
            Assert.NotNull(matchInfo);
        }

        if (expectedMatchKind.Value == ArgumentsMatchKind.EXACT)
        {
            Assert.True(matchInfo.IsExactMatch);
        }
        else if (expectedMatchKind.Value == ArgumentsMatchKind.CLOSE)
        {
            Assert.True(matchInfo.IsCloseMatch);
        }
        else if (expectedMatchKind.Value == ArgumentsMatchKind.REQUIRES_CONVERSION)
        {
            Assert.True(matchInfo.IsMatchRequiringConversion);
        }
    }

    private void CheckArguments(object[] args, params object[] expected)
    {
        Assert.Equal(expected.Length, args.Length);
        for (var i = 0; i < expected.Length; i++)
        {
            CheckArgument(expected[i], args[i]);
        }
    }

    private void CheckArgument(object expected, object actual)
    {
        Assert.Equal(expected, actual);
    }

    private List<Type> GetTypeDescriptors(params Type[] types)
    {
        var typeDescriptors = new List<Type>(types.Length);
        foreach (var type in types)
        {
            typeDescriptors.Add(type);
        }

        return typeDescriptors;
    }

    public interface ITestInterface
    {
        void OneArg(string arg1);

        void TwoArg(string arg1, params string[] arg2);
    }

    public class Super
    {
    }

    public class Sub : Super
    {
    }

    public class Unconvertable
    {
    }

    public class Tester
    {
        public int Field = 3;
        public int Field2;
        public int Field3;

        public string Property { get; set; }

        public string Property2 { private get; set; }

        public string Property3 { get; private set; } = "doodoo";

        public bool Property4 { get; }

#pragma warning disable SA1300 // Element should begin with upper-case letter
        public string iD { get; } = "iD";
#pragma warning restore SA1300 // Element should begin with upper-case letter

        public string Id { get; } = "id";

        public string ID { get; } = "ID";

        public string XY { get; } = "xY";

        public string XyZ { get; } = "xyZ";

#pragma warning disable SA1300 // Element should begin with upper-case letter
        public string pEBS { get; set; } = "pEBS";
#pragma warning restore SA1300 // Element should begin with upper-case letter
    }
}
