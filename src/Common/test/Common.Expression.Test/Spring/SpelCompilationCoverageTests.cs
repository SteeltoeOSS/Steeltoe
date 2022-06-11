// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Ast;
using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using Steeltoe.Common.Expression.Spring.TestData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Spring;

public class SpelCompilationCoverageTests : AbstractExpressionTests
{
    private IExpression _expression;

    public static string Concat(string a, string b)
    {
        return a + b;
    }

    public static string Join(params string[] strings)
    {
        var buf = new StringBuilder();
        foreach (var stringin in strings)
        {
            buf.Append(stringin);
        }

        return buf.ToString();
    }

    [Fact]
    public void TypeReference()
    {
        _expression = Parse("T(String)");
        Assert.Equal(typeof(string), _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(typeof(string), _expression.GetValue());

        _expression = Parse("T(System.IO.IOException)");
        Assert.Equal(typeof(System.IO.IOException), _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(typeof(System.IO.IOException), _expression.GetValue());

        _expression = Parse("T(System.IO.IOException[])");
        Assert.Equal(typeof(System.IO.IOException[]), _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(typeof(System.IO.IOException[]), _expression.GetValue());

        _expression = Parse("T(int[][])");
        Assert.Equal(typeof(int[][]), _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(typeof(int[][]), _expression.GetValue());

        _expression = Parse("T(int)");
        Assert.Equal(typeof(int), _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(typeof(int), _expression.GetValue());

        _expression = Parse("T(byte)");
        Assert.Equal(typeof(byte), _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(typeof(byte), _expression.GetValue());

        _expression = Parse("T(char)");
        Assert.Equal(typeof(char), _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(typeof(char), _expression.GetValue());

        _expression = Parse("T(short)");
        Assert.Equal(typeof(short), _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(typeof(short), _expression.GetValue());

        _expression = Parse("T(long)");
        Assert.Equal(typeof(long), _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(typeof(long), _expression.GetValue());

        _expression = Parse("T(float)");
        Assert.Equal(typeof(float), _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(typeof(float), _expression.GetValue());

        _expression = Parse("T(double)");
        Assert.Equal(typeof(double), _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(typeof(double), _expression.GetValue());

        _expression = Parse("T(boolean)");
        Assert.Equal(typeof(bool), _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(typeof(bool), _expression.GetValue());

        _expression = Parse("T(Missing)");
        AssertGetValueFail(_expression);
        AssertCantCompile(_expression);
    }

    [Fact]
    public void OperatorInstanceOf()
    {
        _expression = Parse("'xyz' instanceof T(String)");
        Assert.True((bool)_expression.GetValue());
        AssertCanCompile(_expression);
        Assert.True((bool)_expression.GetValue());

        _expression = Parse("'xyz' instanceof T(int)");
        Assert.False((bool)_expression.GetValue());
        AssertCanCompile(_expression);
        Assert.False((bool)_expression.GetValue());

        var list = new ArrayList();
        _expression = Parse("#root instanceof T(System.Collections.IList)");
        Assert.True((bool)_expression.GetValue(list));
        AssertCanCompile(_expression);
        Assert.True((bool)_expression.GetValue(list));

        var arrayOfLists = new IList[] { new ArrayList() };
        _expression = Parse("#root instanceof T(System.Collections.IList[])");
        Assert.True((bool)_expression.GetValue(arrayOfLists));
        AssertCanCompile(_expression);
        Assert.True((bool)_expression.GetValue(arrayOfLists));

        var intArray = new[] { 1, 2, 3 };
        _expression = Parse("#root instanceof T(int[])");
        Assert.True((bool)_expression.GetValue(intArray));
        AssertCanCompile(_expression);
        Assert.True((bool)_expression.GetValue(intArray));

        const string root1 = null;
        _expression = Parse("#root instanceof T(System.Int32)");
        Assert.False((bool)_expression.GetValue(root1));
        AssertCanCompile(_expression);
        Assert.False((bool)_expression.GetValue(root1));

        // root still null
        _expression = Parse("#root instanceof T(System.Object)");
        Assert.False((bool)_expression.GetValue(root1));
        AssertCanCompile(_expression);
        Assert.False((bool)_expression.GetValue(root1));

        var root2 = "howdy";
        _expression = Parse("#root instanceof T(System.Object)");
        Assert.True((bool)_expression.GetValue(root2));
        AssertCanCompile(_expression);
        Assert.True((bool)_expression.GetValue(root2));
    }

    [Fact]
    public void OperatorInstanceOf_SPR14250()
    {
        // primitive left operand - should get boxed, return true
        _expression = Parse("3 instanceof T(System.Int32)");
        Assert.True((bool)_expression.GetValue());
        AssertCanCompile(_expression);
        Assert.True((bool)_expression.GetValue());

        // primitive left operand - should get boxed, return false
        _expression = Parse("3 instanceof T(String)");
        Assert.False((bool)_expression.GetValue());
        AssertCanCompile(_expression);
        Assert.False((bool)_expression.GetValue());

        // primitive left operand - should get boxed, return false
        _expression = Parse("3.0d instanceof T(System.Int32)");
        Assert.False((bool)_expression.GetValue());
        AssertCanCompile(_expression);
        Assert.False((bool)_expression.GetValue());

        // primitive left operand - should get boxed, return false
        _expression = Parse("3.0d instanceof T(System.Double)");
        Assert.True((bool)_expression.GetValue());
        AssertCanCompile(_expression);
        Assert.True((bool)_expression.GetValue());

        // Only when the right hand operand is a direct type reference
        // will it be compilable.
        var ctx = new StandardEvaluationContext();
        ctx.SetVariable("foo", typeof(string));
        _expression = Parse("3 instanceof #foo");
        Assert.False((bool)_expression.GetValue(ctx));
        AssertCantCompile(_expression);

        // use of primitive as type for instanceof check - compilable
        // but always false
        _expression = Parse("3 instanceof T(int)");
        Assert.True((bool)_expression.GetValue());
        AssertCanCompile(_expression);
        Assert.True((bool)_expression.GetValue());

        _expression = Parse("3 instanceof T(long)");
        Assert.False((bool)_expression.GetValue());
        AssertCanCompile(_expression);
        Assert.False((bool)_expression.GetValue());
    }

    [Fact]
    public void StringLiteral()
    {
        _expression = _parser.ParseExpression("'abcde'");
        Assert.Equal("abcde", _expression.GetValue<string>(new TestClass1()));
        AssertCanCompile(_expression);
        var resultC = _expression.GetValue<string>(new TestClass1());
        Assert.Equal("abcde", resultC);
        Assert.Equal("abcde", _expression.GetValue<string>());
        Assert.Equal("abcde", _expression.GetValue());
        Assert.Equal("abcde", _expression.GetValue(new StandardEvaluationContext()));
        _expression = _parser.ParseExpression("\"abcde\"");
        AssertCanCompile(_expression);
        Assert.Equal("abcde", _expression.GetValue<string>());
    }

    [Fact]
    public void NullLiteral()
    {
        _expression = _parser.ParseExpression("null");
        var resultI = _expression.GetValue<object>(new TestClass1());
        AssertCanCompile(_expression);
        var resultC = _expression.GetValue<object>(new TestClass1());
        Assert.Null(resultI);
        Assert.Null(resultC);
    }

    [Fact]
    public void RealLiteral()
    {
        _expression = _parser.ParseExpression("3.4d");
        var resultI = _expression.GetValue<double>(new TestClass1());
        AssertCanCompile(_expression);
        var resultC = _expression.GetValue<double>(new TestClass1());
        Assert.Equal(3.4d, resultI);
        Assert.Equal(3.4d, resultC);
    }

    [Fact]
    public void InlineList()
    {
        _expression = _parser.ParseExpression("'abcde'.Substring({1,3,4}[0])");
        var o = _expression.GetValue();
        Assert.Equal("bcde", o);
        AssertCanCompile(_expression);
        o = _expression.GetValue();
        Assert.Equal("bcde", o);

        _expression = _parser.ParseExpression("{'abc','def'}");
        var l = _expression.GetValue<IList>();
        Assert.Equal(2, l.Count);
        Assert.Equal("abc", l[0]);
        Assert.Equal("def", l[1]);
        AssertCanCompile(_expression);
        l = _expression.GetValue<IList>();
        Assert.Equal(2, l.Count);
        Assert.Equal("abc", l[0]);
        Assert.Equal("def", l[1]);

        _expression = _parser.ParseExpression("{'abc','def'}[0]");
        o = _expression.GetValue();
        Assert.Equal("abc", o);
        AssertCanCompile(_expression);
        o = _expression.GetValue();
        Assert.Equal("abc", o);

        _expression = _parser.ParseExpression("{'abcde','ijklm'}[0].Substring({1,3,4}[0])");
        o = _expression.GetValue();
        Assert.Equal("bcde", o);
        AssertCanCompile(_expression);
        o = _expression.GetValue();
        Assert.Equal("bcde", o);

        _expression = _parser.ParseExpression("{'abcde','ijklm'}[0].Substring({1,3,4}[0],{1,3,4}[1])");
        o = _expression.GetValue();
        Assert.Equal("bcd", o);
        AssertCanCompile(_expression);
        o = _expression.GetValue();
        Assert.Equal("bcd", o);
    }

    [Fact]
    public void NestedInlineLists()
    {
        _expression = _parser.ParseExpression("{{1,2,3},{4,5,6},{7,8,9}}");
        var o = _expression.GetValue<IList>();
        Assert.Equal(3, o.Count);

        var o1 = o[0] as IList;
        Assert.Equal(3, o1.Count);
        Assert.Equal(3, o1[2]);

        var o2 = o[1] as IList;
        Assert.Equal(3, o2.Count);
        Assert.Equal(6, o2[2]);

        var o3 = o[2] as IList;
        Assert.Equal(3, o3.Count);
        Assert.Equal(9, o3[2]);

        AssertCanCompile(_expression);
        o = _expression.GetValue<IList>();
        Assert.Equal(3, o.Count);

        o1 = o[0] as IList;
        Assert.Equal(3, o1.Count);
        Assert.Equal(3, o1[2]);

        o2 = o[1] as IList;
        Assert.Equal(3, o2.Count);
        Assert.Equal(6, o2[2]);

        o3 = o[2] as IList;
        Assert.Equal(3, o3.Count);
        Assert.Equal(9, o3[2]);

        _expression = _parser.ParseExpression("{{1,2,3},{4,5,6},{7,8,9}}.Count");
        var c = _expression.GetValue<int>();
        Assert.Equal(3, c);
        AssertCanCompile(_expression);
        Assert.Equal(3, c);

        _expression = _parser.ParseExpression("{{1,2,3},{4,5,6},{7,8,9}}[1][0]");
        var n = _expression.GetValue<int>();
        Assert.Equal(4, n);
        AssertCanCompile(_expression);
        Assert.Equal(4, n);

        _expression = _parser.ParseExpression("{{1,2,3},'abc',{7,8,9}}[1]");
        var obj = _expression.GetValue();
        Assert.Equal("abc", obj);
        AssertCanCompile(_expression);
        Assert.Equal("abc", obj);

        _expression = _parser.ParseExpression("'abcde'.Substring({{1,3},1,3,4}[0][1])");
        obj = _expression.GetValue();
        Assert.Equal("de", obj);
        AssertCanCompile(_expression);
        Assert.Equal("de", obj);

        _expression = _parser.ParseExpression("'abcde'.Substring({{1,3},1,3,4}[1])");
        obj = _expression.GetValue();
        Assert.Equal("bcde", obj);
        AssertCanCompile(_expression);
        Assert.Equal("bcde", obj);

        _expression = _parser.ParseExpression("{'abc',{'def','ghi'}}");
        var l = _expression.GetValue<IList>();
        Assert.Equal(2, l.Count);
        AssertCanCompile(_expression);
        l = _expression.GetValue<IList>();
        Assert.Equal(2, l.Count);

        _expression = _parser.ParseExpression("{'abcde',{'ijklm','nopqr'}}[0].Substring({1,3,4}[0])");
        obj = _expression.GetValue();
        Assert.Equal("bcde", obj);
        AssertCanCompile(_expression);
        Assert.Equal("bcde", obj);

        _expression = _parser.ParseExpression("{'abcde',{'ijklm','nopqr'}}[1][0].Substring({1,3,4}[0])");
        obj = _expression.GetValue();
        Assert.Equal("jklm", obj);
        AssertCanCompile(_expression);
        Assert.Equal("jklm", obj);

        _expression = _parser.ParseExpression("{'abcde',{'ijklm','nopqr'}}[1][1].Substring({1,3,4}[0],{1,3,4}[1])");
        obj = _expression.GetValue();
        Assert.Equal("opq", obj);
        AssertCanCompile(_expression);
        Assert.Equal("opq", obj);
    }

    [Fact]
    public void IntLiteral()
    {
        _expression = _parser.ParseExpression("42");
        var resultI = _expression.GetValue<int>(new TestClass1());
        AssertCanCompile(_expression);
        var resultC = _expression.GetValue<int>(new TestClass1());
        Assert.Equal(42, resultI);
        Assert.Equal(42, resultC);

        _expression = _parser.ParseExpression("0");
        AssertCanCompile(_expression);
        Assert.Equal(0, _expression.GetValue<int>());
        _expression = _parser.ParseExpression("2");
        AssertCanCompile(_expression);
        Assert.Equal(2, _expression.GetValue<int>());
        _expression = _parser.ParseExpression("7");
        AssertCanCompile(_expression);
        Assert.Equal(7, _expression.GetValue<int>());
    }

    [Fact]
    public void LongLiteral()
    {
        _expression = _parser.ParseExpression("99L");
        var resultI = _expression.GetValue<long>(new TestClass1());
        AssertCanCompile(_expression);
        var resultC = _expression.GetValue<long>(new TestClass1());
        Assert.Equal(99L, resultI);
        Assert.Equal(99L, resultC);
    }

    [Fact]
    public void BooleanLiteral()
    {
        _expression = _parser.ParseExpression("True");
        var resultI = _expression.GetValue<bool>(new TestClass1());
        Assert.True(resultI);
        AssertCanCompile(_expression);
        var resultC = _expression.GetValue<bool>(new TestClass1());
        Assert.True(resultC);

        _expression = _parser.ParseExpression("False");
        resultI = _expression.GetValue<bool>(new TestClass1());
        Assert.False(resultI);
        AssertCanCompile(_expression);
        resultC = _expression.GetValue<bool>(new TestClass1());
        Assert.False(resultC);
    }

    [Fact]
    public void FloatLiteral()
    {
        _expression = _parser.ParseExpression("3.4f");
        var resultI = _expression.GetValue<double>(new TestClass1());
        AssertCanCompile(_expression);
        var resultC = _expression.GetValue<double>(new TestClass1());
        Assert.Equal(3.4f, resultI);
        Assert.Equal(3.4f, resultC);
    }

    [Fact]
    public void OpOr()
    {
        var expression = _parser.ParseExpression("False or False");
        var resultI = expression.GetValue<bool>(1);
        SpelCompiler.Compile(expression);
        var resultC = expression.GetValue<bool>(1);
        Assert.False(resultI);
        Assert.False(resultC);

        expression = _parser.ParseExpression("False or True");
        resultI = expression.GetValue<bool>(1);
        SpelCompiler.Compile(expression);
        resultC = expression.GetValue<bool>(1);
        Assert.True(resultI);
        Assert.True(resultC);

        expression = _parser.ParseExpression("True or False");
        resultI = expression.GetValue<bool>(1);
        SpelCompiler.Compile(expression);
        resultC = expression.GetValue<bool>(1);
        Assert.True(resultI);
        Assert.True(resultC);

        expression = _parser.ParseExpression("True or True");
        resultI = expression.GetValue<bool>(1);
        SpelCompiler.Compile(expression);
        resultC = expression.GetValue<bool>(1);
        Assert.True(resultI);
        Assert.True(resultC);

        var tc = new TestClass4();
        expression = _parser.ParseExpression("GetFalse() or GetTrue()");
        resultI = expression.GetValue<bool>(tc);
        SpelCompiler.Compile(expression);
        resultC = expression.GetValue<bool>(tc);
        Assert.True(resultI);
        Assert.True(resultC);

        // Can't compile this as we aren't going down the getfalse() branch in our evaluation
        expression = _parser.ParseExpression("GetTrue() or GetFalse()");
        resultI = expression.GetValue<bool>(tc);
        AssertCantCompile(expression);

        expression = _parser.ParseExpression("A or B");
        tc.A = true;
        tc.B = true;
        resultI = expression.GetValue<bool>(tc);
        AssertCantCompile(expression); // Haven't yet been into second branch
        tc.A = false;
        tc.B = true;
        resultI = expression.GetValue<bool>(tc);
        AssertCanCompile(expression); // Now been down both
        Assert.True(resultI);

        var b = false;
        expression = Parse("#root or #root");
        var resultI2 = expression.GetValue<bool>(b);
        AssertCanCompile(expression);
        Assert.False(resultI2);
        Assert.False(expression.GetValue<bool>(b));
    }

    [Fact]
    public void OpAnd()
    {
        var expression = _parser.ParseExpression("False and False");
        var resultI = expression.GetValue<bool>(1);
        SpelCompiler.Compile(expression);
        var resultC = expression.GetValue<bool>(1);
        Assert.False(resultI);
        Assert.False(resultC);

        expression = _parser.ParseExpression("False and True");
        resultI = expression.GetValue<bool>(1);
        SpelCompiler.Compile(expression);
        resultC = expression.GetValue<bool>(1);
        Assert.False(resultI);
        Assert.False(resultC);

        expression = _parser.ParseExpression("True and False");
        resultI = expression.GetValue<bool>(1);
        SpelCompiler.Compile(expression);
        resultC = expression.GetValue<bool>(1);
        Assert.False(resultI);
        Assert.False(resultC);

        expression = _parser.ParseExpression("True and True");
        resultI = expression.GetValue<bool>(1);
        SpelCompiler.Compile(expression);
        resultC = expression.GetValue<bool>(1);
        Assert.True(resultI);
        Assert.True(resultC);

        var tc = new TestClass4();

        // Can't compile this as we aren't going down the gettrue() branch in our evaluation
        expression = _parser.ParseExpression("GetFalse() and GetTrue()");
        resultI = expression.GetValue<bool>(tc);
        AssertCantCompile(expression);
        Assert.False(resultI);

        expression = _parser.ParseExpression("A and B");
        tc.A = false;
        tc.B = false;
        resultI = expression.GetValue<bool>(tc);
        AssertCantCompile(expression); // Haven't yet been into second branch
        tc.A = true;
        tc.B = false;
        resultI = expression.GetValue<bool>(tc);
        AssertCanCompile(expression); // Now been down both
        Assert.False(resultI);
        tc.A = true;
        tc.B = true;
        resultI = expression.GetValue<bool>(tc);
        Assert.True(resultI);

        var b = true;
        expression = Parse("#root and #root");
        var resultI2 = expression.GetValue<bool>(b);
        AssertCanCompile(expression);
        Assert.True(resultI2);
        Assert.True(expression.GetValue<bool>(b));
    }

    [Fact]
    public void OperatorNot()
    {
        _expression = _parser.ParseExpression("!True");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = _parser.ParseExpression("!False");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        var b = true;
        _expression = _parser.ParseExpression("!#root");
        Assert.False(_expression.GetValue<bool>(b));
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>(b));

        b = false;
        _expression = _parser.ParseExpression("!#root");
        Assert.True(_expression.GetValue<bool>(b));
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>(b));
    }

    [Fact]
    public void Ternary()
    {
        var expression = _parser.ParseExpression("True?'a':'b'");
        var resultI = expression.GetValue<string>();
        AssertCanCompile(expression);
        var resultC = expression.GetValue<string>();
        Assert.Equal("a", resultI);
        Assert.Equal("a", resultC);

        expression = _parser.ParseExpression("False?'a':'b'");
        resultI = expression.GetValue<string>();
        AssertCanCompile(expression);
        resultC = expression.GetValue<string>();
        Assert.Equal("b", resultI);
        Assert.Equal("b", resultC);

        expression = _parser.ParseExpression("False?1:'b'");
        AssertCanCompile(expression);
        Assert.Equal("b", expression.GetValue<string>());

        var root = true;
        expression = _parser.ParseExpression("(#root and True)?T(int).Parse('1'):T(long).Parse('3')");
        Assert.Equal(1, expression.GetValue(root));
        AssertCantCompile(expression); // Have not gone down false branch
        root = false;
        Assert.Equal(3L, expression.GetValue(root));
        AssertCanCompile(expression);
        Assert.Equal(3L, expression.GetValue(root));
        root = true;
        Assert.Equal(1, expression.GetValue(root));
    }

    [Fact]
    public void TernaryWithBooleanReturn_SPR12271()
    {
        var expression = _parser.ParseExpression("T(Boolean).Parse('True')?'abc':'def'");
        Assert.Equal("abc", expression.GetValue<string>());
        AssertCanCompile(expression);
        Assert.Equal("abc", expression.GetValue<string>());

        expression = _parser.ParseExpression("T(Boolean).Parse('False')?'abc':'def'");
        Assert.Equal("def", expression.GetValue<string>());
        AssertCanCompile(expression);
        Assert.Equal("def", expression.GetValue<string>());
    }

    [Fact]
    public void NullSafeFieldPropertyDereferencing_SPR16489()
    {
        var foh = new FooObjectHolder();
        var context = new StandardEvaluationContext(foh);

        var expression = (SpelExpression)_parser.ParseExpression("Foo?.Object");
        Assert.Equal("hello", expression.GetValue(context));
        foh.Foo = null;
        Assert.Null(expression.GetValue(context));

        foh.Foo = new FooObject();
        Assert.Equal("hello", expression.GetValue(context));
        AssertCanCompile(expression);
        Assert.Equal("hello", expression.GetValue(context));
        foh.Foo = null;
        Assert.Null(expression.GetValue(context));

        // Static references
        expression = (SpelExpression)_parser.ParseExpression("#var?.PropertyA");
        context.SetVariable("var", typeof(StaticsHelper));
        Assert.Equal("sh", expression.GetValue(context).ToString());
        context.SetVariable("var", null);
        Assert.Null(expression.GetValue());
        AssertCanCompile(expression);
        context.SetVariable("var", typeof(StaticsHelper));
        Assert.Equal("sh", expression.GetValue(context).ToString());
        context.SetVariable("var", null);
        Assert.Null(expression.GetValue());

        // Single size primitive (boolean)
        expression = (SpelExpression)_parser.ParseExpression("#var?.A");
        context.SetVariable("var", new TestClass4());
        Assert.False(expression.GetValue<bool>(context));
        context.SetVariable("var", null);
        Assert.Null(expression.GetValue());
        AssertCanCompile(expression);
        context.SetVariable("var", new TestClass4());
        Assert.False(expression.GetValue<bool>(context));
        context.SetVariable("var", null);
        Assert.Null(expression.GetValue());

        // Double slot primitives
        expression = (SpelExpression)_parser.ParseExpression("#var?.Four");
        context.SetVariable("var", new Three());
        Assert.Equal(0.04d, expression.GetValue<double>(context));
        context.SetVariable("var", null);
        Assert.Null(expression.GetValue());
        AssertCanCompile(expression);
        context.SetVariable("var", new Three());
        Assert.Equal(0.04d, expression.GetValue<double>(context));
        context.SetVariable("var", null);
        Assert.Null(expression.GetValue());

        expression = (SpelExpression)_parser.ParseExpression("#var?.Day");
        context.SetVariable("var", DateTime.Now);
        Assert.InRange(expression.GetValue<int>(context), 1, 31);
        context.SetVariable("var", null);
        Assert.Null(expression.GetValue());
        AssertCanCompile(expression);
        context.SetVariable("var", DateTime.Now);
        Assert.InRange(expression.GetValue<int>(context), 1, 31);
        context.SetVariable("var", null);
        Assert.Null(expression.GetValue());
    }

    [Fact]
    public void NullSafeMethodChaining_SPR16489()
    {
        var foh = new FooObjectHolder();
        var context = new StandardEvaluationContext(foh);

        // First non compiled:
        var expression = (SpelExpression)_parser.ParseExpression("Foo?.GetObject()");
        Assert.Equal("hello", expression.GetValue(context));
        foh.Foo = null;
        Assert.Null(expression.GetValue(context));
        AssertCanCompile(expression);
        foh.Foo = new FooObject();
        Assert.Equal("hello", expression.GetValue(context));
        foh.Foo = null;
        Assert.Null(expression.GetValue(context));

        // Static method references
        expression = (SpelExpression)_parser.ParseExpression("#var?.MethodA()");
        context.SetVariable("var", typeof(StaticsHelper));
        Assert.Equal("sh", expression.GetValue(context).ToString());
        context.SetVariable("var", null);
        Assert.Null(expression.GetValue());
        AssertCanCompile(expression);
        context.SetVariable("var", typeof(StaticsHelper));
        Assert.Equal("sh", expression.GetValue(context).ToString());
        context.SetVariable("var", null);
        Assert.Null(expression.GetValue());

        // Nullsafe guard on expression element evaluating to primitive/null
        expression = (SpelExpression)_parser.ParseExpression("#var?.ToString()");
        context.SetVariable("var", 4);
        Assert.Equal("4", expression.GetValue(context));
        context.SetVariable("var", null);
        Assert.Null(expression.GetValue());
        AssertCanCompile(expression);
        context.SetVariable("var", 4);
        Assert.Equal("4", expression.GetValue(context));
        context.SetVariable("var", null);
        Assert.Null(expression.GetValue());

        // Nullsafe guard on expression element evaluating to primitive/null
        expression = (SpelExpression)_parser.ParseExpression("#var?.ToString()");
        context.SetVariable("var", false);
        Assert.Equal("False", expression.GetValue(context));
        context.SetVariable("var", null);
        Assert.Null(expression.GetValue());
        AssertCanCompile(expression);
        context.SetVariable("var", false);
        Assert.Equal("False", expression.GetValue(context));
        context.SetVariable("var", null);
        Assert.Null(expression.GetValue());

        // Nullsafe guard on expression element evaluating to primitive/null
        expression = (SpelExpression)_parser.ParseExpression("#var?.ToString()");
        context.SetVariable("var", 5L);
        Assert.Equal("5", expression.GetValue(context));
        context.SetVariable("var", null);
        Assert.Null(expression.GetValue());
        AssertCanCompile(expression);
        context.SetVariable("var", 5L);
        Assert.Equal("5", expression.GetValue(context));
        context.SetVariable("var", null);
        Assert.Null(expression.GetValue());

        // Nullsafe guard on expression element evaluating to primitive/null
        expression = (SpelExpression)_parser.ParseExpression("#var?.ToString()");
        context.SetVariable("var", (short)10);
        Assert.Equal("10", expression.GetValue(context));
        context.SetVariable("var", null);
        Assert.Null(expression.GetValue());
        AssertCanCompile(expression);
        context.SetVariable("var", (short)10);
        Assert.Equal("10", expression.GetValue(context));
        context.SetVariable("var", null);
        Assert.Null(expression.GetValue());

        // Nullsafe guard on expression element evaluating to primitive/null
        expression = (SpelExpression)_parser.ParseExpression("#var?.ToString()");
        context.SetVariable("var", 10.0f);
        Assert.Equal("10", expression.GetValue(context));
        context.SetVariable("var", null);
        Assert.Null(expression.GetValue());
        AssertCanCompile(expression);
        context.SetVariable("var", 10.0f);
        Assert.Equal("10", expression.GetValue(context));
        context.SetVariable("var", null);
        Assert.Null(expression.GetValue());

        // Nullsafe guard on expression element evaluating to primitive/null
        expression = (SpelExpression)_parser.ParseExpression("#var?.ToString()");
        context.SetVariable("var", 10.0d);
        Assert.Equal("10", expression.GetValue(context));
        context.SetVariable("var", null);
        Assert.Null(expression.GetValue());
        AssertCanCompile(expression);
        context.SetVariable("var", 10.0d);
        Assert.Equal("10", expression.GetValue(context));
        context.SetVariable("var", null);
        Assert.Null(expression.GetValue());
    }

    [Fact]
    public void Elvis()
    {
        var expression = _parser.ParseExpression("'a'?:'b'");
        var resultI = expression.GetValue<string>();
        AssertCanCompile(expression);
        var resultC = expression.GetValue<string>();
        Assert.Equal("a", resultI);
        Assert.Equal("a", resultC);

        expression = _parser.ParseExpression("null?:'a'");
        resultI = expression.GetValue<string>();
        AssertCanCompile(expression);
        resultC = expression.GetValue<string>();
        Assert.Equal("a", resultI);
        Assert.Equal("a", resultC);

        var s = "abc";
        expression = _parser.ParseExpression("#root?:'b'");
        AssertCantCompile(expression);
        resultI = expression.GetValue<string>(s);
        Assert.Equal("abc", resultI);
        AssertCanCompile(expression);
        resultC = expression.GetValue<string>(s);
        Assert.Equal("abc", resultC);
    }

    [Fact]
    public void VariableReference_Root()
    {
        var s = "hello";
        var expression = _parser.ParseExpression("#root");
        var resultI = expression.GetValue<string>(s);
        AssertCanCompile(expression);
        var resultC = expression.GetValue<string>(s);
        Assert.Equal("hello", resultI);
        Assert.Equal("hello", resultC);

        expression = _parser.ParseExpression("#root");
        var i = expression.GetValue<int>(42);
        Assert.Equal(42, i);
        AssertCanCompile(expression);
        i = expression.GetValue<int>(42);
        Assert.Equal(42, i);
    }

    [Fact]
    public void CompiledExpressionShouldWorkWhenUsingCustomFunctionWithVarargs()
    {
        StandardEvaluationContext context = null;

        // Here the target method takes Object... and we are passing a string
        _expression = _parser.ParseExpression("#DoFormat('hey {0}', 'there')");
        context = new StandardEvaluationContext();
        context.RegisterFunction("DoFormat", typeof(DelegatingStringFormat).GetMethod("Format", new[] { typeof(string), typeof(object[]) }));
        ((SpelExpression)_expression).EvaluationContext = context;

        Assert.Equal("hey there", _expression.GetValue<string>());
        Assert.True(((SpelNode)((SpelExpression)_expression).AST).IsCompilable());
        AssertCanCompile(_expression);
        Assert.Equal("hey there", _expression.GetValue<string>());

        _expression = _parser.ParseExpression("#DoFormat([0], 'there')");
        context = new StandardEvaluationContext(new object[] { "hey {0}" });
        context.RegisterFunction("DoFormat", typeof(DelegatingStringFormat).GetMethod("Format", new[] { typeof(string), typeof(object[]) }));
        ((SpelExpression)_expression).EvaluationContext = context;

        Assert.Equal("hey there", _expression.GetValue<string>());
        Assert.True(((SpelNode)((SpelExpression)_expression).AST).IsCompilable());
        AssertCanCompile(_expression);
        Assert.Equal("hey there", _expression.GetValue<string>());

        _expression = _parser.ParseExpression("#DoFormat([0], #arg)");
        context = new StandardEvaluationContext(new object[] { "hey {0}" });
        context.RegisterFunction("DoFormat", typeof(DelegatingStringFormat).GetMethod("Format", new[] { typeof(string), typeof(object[]) }));
        context.SetVariable("arg", "there");
        ((SpelExpression)_expression).EvaluationContext = context;

        Assert.Equal("hey there", _expression.GetValue<string>());
        Assert.True(((SpelNode)((SpelExpression)_expression).AST).IsCompilable());
        AssertCanCompile(_expression);
        Assert.Equal("hey there", _expression.GetValue<string>());
    }

    [Fact]
    public void FunctionReference()
    {
        var ctx = new StandardEvaluationContext();
        var m = GetType().GetMethod("Concat", new[] { typeof(string), typeof(string) });
        ctx.SetVariable("Concat", m);

        _expression = _parser.ParseExpression("#Concat('a','b')");
        Assert.Equal("ab", _expression.GetValue(ctx));
        AssertCanCompile(_expression);
        Assert.Equal("ab", _expression.GetValue(ctx));

        _expression = _parser.ParseExpression("#Concat(#Concat('a','b'),'c').get_Chars(1)");
        Assert.Equal('b', _expression.GetValue(ctx));
        AssertCanCompile(_expression);
        Assert.Equal('b', _expression.GetValue(ctx));

        _expression = _parser.ParseExpression("#Concat(#a,#b)");
        ctx.SetVariable("a", "foo");
        ctx.SetVariable("b", "bar");
        Assert.Equal("foobar", _expression.GetValue(ctx));
        AssertCanCompile(_expression);
        Assert.Equal("foobar", _expression.GetValue(ctx));
        ctx.SetVariable("b", "boo");
        Assert.Equal("fooboo", _expression.GetValue(ctx));

        m = typeof(Math).GetMethod("Pow", new[] { typeof(double), typeof(double) });
        ctx.SetVariable("kapow", m);
        _expression = _parser.ParseExpression("#kapow(2.0d,2.0d)");
        Assert.Equal(4.0d, _expression.GetValue(ctx));
        AssertCanCompile(_expression);
        Assert.Equal(4.0d, _expression.GetValue(ctx));
    }

    [Fact]
    public void FunctionReferenceVisibility_SPR12359()
    {
        // Confirms visibility of what is being called.
        var context = new StandardEvaluationContext(new object[] { "1" });
        var m = typeof(SomeCompareMethod).GetMethod("Compare", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(object), typeof(object) }, null);
        context.RegisterFunction("doCompare", m);
        context.SetVariable("arg", "2");

        // type nor method are public
        _expression = _parser.ParseExpression("#doCompare([0],#arg)");
        Assert.Equal("-1", _expression.GetValue(context).ToString());
        AssertCantCompile(_expression);

        // type not public but method is
        context = new StandardEvaluationContext(new object[] { "1" });
        m = typeof(SomeCompareMethod).GetMethod("Compare2", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(object), typeof(object) }, null);
        context.RegisterFunction("doCompare", m);
        context.SetVariable("arg", "2");
        _expression = _parser.ParseExpression("#doCompare([0],#arg)");
        Assert.Equal("-1", _expression.GetValue(context).ToString());
        AssertCantCompile(_expression);
    }

    [Fact]
    public void FunctionReferenceNonCompilableArguments_SPR12359()
    {
        var context = new StandardEvaluationContext(new object[] { "1" });
        var m = typeof(SomeCompareMethod2).GetMethod("Negate", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(int) }, null);
        context.RegisterFunction("negate", m);
        context.SetVariable("arg", "2");

        var ints = new[] { 1, 2, 3 };
        context.SetVariable("ints", ints);

        _expression = _parser.ParseExpression("#negate(#ints.?[#this<2][0])");
        Assert.Equal("-1", _expression.GetValue(context).ToString());

        // Selection isn't compilable.
        Assert.False(((SpelNode)((SpelExpression)_expression).AST).IsCompilable());
    }

    [Fact]
    public void FunctionReferenceVarargs_SPR12359()
    {
        var context = new StandardEvaluationContext();
        context.RegisterFunction("append", typeof(SomeCompareMethod2).GetMethod("Append", new[] { typeof(string[]) }));
        context.RegisterFunction("append2", typeof(SomeCompareMethod2).GetMethod("Append2", new[] { typeof(object[]) }));
        context.RegisterFunction("append3", typeof(SomeCompareMethod2).GetMethod("Append3", new[] { typeof(string[]) }));
        context.RegisterFunction("append4", typeof(SomeCompareMethod2).GetMethod("Append4", new[] { typeof(string), typeof(string[]) }));
        context.RegisterFunction("appendChar", typeof(SomeCompareMethod2).GetMethod("AppendChar", new[] { typeof(char[]) }));
        context.RegisterFunction("sum", typeof(SomeCompareMethod2).GetMethod("Sum", new[] { typeof(int[]) }));
        context.RegisterFunction("sumDouble", typeof(SomeCompareMethod2).GetMethod("SumDouble", new[] { typeof(double[]) }));
        context.RegisterFunction("sumFloat", typeof(SomeCompareMethod2).GetMethod("SumFloat", new[] { typeof(float[]) }));

        context.SetVariable("stringArray", new[] { "x", "y", "z" });
        context.SetVariable("intArray", new[] { 5, 6, 9 });
        context.SetVariable("doubleArray", new[] { 5.0d, 6.0d, 9.0d });
        context.SetVariable("floatArray", new[] { 5.0f, 6.0f, 9.0f });

        _expression = _parser.ParseExpression("#append('a','b','c')");
        Assert.Equal("abc", _expression.GetValue(context).ToString());
        Assert.True(((SpelNode)((SpelExpression)_expression).AST).IsCompilable());
        AssertCanCompile(_expression);
        Assert.Equal("abc", _expression.GetValue(context).ToString());

        _expression = _parser.ParseExpression("#append('a')");
        Assert.Equal("a", _expression.GetValue(context).ToString());
        Assert.True(((SpelNode)((SpelExpression)_expression).AST).IsCompilable());
        AssertCanCompile(_expression);
        Assert.Equal("a", _expression.GetValue(context).ToString());

        _expression = _parser.ParseExpression("#append()");
        Assert.Equal(string.Empty, _expression.GetValue(context).ToString());
        Assert.True(((SpelNode)((SpelExpression)_expression).AST).IsCompilable());
        AssertCanCompile(_expression);
        Assert.Equal(string.Empty, _expression.GetValue(context).ToString());

        _expression = _parser.ParseExpression("#append(#stringArray)");
        Assert.Equal("xyz", _expression.GetValue(context).ToString());
        Assert.True(((SpelNode)((SpelExpression)_expression).AST).IsCompilable());
        AssertCanCompile(_expression);
        Assert.Equal("xyz", _expression.GetValue(context).ToString());

        // This is a methodreference invocation, to compare with functionreference
        _expression = _parser.ParseExpression("Append(#stringArray)");
        Assert.Equal("xyz", _expression.GetValue(context, new SomeCompareMethod2()).ToString());
        Assert.True(((SpelNode)((SpelExpression)_expression).AST).IsCompilable());
        AssertCanCompile(_expression);
        Assert.Equal("xyz", _expression.GetValue(context, new SomeCompareMethod2()).ToString());

        _expression = _parser.ParseExpression("#append2('a','b','c')");
        Assert.Equal("abc", _expression.GetValue(context).ToString());
        Assert.True(((SpelNode)((SpelExpression)_expression).AST).IsCompilable());
        AssertCanCompile(_expression);
        Assert.Equal("abc", _expression.GetValue(context).ToString());

        _expression = _parser.ParseExpression("#append2('a','b','c')");
        Assert.Equal("abc", _expression.GetValue(context).ToString());
        Assert.True(((SpelNode)((SpelExpression)_expression).AST).IsCompilable());
        AssertCanCompile(_expression);
        Assert.Equal("abc", _expression.GetValue(context).ToString());

        _expression = _parser.ParseExpression("Append2('a','b')");
        Assert.Equal("ab", _expression.GetValue(context, new SomeCompareMethod2()).ToString());
        Assert.True(((SpelNode)((SpelExpression)_expression).AST).IsCompilable());
        AssertCanCompile(_expression);
        Assert.Equal("ab", _expression.GetValue(context, new SomeCompareMethod2()).ToString());

        _expression = _parser.ParseExpression("#append2('a','b')");
        Assert.Equal("ab", _expression.GetValue(context).ToString());
        Assert.True(((SpelNode)((SpelExpression)_expression).AST).IsCompilable());
        AssertCanCompile(_expression);
        Assert.Equal("ab", _expression.GetValue(context).ToString());

        _expression = _parser.ParseExpression("#append2()");
        Assert.Equal(string.Empty, _expression.GetValue(context).ToString());
        Assert.True(((SpelNode)((SpelExpression)_expression).AST).IsCompilable());
        AssertCanCompile(_expression);
        Assert.Equal(string.Empty, _expression.GetValue(context).ToString());

        _expression = _parser.ParseExpression("#append3(#stringArray)");
        Assert.Equal("xyz", _expression.GetValue(context, new SomeCompareMethod2()).ToString());
        Assert.True(((SpelNode)((SpelExpression)_expression).AST).IsCompilable());
        AssertCanCompile(_expression);
        Assert.Equal("xyz", _expression.GetValue(context, new SomeCompareMethod2()).ToString());

        _expression = _parser.ParseExpression("#sum(1,2,3)");
        Assert.Equal(6, _expression.GetValue(context));
        Assert.True(((SpelNode)((SpelExpression)_expression).AST).IsCompilable());
        AssertCanCompile(_expression);
        Assert.Equal(6, _expression.GetValue(context));

        _expression = _parser.ParseExpression("#sum(2)");
        Assert.Equal(2, _expression.GetValue(context));
        Assert.True(((SpelNode)((SpelExpression)_expression).AST).IsCompilable());
        AssertCanCompile(_expression);
        Assert.Equal(2, _expression.GetValue(context));

        _expression = _parser.ParseExpression("#sum()");
        Assert.Equal(0, _expression.GetValue(context));
        Assert.True(((SpelNode)((SpelExpression)_expression).AST).IsCompilable());
        AssertCanCompile(_expression);
        Assert.Equal(0, _expression.GetValue(context));

        _expression = _parser.ParseExpression("#sum(#intArray)");
        Assert.Equal(20, _expression.GetValue(context));
        Assert.True(((SpelNode)((SpelExpression)_expression).AST).IsCompilable());
        AssertCanCompile(_expression);
        Assert.Equal(20, _expression.GetValue(context));

        _expression = _parser.ParseExpression("#sumDouble(1.0d,2.0d,3.0d)");
        Assert.Equal(6, _expression.GetValue(context));
        Assert.True(((SpelNode)((SpelExpression)_expression).AST).IsCompilable());
        AssertCanCompile(_expression);
        Assert.Equal(6, _expression.GetValue(context));

        _expression = _parser.ParseExpression("#sumDouble(2.0d)");
        Assert.Equal(2, _expression.GetValue(context));
        Assert.True(((SpelNode)((SpelExpression)_expression).AST).IsCompilable());
        AssertCanCompile(_expression);
        Assert.Equal(2, _expression.GetValue(context));

        _expression = _parser.ParseExpression("#sumDouble()");
        Assert.Equal(0, _expression.GetValue(context));
        Assert.True(((SpelNode)((SpelExpression)_expression).AST).IsCompilable());
        AssertCanCompile(_expression);
        Assert.Equal(0, _expression.GetValue(context));

        _expression = _parser.ParseExpression("#sumDouble(#doubleArray)");
        Assert.Equal(20, _expression.GetValue(context));
        Assert.True(((SpelNode)((SpelExpression)_expression).AST).IsCompilable());
        AssertCanCompile(_expression);
        Assert.Equal(20, _expression.GetValue(context));

        _expression = _parser.ParseExpression("#sumFloat(1.0f,2.0f,3.0f)");
        Assert.Equal(6, _expression.GetValue(context));
        Assert.True(((SpelNode)((SpelExpression)_expression).AST).IsCompilable());
        AssertCanCompile(_expression);
        Assert.Equal(6, _expression.GetValue(context));

        _expression = _parser.ParseExpression("#sumFloat(2.0f)");
        Assert.Equal(2, _expression.GetValue(context));
        Assert.True(((SpelNode)((SpelExpression)_expression).AST).IsCompilable());
        AssertCanCompile(_expression);
        Assert.Equal(2, _expression.GetValue(context));

        _expression = _parser.ParseExpression("#sumFloat()");
        Assert.Equal(0, _expression.GetValue(context));
        Assert.True(((SpelNode)((SpelExpression)_expression).AST).IsCompilable());
        AssertCanCompile(_expression);
        Assert.Equal(0, _expression.GetValue(context));

        _expression = _parser.ParseExpression("#sumFloat(#floatArray)");
        Assert.Equal(20, _expression.GetValue(context));
        Assert.True(((SpelNode)((SpelExpression)_expression).AST).IsCompilable());
        AssertCanCompile(_expression);
        Assert.Equal(20, _expression.GetValue(context));

        _expression = _parser.ParseExpression("#appendChar('abc'.get_Chars(0),'abc'.get_Chars(1))");
        Assert.Equal("ab", _expression.GetValue(context));
        Assert.True(((SpelNode)((SpelExpression)_expression).AST).IsCompilable());
        AssertCanCompile(_expression);
        Assert.Equal("ab", _expression.GetValue(context));

        _expression = _parser.ParseExpression("#append4('a','b','c')");
        Assert.Equal("a::bc", _expression.GetValue(context));
        Assert.True(((SpelNode)((SpelExpression)_expression).AST).IsCompilable());
        AssertCanCompile(_expression);
        Assert.Equal("a::bc", _expression.GetValue(context));

        _expression = _parser.ParseExpression("#append4('a','b')");
        Assert.Equal("a::b", _expression.GetValue(context));
        Assert.True(((SpelNode)((SpelExpression)_expression).AST).IsCompilable());
        AssertCanCompile(_expression);
        Assert.Equal("a::b", _expression.GetValue(context));

        _expression = _parser.ParseExpression("#append4('a')");
        Assert.Equal("a::", _expression.GetValue(context));
        Assert.True(((SpelNode)((SpelExpression)_expression).AST).IsCompilable());
        AssertCanCompile(_expression);
        Assert.Equal("a::", _expression.GetValue(context));

        _expression = _parser.ParseExpression("#append4('a',#stringArray)");
        Assert.Equal("a::xyz", _expression.GetValue(context).ToString());
        Assert.True(((SpelNode)((SpelExpression)_expression).AST).IsCompilable());
        AssertCanCompile(_expression);
        Assert.Equal("a::xyz", _expression.GetValue(context).ToString());
    }

    [Fact]
    public void FunctionReferenceVarargs()
    {
        var ctx = new StandardEvaluationContext();
        var m = GetType().GetMethod("Join", new[] { typeof(string[]) });
        ctx.SetVariable("join", m);
        _expression = _parser.ParseExpression("#join('a','b','c')");
        Assert.Equal("abc", _expression.GetValue(ctx));
        AssertCanCompile(_expression);
        Assert.Equal("abc", _expression.GetValue(ctx));
    }

    [Fact]
    public void VariableReferenceUserDefined()
    {
        var ctx = new StandardEvaluationContext();
        ctx.SetVariable("target", "abc");
        _expression = _parser.ParseExpression("#target");
        Assert.Equal("abc", _expression.GetValue(ctx));
        AssertCanCompile(_expression);
        Assert.Equal("abc", _expression.GetValue(ctx));
        ctx.SetVariable("target", "123");
        Assert.Equal("123", _expression.GetValue(ctx));
        ctx.SetVariable("target", 42);
        var ex = Assert.Throws<SpelEvaluationException>(() => _expression.GetValue(ctx));
        Assert.IsType<InvalidCastException>(ex.InnerException);

        ctx.SetVariable("target", "abc");
        _expression = _parser.ParseExpression("#target.get_Chars(0)");
        Assert.Equal('a', _expression.GetValue(ctx));
        AssertCanCompile(_expression);
        Assert.Equal('a', _expression.GetValue(ctx));
        ctx.SetVariable("target", "1");
        Assert.Equal('1', _expression.GetValue(ctx));
        ctx.SetVariable("target", 42);
        ex = Assert.Throws<SpelEvaluationException>(() => _expression.GetValue(ctx));
        Assert.IsType<InvalidCastException>(ex.InnerException);
    }

    [Fact]
    public void OpLt()
    {
        _expression = Parse("3.0d < 4.0d");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("3446.0d < 1123.0d");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("3 < 1");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("2 < 4");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("3.0f < 1.0f");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("1.0f < 5.0f");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("30L < 30L");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("15L < 20L");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("1 < 3.0d");
        AssertCantCompile(_expression);

        object d = 3.0d;
        _expression = Parse("#root<3.0d");
        Assert.False(_expression.GetValue<bool>(d));
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>(d));

        object i = 3;
        _expression = Parse("#root<3");
        Assert.False(_expression.GetValue<bool>(i));
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>(i));

        object f = 3.0f;
        _expression = Parse("#root<3.0f");
        Assert.False(_expression.GetValue<bool>(f));
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>(f));

        var l = 300L;
        _expression = Parse("#root<300l");
        Assert.False(_expression.GetValue<bool>(l));
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>(l));

        _expression = Parse("T(int).Parse('3') < 4");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("T(int).Parse('3') < T(Int32).Parse('3')");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("5 < T(int).Parse('3')");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("T(float).Parse('3.0') < 4.0f");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("T(float).Parse('3.0') < T(Single).Parse('3.0')");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("5.0f < T(float).Parse('3.0')");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("T(long).Parse('3') < 4L");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("T(long).Parse('3') < T(long).Parse('3')");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("5L < T(long).Parse('3')");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("T(double).Parse('3.0') < 4.0d");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("T(double).Parse('3.0') < T(double).Parse('3.0')");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("5.0d < T(double).Parse('3.0')");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());
    }

    [Fact]
    public void OpLE()
    {
        _expression = Parse("3.0d <= 4.0d");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("3446.0d <= 1123.0d");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("3 <= 1");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("2 <= 4");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("3.0f <= 1.0f");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("1.0f <= 5.0f");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("30L <= 30L");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("15L <= 20L");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("1 <= 3.0d");
        AssertCantCompile(_expression);

        object d = 3.0d;
        _expression = Parse("#root<=3.0d");
        Assert.True(_expression.GetValue<bool>(d));
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>(d));

        object i = 3;
        _expression = Parse("#root<=3");
        Assert.True(_expression.GetValue<bool>(i));
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>(i));

        object f = 3.0f;
        _expression = Parse("#root<=3.0f");
        Assert.True(_expression.GetValue<bool>(f));
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>(f));

        var l = 300L;
        _expression = Parse("#root<=300l");
        Assert.True(_expression.GetValue<bool>(l));
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>(l));

        _expression = Parse("T(int).Parse('3') <= 4");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("T(int).Parse('3') <= T(Int32).Parse('3')");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("5 <= T(int).Parse('3')");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("T(float).Parse('3.0') <= 4.0f");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("T(float).Parse('3.0') <= T(Single).Parse('3.0')");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("5.0f <= T(float).Parse('3.0')");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("T(long).Parse('3') <= 4L");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("T(long).Parse('3') <= T(long).Parse('3')");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("5L <= T(long).Parse('3')");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("T(double).Parse('3.0') <= 4.0d");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("T(double).Parse('3.0') <= T(double).Parse('3.0')");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("5.0d <= T(double).Parse('3.0')");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());
    }

    [Fact]
    public void OpGT()
    {
        _expression = Parse("3.0d > 4.0d");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("3446.0d > 1123.0d");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("3 > 1");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("2 > 4");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("3.0f > 1.0f");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("1.0f > 5.0f");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("30L > 30L");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("15L > 20L");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("1 > 3.0d");
        AssertCantCompile(_expression);

        object d = 3.0d;
        _expression = Parse("#root>3.0d");
        Assert.False(_expression.GetValue<bool>(d));
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>(d));

        object i = 3;
        _expression = Parse("#root>3");
        Assert.False(_expression.GetValue<bool>(i));
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>(i));

        object f = 3.0f;
        _expression = Parse("#root>3.0f");
        Assert.False(_expression.GetValue<bool>(f));
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>(f));

        var l = 300L;
        _expression = Parse("#root>300l");
        Assert.False(_expression.GetValue<bool>(l));
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>(l));

        _expression = Parse("T(int).Parse('3') > 4");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("T(int).Parse('3') > T(Int32).Parse('3')");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("5 > T(int).Parse('3')");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("T(float).Parse('3.0') > 4.0f");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("T(float).Parse('3.0') > T(Single).Parse('3.0')");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("5.0f > T(float).Parse('3.0')");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("T(long).Parse('3') > 4L");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("T(long).Parse('3') > T(long).Parse('3')");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("5L > T(long).Parse('3')");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("T(double).Parse('3.0') > 4.0d");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("T(double).Parse('3.0') > T(double).Parse('3.0')");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("5.0d > T(double).Parse('3.0')");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());
    }

    [Fact]
    public void OpGE()
    {
        _expression = Parse("3.0d >= 4.0d");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("3446.0d >= 1123.0d");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("3 >= 1");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("2 >= 4");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("3.0f >= 1.0f");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("1.0f >= 5.0f");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("30L >= 30L");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("15L >= 20L");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("1 >= 3.0d");
        AssertCantCompile(_expression);

        object d = 3.0d;
        _expression = Parse("#root>=3.0d");
        Assert.True(_expression.GetValue<bool>(d));
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>(d));

        object i = 3;
        _expression = Parse("#root>=3");
        Assert.True(_expression.GetValue<bool>(i));
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>(i));

        object f = 3.0f;
        _expression = Parse("#root>=3.0f");
        Assert.True(_expression.GetValue<bool>(f));
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>(f));

        var l = 300L;
        _expression = Parse("#root>=300l");
        Assert.True(_expression.GetValue<bool>(l));
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>(l));

        _expression = Parse("T(int).Parse('3') >= 4");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("T(int).Parse('3') >= T(Int32).Parse('3')");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("5 >= T(int).Parse('3')");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("T(float).Parse('3.0') >= 4.0f");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("T(float).Parse('3.0') >= T(Single).Parse('3.0')");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("5.0f >= T(float).Parse('3.0')");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("T(long).Parse('3') >= 4L");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("T(long).Parse('3') >= T(long).Parse('3')");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("5L >= T(long).Parse('3')");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("T(double).Parse('3.0') >= 4.0d");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("T(double).Parse('3.0') >= T(double).Parse('3.0')");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("5.0d >= T(double).Parse('3.0')");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());
    }

    [Fact]
    public void OpEq()
    {
        var tvar = "35";
        _expression = Parse("#root == 35");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("35 == #root");
        _expression.GetValue(tvar);
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        var tc7 = new TestClass7();
        _expression = Parse("Property == 'UK'");
        Assert.True(_expression.GetValue<bool>(tc7));
        TestClass7.Property = null;
        Assert.False(_expression.GetValue<bool>(tc7));
        AssertCanCompile(_expression);
        TestClass7.Reset();
        Assert.True(_expression.GetValue<bool>(tc7));
        TestClass7.Property = "UK";
        Assert.True(_expression.GetValue<bool>(tc7));
        TestClass7.Reset();
        TestClass7.Property = null;
        Assert.False(_expression.GetValue<bool>(tc7));
        _expression = Parse("Property == null");
        Assert.True(_expression.GetValue<bool>(tc7));
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>(tc7));

        _expression = Parse("3.0d == 4.0d");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("3446.0d == 3446.0d");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("3 == 1");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("3 == 3");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("3.0f == 1.0f");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("2.0f == 2.0f");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("30L == 30L");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("15L == 20L");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("1 == 3.0d");
        AssertCantCompile(_expression);

        object d = 3.0d;
        _expression = Parse("#root==3.0d");
        Assert.True(_expression.GetValue<bool>(d));
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>(d));

        object i = 3;
        _expression = Parse("#root==3");
        Assert.True(_expression.GetValue<bool>(i));
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>(i));

        object f = 3.0f;
        _expression = Parse("#root==3.0f");
        Assert.True(_expression.GetValue<bool>(f));
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>(f));

        var l = 300L;
        _expression = Parse("#root==300l");
        Assert.True(_expression.GetValue<bool>(l));
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>(l));

        var b = true;
        _expression = Parse("#root==true");
        Assert.True(_expression.GetValue<bool>(b));
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>(b));

        _expression = Parse("T(int).Parse('3') == 4");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("T(int).Parse('3') == T(Int32).Parse('3')");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("5 == T(int).Parse('3')");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("T(float).Parse('3.0') == 4.0f");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("T(float).Parse('3.0') == T(Single).Parse('3.0')");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("5.0f == T(float).Parse('3.0')");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("T(long).Parse('3') == 4L");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("T(long).Parse('3') == T(long).Parse('3')");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("5L == T(long).Parse('3')");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("T(double).Parse('3.0') == 4.0d");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("T(double).Parse('3.0') == T(double).Parse('3.0')");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("5.0d == T(double).Parse('3.0')");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("False == True");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("T(boolean).Parse('True') == T(boolean).Parse('True')");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("T(boolean).Parse('True') == True");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("False == T(boolean).Parse('False')");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());
    }

    [Fact]
    public void OpNe()
    {
        _expression = Parse("3.0d != 4.0d");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("3446.0d != 3446.0d");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("3 != 1");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("3 != 3");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("3.0f != 1.0f");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("2.0f != 2.0f");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("30L != 30L");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("15L != 20L");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("1 != 3.0d");
        AssertCantCompile(_expression);

        object d = 3.0d;
        _expression = Parse("#root!=3.0d");
        Assert.False(_expression.GetValue<bool>(d));
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>(d));

        object i = 3;
        _expression = Parse("#root!=3");
        Assert.False(_expression.GetValue<bool>(i));
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>(i));

        object f = 3.0f;
        _expression = Parse("#root!=3.0f");
        Assert.False(_expression.GetValue<bool>(f));
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>(f));

        var l = 300L;
        _expression = Parse("#root!=300l");
        Assert.False(_expression.GetValue<bool>(l));
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>(l));

        var b = true;
        _expression = Parse("#root!=true");
        Assert.False(_expression.GetValue<bool>(b));
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>(b));

        _expression = Parse("T(int).Parse('3') != 4");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("T(int).Parse('3') != T(Int32).Parse('3')");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("5 != T(int).Parse('3')");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("T(float).Parse('3.0') != 4.0f");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("T(float).Parse('3.0') != T(Single).Parse('3.0')");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("5.0f != T(float).Parse('3.0')");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("T(long).Parse('3') != 4L");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("T(long).Parse('3') != T(long).Parse('3')");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("5L != T(long).Parse('3')");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("T(double).Parse('3.0') != 4.0d");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("T(double).Parse('3.0') != T(double).Parse('3.0')");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("5.0d != T(double).Parse('3.0')");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("False != True");
        Assert.True(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>());

        _expression = Parse("T(boolean).Parse('True') != T(boolean).Parse('True')");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("T(boolean).Parse('True') != True");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());

        _expression = Parse("False != T(boolean).Parse('False')");
        Assert.False(_expression.GetValue<bool>());
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>());
    }

    [Fact]
    public void OpNe_SPR14863()
    {
        var configuration = new SpelParserOptions(SpelCompilerMode.MIXED);
        var parser = new SpelExpressionParser(configuration);
        var expression = parser.ParseExpression("Data['my-key'] != 'my-value'");
        var data = new Dictionary<string, string>
        {
            { "my-key", "my-value" }
        };
        var context = new StandardEvaluationContext(new MyContext(data));
        Assert.False(expression.GetValue<bool>(context));
        AssertCanCompile(expression);
        Assert.False(expression.GetValue<bool>(context));

        var ls = new List<string>
        {
            "foo"
        };
        context = new StandardEvaluationContext(ls);
        expression = Parse("get_Item(0) != 'foo'");
        Assert.False(expression.GetValue<bool>(context));
        AssertCanCompile(expression);
        Assert.False(expression.GetValue<bool>(context));

        ls.RemoveAt(0);
        ls.Add("goo");
        Assert.True(expression.GetValue<bool>(context));
    }

    [Fact]
    public void OpEq_SPR14863()
    {
        // Exercise the comparator invocation code that runs in
        // equalityCheck() (called from interpreted and compiled code)
        _expression = _parser.ParseExpression("#aa==#bb");
        var sec = new StandardEvaluationContext();
        var aa = new Apple(1);
        var bb = new Apple(2);
        sec.SetVariable("aa", aa);
        sec.SetVariable("bb", bb);

        var b = _expression.GetValue<bool>(sec);
        Assert.Same(bb, aa.GotComparedTo);
        Assert.False(b);
        bb.SetValue(1);
        b = _expression.GetValue<bool>(sec);
        Assert.Same(bb, aa.GotComparedTo);
        Assert.True(b);

        AssertCanCompile(_expression);

        // Similar test with compiled expression
        aa = new Apple(99);
        bb = new Apple(100);
        sec.SetVariable("aa", aa);
        sec.SetVariable("bb", bb);
        b = _expression.GetValue<bool>(sec);
        Assert.Same(bb, aa.GotComparedTo);
        Assert.False(b);
        bb.SetValue(99);
        b = _expression.GetValue<bool>(sec);
        Assert.Same(bb, aa.GotComparedTo);
        Assert.True(b);

        var ls = new List<string>
        {
            "foo"
        };
        var context = new StandardEvaluationContext(ls);
        var expression = Parse("get_Item(0) == 'foo'");
        Assert.True(expression.GetValue<bool>(context));
        AssertCanCompile(expression);
        Assert.True(expression.GetValue<bool>(context));

        ls.RemoveAt(0);
        ls.Add("goo");
        Assert.False(expression.GetValue<bool>(context));
    }

    [Fact]
    public void OpDivide_MixedNumberTypes()
    {
        var p = new PayloadX();

        // This is what you had to do before the changes in order for it to compile:
        // expression = parse("(T(java.lang.Double).parseDouble(payload.valueI.toString()))/60D");

        // right is a double
        CheckCalc(p, "payload.valueSB/60D", 2d);
        CheckCalc(p, "payload.valueBB/60D", 2d);
        CheckCalc(p, "payload.valueFB/60D", 2d);
        CheckCalc(p, "payload.valueDB/60D", 2d);
        CheckCalc(p, "payload.valueJB/60D", 2d);
        CheckCalc(p, "payload.valueIB/60D", 2d);

        CheckCalc(p, "payload.valueS/60D", 2d);
        CheckCalc(p, "payload.valueB/60D", 2d);
        CheckCalc(p, "payload.valueF/60D", 2d);
        CheckCalc(p, "payload.valueD/60D", 2d);
        CheckCalc(p, "payload.valueJ/60D", 2d);
        CheckCalc(p, "payload.valueI/60D", 2d);

        CheckCalc(p, "payload.valueSB/payload.valueDB60", 2d);
        CheckCalc(p, "payload.valueBB/payload.valueDB60", 2d);
        CheckCalc(p, "payload.valueFB/payload.valueDB60", 2d);
        CheckCalc(p, "payload.valueDB/payload.valueDB60", 2d);
        CheckCalc(p, "payload.valueJB/payload.valueDB60", 2d);
        CheckCalc(p, "payload.valueIB/payload.valueDB60", 2d);

        CheckCalc(p, "payload.valueS/payload.valueDB60", 2d);
        CheckCalc(p, "payload.valueB/payload.valueDB60", 2d);
        CheckCalc(p, "payload.valueF/payload.valueDB60", 2d);
        CheckCalc(p, "payload.valueD/payload.valueDB60", 2d);
        CheckCalc(p, "payload.valueJ/payload.valueDB60", 2d);
        CheckCalc(p, "payload.valueI/payload.valueDB60", 2d);

        // right is a float
        CheckCalc(p, "payload.valueSB/60F", 2F);
        CheckCalc(p, "payload.valueBB/60F", 2F);
        CheckCalc(p, "payload.valueFB/60F", 2f);
        CheckCalc(p, "payload.valueDB/60F", 2d);
        CheckCalc(p, "payload.valueJB/60F", 2F);
        CheckCalc(p, "payload.valueIB/60F", 2F);

        CheckCalc(p, "payload.valueS/60F", 2F);
        CheckCalc(p, "payload.valueB/60F", 2F);
        CheckCalc(p, "payload.valueF/60F", 2f);
        CheckCalc(p, "payload.valueD/60F", 2d);
        CheckCalc(p, "payload.valueJ/60F", 2F);
        CheckCalc(p, "payload.valueI/60F", 2F);

        CheckCalc(p, "payload.valueSB/payload.valueFB60", 2F);
        CheckCalc(p, "payload.valueBB/payload.valueFB60", 2F);
        CheckCalc(p, "payload.valueFB/payload.valueFB60", 2f);
        CheckCalc(p, "payload.valueDB/payload.valueFB60", 2d);
        CheckCalc(p, "payload.valueJB/payload.valueFB60", 2F);
        CheckCalc(p, "payload.valueIB/payload.valueFB60", 2F);

        CheckCalc(p, "payload.valueS/payload.valueFB60", 2F);
        CheckCalc(p, "payload.valueB/payload.valueFB60", 2F);
        CheckCalc(p, "payload.valueF/payload.valueFB60", 2f);
        CheckCalc(p, "payload.valueD/payload.valueFB60", 2d);
        CheckCalc(p, "payload.valueJ/payload.valueFB60", 2F);
        CheckCalc(p, "payload.valueI/payload.valueFB60", 2F);

        // right is a long
        CheckCalc(p, "payload.valueSB/60L", 2L);
        CheckCalc(p, "payload.valueBB/60L", 2L);
        CheckCalc(p, "payload.valueFB/60L", 2f);
        CheckCalc(p, "payload.valueDB/60L", 2d);
        CheckCalc(p, "payload.valueJB/60L", 2L);
        CheckCalc(p, "payload.valueIB/60L", 2L);

        CheckCalc(p, "payload.valueS/60L", 2L);
        CheckCalc(p, "payload.valueB/60L", 2L);
        CheckCalc(p, "payload.valueF/60L", 2f);
        CheckCalc(p, "payload.valueD/60L", 2d);
        CheckCalc(p, "payload.valueJ/60L", 2L);
        CheckCalc(p, "payload.valueI/60L", 2L);

        CheckCalc(p, "payload.valueSB/payload.valueJB60", 2L);
        CheckCalc(p, "payload.valueBB/payload.valueJB60", 2L);
        CheckCalc(p, "payload.valueFB/payload.valueJB60", 2f);
        CheckCalc(p, "payload.valueDB/payload.valueJB60", 2d);
        CheckCalc(p, "payload.valueJB/payload.valueJB60", 2L);
        CheckCalc(p, "payload.valueIB/payload.valueJB60", 2L);

        CheckCalc(p, "payload.valueS/payload.valueJB60", 2L);
        CheckCalc(p, "payload.valueB/payload.valueJB60", 2L);
        CheckCalc(p, "payload.valueF/payload.valueJB60", 2f);
        CheckCalc(p, "payload.valueD/payload.valueJB60", 2d);
        CheckCalc(p, "payload.valueJ/payload.valueJB60", 2L);
        CheckCalc(p, "payload.valueI/payload.valueJB60", 2L);

        // right is an int
        CheckCalc(p, "payload.valueSB/60", 2);
        CheckCalc(p, "payload.valueBB/60", 2);
        CheckCalc(p, "payload.valueFB/60", 2f);
        CheckCalc(p, "payload.valueDB/60", 2d);
        CheckCalc(p, "payload.valueJB/60", 2L);
        CheckCalc(p, "payload.valueIB/60", 2);

        CheckCalc(p, "payload.valueS/60", 2);
        CheckCalc(p, "payload.valueB/60", 2);
        CheckCalc(p, "payload.valueF/60", 2f);
        CheckCalc(p, "payload.valueD/60", 2d);
        CheckCalc(p, "payload.valueJ/60", 2L);
        CheckCalc(p, "payload.valueI/60", 2);

        CheckCalc(p, "payload.valueSB/payload.valueIB60", 2);
        CheckCalc(p, "payload.valueBB/payload.valueIB60", 2);
        CheckCalc(p, "payload.valueFB/payload.valueIB60", 2f);
        CheckCalc(p, "payload.valueDB/payload.valueIB60", 2d);
        CheckCalc(p, "payload.valueJB/payload.valueIB60", 2L);
        CheckCalc(p, "payload.valueIB/payload.valueIB60", 2);

        CheckCalc(p, "payload.valueS/payload.valueIB60", 2);
        CheckCalc(p, "payload.valueB/payload.valueIB60", 2);
        CheckCalc(p, "payload.valueF/payload.valueIB60", 2f);
        CheckCalc(p, "payload.valueD/payload.valueIB60", 2d);
        CheckCalc(p, "payload.valueJ/payload.valueIB60", 2L);
        CheckCalc(p, "payload.valueI/payload.valueIB60", 2);

        // right is a short
        CheckCalc(p, "payload.valueSB/payload.valueS", 1);
        CheckCalc(p, "payload.valueBB/payload.valueS", 1);
        CheckCalc(p, "payload.valueFB/payload.valueS", 1f);
        CheckCalc(p, "payload.valueDB/payload.valueS", 1d);
        CheckCalc(p, "payload.valueJB/payload.valueS", 1L);
        CheckCalc(p, "payload.valueIB/payload.valueS", 1);

        CheckCalc(p, "payload.valueS/payload.valueS", 1);
        CheckCalc(p, "payload.valueB/payload.valueS", 1);
        CheckCalc(p, "payload.valueF/payload.valueS", 1f);
        CheckCalc(p, "payload.valueD/payload.valueS", 1d);
        CheckCalc(p, "payload.valueJ/payload.valueS", 1L);
        CheckCalc(p, "payload.valueI/payload.valueS", 1);

        CheckCalc(p, "payload.valueSB/payload.valueSB", 1);
        CheckCalc(p, "payload.valueBB/payload.valueSB", 1);
        CheckCalc(p, "payload.valueFB/payload.valueSB", 1f);
        CheckCalc(p, "payload.valueDB/payload.valueSB", 1d);
        CheckCalc(p, "payload.valueJB/payload.valueSB", 1L);
        CheckCalc(p, "payload.valueIB/payload.valueSB", 1);

        CheckCalc(p, "payload.valueS/payload.valueSB", 1);
        CheckCalc(p, "payload.valueB/payload.valueSB", 1);
        CheckCalc(p, "payload.valueF/payload.valueSB", 1f);
        CheckCalc(p, "payload.valueD/payload.valueSB", 1d);
        CheckCalc(p, "payload.valueJ/payload.valueSB", 1L);
        CheckCalc(p, "payload.valueI/payload.valueSB", 1);

        // right is a byte
        CheckCalc(p, "payload.valueSB/payload.valueB", 1);
        CheckCalc(p, "payload.valueBB/payload.valueB", 1);
        CheckCalc(p, "payload.valueFB/payload.valueB", 1f);
        CheckCalc(p, "payload.valueDB/payload.valueB", 1d);
        CheckCalc(p, "payload.valueJB/payload.valueB", 1L);
        CheckCalc(p, "payload.valueIB/payload.valueB", 1);

        CheckCalc(p, "payload.valueS/payload.valueB", 1);
        CheckCalc(p, "payload.valueB/payload.valueB", 1);
        CheckCalc(p, "payload.valueF/payload.valueB", 1f);
        CheckCalc(p, "payload.valueD/payload.valueB", 1d);
        CheckCalc(p, "payload.valueJ/payload.valueB", 1L);
        CheckCalc(p, "payload.valueI/payload.valueB", 1);

        CheckCalc(p, "payload.valueSB/payload.valueBB", 1);
        CheckCalc(p, "payload.valueBB/payload.valueBB", 1);
        CheckCalc(p, "payload.valueFB/payload.valueBB", 1f);
        CheckCalc(p, "payload.valueDB/payload.valueBB", 1d);
        CheckCalc(p, "payload.valueJB/payload.valueBB", 1L);
        CheckCalc(p, "payload.valueIB/payload.valueBB", 1);

        CheckCalc(p, "payload.valueS/payload.valueBB", 1);
        CheckCalc(p, "payload.valueB/payload.valueBB", 1);
        CheckCalc(p, "payload.valueF/payload.valueBB", 1f);
        CheckCalc(p, "payload.valueD/payload.valueBB", 1d);
        CheckCalc(p, "payload.valueJ/payload.valueBB", 1L);
        CheckCalc(p, "payload.valueI/payload.valueBB", 1);
    }

    [Fact]
    public void OpPlus()
    {
        _expression = Parse("2+2");
        Assert.Equal(4, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(4, _expression.GetValue());

        _expression = Parse("2L+2L");
        Assert.Equal(4L, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(4L, _expression.GetValue());

        _expression = Parse("2.0f+2.0f");
        Assert.Equal(4F, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(4F, _expression.GetValue());

        _expression = Parse("3.0d+4.0d");
        Assert.Equal(7D, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(7D, _expression.GetValue());

        _expression = Parse("+1");
        Assert.Equal(1, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(1, _expression.GetValue());

        _expression = Parse("+1L");
        Assert.Equal(1L, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(1L, _expression.GetValue());

        _expression = Parse("+1.5f");
        Assert.Equal(1.5f, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(1.5f, _expression.GetValue());

        _expression = Parse("+2.5d");
        Assert.Equal(2.5d, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(2.5d, _expression.GetValue());

        _expression = Parse("+T(Double).Parse('2.5')");
        Assert.Equal(2.5d, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(2.5d, _expression.GetValue());

        _expression = Parse("T(int).Parse('2')+6");
        Assert.Equal(8, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(8, _expression.GetValue());

        _expression = Parse("T(int).Parse('1') + T(int).Parse('3')");
        Assert.Equal(4, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(4, _expression.GetValue());

        _expression = Parse("1+T(int).Parse('3')");
        Assert.Equal(4, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(4, _expression.GetValue());

        _expression = Parse("T(Single).Parse('2.0')+6");
        Assert.Equal(8.0f, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(8.0f, _expression.GetValue());

        _expression = Parse("T(float).Parse('2.0')+T(float).Parse('3.0')");
        Assert.Equal(5.0f, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(5.0f, _expression.GetValue());

        _expression = Parse("3L+T(Int64).Parse('4')");
        Assert.Equal(7L, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(7L, _expression.GetValue());

        _expression = Parse("T(long).Parse('2')+6");
        Assert.Equal(8L, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(8L, _expression.GetValue());

        _expression = Parse("T(Int64).Parse('2')+T(long).Parse('3')");
        Assert.Equal(5L, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(5L, _expression.GetValue());

        _expression = Parse("1L+T(long).Parse('2')");
        Assert.Equal(3L, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(3L, _expression.GetValue());
    }

    [Fact]
    public void OpPlus_MixedNumberTypes()
    {
        var p = new PayloadX();

        // This is what you had to do before the changes in order for it to compile:
        // expression = parse("(T(java.lang.Double).parseDouble(payload.valueI.toString()))/60D");

        // right is a double
        CheckCalc(p, "payload.valueSB+60D", 180d);
        CheckCalc(p, "payload.valueBB+60D", 180d);
        CheckCalc(p, "payload.valueFB+60D", 180d);
        CheckCalc(p, "payload.valueDB+60D", 180d);
        CheckCalc(p, "payload.valueJB+60D", 180d);
        CheckCalc(p, "payload.valueIB+60D", 180d);

        CheckCalc(p, "payload.valueS+60D", 180d);
        CheckCalc(p, "payload.valueB+60D", 180d);
        CheckCalc(p, "payload.valueF+60D", 180d);
        CheckCalc(p, "payload.valueD+60D", 180d);
        CheckCalc(p, "payload.valueJ+60D", 180d);
        CheckCalc(p, "payload.valueI+60D", 180d);

        CheckCalc(p, "payload.valueSB+payload.valueDB60", 180d);
        CheckCalc(p, "payload.valueBB+payload.valueDB60", 180d);
        CheckCalc(p, "payload.valueFB+payload.valueDB60", 180d);
        CheckCalc(p, "payload.valueDB+payload.valueDB60", 180d);
        CheckCalc(p, "payload.valueJB+payload.valueDB60", 180d);
        CheckCalc(p, "payload.valueIB+payload.valueDB60", 180d);

        CheckCalc(p, "payload.valueS+payload.valueDB60", 180d);
        CheckCalc(p, "payload.valueB+payload.valueDB60", 180d);
        CheckCalc(p, "payload.valueF+payload.valueDB60", 180d);
        CheckCalc(p, "payload.valueD+payload.valueDB60", 180d);
        CheckCalc(p, "payload.valueJ+payload.valueDB60", 180d);
        CheckCalc(p, "payload.valueI+payload.valueDB60", 180d);

        // right is a float
        CheckCalc(p, "payload.valueSB+60F", 180F);
        CheckCalc(p, "payload.valueBB+60F", 180F);
        CheckCalc(p, "payload.valueFB+60F", 180f);
        CheckCalc(p, "payload.valueDB+60F", 180d);
        CheckCalc(p, "payload.valueJB+60F", 180F);
        CheckCalc(p, "payload.valueIB+60F", 180F);

        CheckCalc(p, "payload.valueS+60F", 180F);
        CheckCalc(p, "payload.valueB+60F", 180F);
        CheckCalc(p, "payload.valueF+60F", 180f);
        CheckCalc(p, "payload.valueD+60F", 180d);
        CheckCalc(p, "payload.valueJ+60F", 180F);
        CheckCalc(p, "payload.valueI+60F", 180F);

        CheckCalc(p, "payload.valueSB+payload.valueFB60", 180F);
        CheckCalc(p, "payload.valueBB+payload.valueFB60", 180F);
        CheckCalc(p, "payload.valueFB+payload.valueFB60", 180f);
        CheckCalc(p, "payload.valueDB+payload.valueFB60", 180d);
        CheckCalc(p, "payload.valueJB+payload.valueFB60", 180F);
        CheckCalc(p, "payload.valueIB+payload.valueFB60", 180F);

        CheckCalc(p, "payload.valueS+payload.valueFB60", 180F);
        CheckCalc(p, "payload.valueB+payload.valueFB60", 180F);
        CheckCalc(p, "payload.valueF+payload.valueFB60", 180f);
        CheckCalc(p, "payload.valueD+payload.valueFB60", 180d);
        CheckCalc(p, "payload.valueJ+payload.valueFB60", 180F);
        CheckCalc(p, "payload.valueI+payload.valueFB60", 180F);

        // right is a long
        CheckCalc(p, "payload.valueSB+60L", 180L);
        CheckCalc(p, "payload.valueBB+60L", 180L);
        CheckCalc(p, "payload.valueFB+60L", 180f);
        CheckCalc(p, "payload.valueDB+60L", 180d);
        CheckCalc(p, "payload.valueJB+60L", 180L);
        CheckCalc(p, "payload.valueIB+60L", 180L);

        CheckCalc(p, "payload.valueS+60L", 180L);
        CheckCalc(p, "payload.valueB+60L", 180L);
        CheckCalc(p, "payload.valueF+60L", 180f);
        CheckCalc(p, "payload.valueD+60L", 180d);
        CheckCalc(p, "payload.valueJ+60L", 180L);
        CheckCalc(p, "payload.valueI+60L", 180L);

        CheckCalc(p, "payload.valueSB+payload.valueJB60", 180L);
        CheckCalc(p, "payload.valueBB+payload.valueJB60", 180L);
        CheckCalc(p, "payload.valueFB+payload.valueJB60", 180f);
        CheckCalc(p, "payload.valueDB+payload.valueJB60", 180d);
        CheckCalc(p, "payload.valueJB+payload.valueJB60", 180L);
        CheckCalc(p, "payload.valueIB+payload.valueJB60", 180L);

        CheckCalc(p, "payload.valueS+payload.valueJB60", 180L);
        CheckCalc(p, "payload.valueB+payload.valueJB60", 180L);
        CheckCalc(p, "payload.valueF+payload.valueJB60", 180f);
        CheckCalc(p, "payload.valueD+payload.valueJB60", 180d);
        CheckCalc(p, "payload.valueJ+payload.valueJB60", 180L);
        CheckCalc(p, "payload.valueI+payload.valueJB60", 180L);

        // right is an int
        CheckCalc(p, "payload.valueSB+60", 180);
        CheckCalc(p, "payload.valueBB+60", 180);
        CheckCalc(p, "payload.valueFB+60", 180f);
        CheckCalc(p, "payload.valueDB+60", 180d);
        CheckCalc(p, "payload.valueJB+60", 180L);
        CheckCalc(p, "payload.valueIB+60", 180);

        CheckCalc(p, "payload.valueS+60", 180);
        CheckCalc(p, "payload.valueB+60", 180);
        CheckCalc(p, "payload.valueF+60", 180f);
        CheckCalc(p, "payload.valueD+60", 180d);
        CheckCalc(p, "payload.valueJ+60", 180L);
        CheckCalc(p, "payload.valueI+60", 180);

        CheckCalc(p, "payload.valueSB+payload.valueIB60", 180);
        CheckCalc(p, "payload.valueBB+payload.valueIB60", 180);
        CheckCalc(p, "payload.valueFB+payload.valueIB60", 180f);
        CheckCalc(p, "payload.valueDB+payload.valueIB60", 180d);
        CheckCalc(p, "payload.valueJB+payload.valueIB60", 180L);
        CheckCalc(p, "payload.valueIB+payload.valueIB60", 180);

        CheckCalc(p, "payload.valueS+payload.valueIB60", 180);
        CheckCalc(p, "payload.valueB+payload.valueIB60", 180);
        CheckCalc(p, "payload.valueF+payload.valueIB60", 180f);
        CheckCalc(p, "payload.valueD+payload.valueIB60", 180d);
        CheckCalc(p, "payload.valueJ+payload.valueIB60", 180L);
        CheckCalc(p, "payload.valueI+payload.valueIB60", 180);

        // right is a short
        CheckCalc(p, "payload.valueSB+payload.valueS", 240);
        CheckCalc(p, "payload.valueBB+payload.valueS", 240);
        CheckCalc(p, "payload.valueFB+payload.valueS", 240f);
        CheckCalc(p, "payload.valueDB+payload.valueS", 240d);
        CheckCalc(p, "payload.valueJB+payload.valueS", 240L);
        CheckCalc(p, "payload.valueIB+payload.valueS", 240);

        CheckCalc(p, "payload.valueS+payload.valueS", 240);
        CheckCalc(p, "payload.valueB+payload.valueS", 240);
        CheckCalc(p, "payload.valueF+payload.valueS", 240f);
        CheckCalc(p, "payload.valueD+payload.valueS", 240d);
        CheckCalc(p, "payload.valueJ+payload.valueS", 240L);
        CheckCalc(p, "payload.valueI+payload.valueS", 240);

        CheckCalc(p, "payload.valueSB+payload.valueSB", 240);
        CheckCalc(p, "payload.valueBB+payload.valueSB", 240);
        CheckCalc(p, "payload.valueFB+payload.valueSB", 240f);
        CheckCalc(p, "payload.valueDB+payload.valueSB", 240d);
        CheckCalc(p, "payload.valueJB+payload.valueSB", 240L);
        CheckCalc(p, "payload.valueIB+payload.valueSB", 240);

        CheckCalc(p, "payload.valueS+payload.valueSB", 240);
        CheckCalc(p, "payload.valueB+payload.valueSB", 240);
        CheckCalc(p, "payload.valueF+payload.valueSB", 240f);
        CheckCalc(p, "payload.valueD+payload.valueSB", 240d);
        CheckCalc(p, "payload.valueJ+payload.valueSB", 240L);
        CheckCalc(p, "payload.valueI+payload.valueSB", 240);

        // right is a byte
        CheckCalc(p, "payload.valueSB+payload.valueB", 240);
        CheckCalc(p, "payload.valueBB+payload.valueB", 240);
        CheckCalc(p, "payload.valueFB+payload.valueB", 240f);
        CheckCalc(p, "payload.valueDB+payload.valueB", 240d);
        CheckCalc(p, "payload.valueJB+payload.valueB", 240L);
        CheckCalc(p, "payload.valueIB+payload.valueB", 240);

        CheckCalc(p, "payload.valueS+payload.valueB", 240);
        CheckCalc(p, "payload.valueB+payload.valueB", 240);
        CheckCalc(p, "payload.valueF+payload.valueB", 240f);
        CheckCalc(p, "payload.valueD+payload.valueB", 240d);
        CheckCalc(p, "payload.valueJ+payload.valueB", 240L);
        CheckCalc(p, "payload.valueI+payload.valueB", 240);

        CheckCalc(p, "payload.valueSB+payload.valueBB", 240);
        CheckCalc(p, "payload.valueBB+payload.valueBB", 240);
        CheckCalc(p, "payload.valueFB+payload.valueBB", 240f);
        CheckCalc(p, "payload.valueDB+payload.valueBB", 240d);
        CheckCalc(p, "payload.valueJB+payload.valueBB", 240L);
        CheckCalc(p, "payload.valueIB+payload.valueBB", 240);

        CheckCalc(p, "payload.valueS+payload.valueBB", 240);
        CheckCalc(p, "payload.valueB+payload.valueBB", 240);
        CheckCalc(p, "payload.valueF+payload.valueBB", 240f);
        CheckCalc(p, "payload.valueD+payload.valueBB", 240d);
        CheckCalc(p, "payload.valueJ+payload.valueBB", 240L);
        CheckCalc(p, "payload.valueI+payload.valueBB", 240);
    }

    [Fact]
    public void OpPlusString()
    {
        _expression = Parse("'hello' + 'world'");
        Assert.Equal("helloworld", _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal("helloworld", _expression.GetValue());

        _expression = Parse("'hello' + World");
        Assert.Equal("helloworld", _expression.GetValue(new Greeter()));
        AssertCanCompile(_expression);
        Assert.Equal("helloworld", _expression.GetValue(new Greeter()));

        _expression = Parse("World + 'hello'");
        Assert.Equal("worldhello", _expression.GetValue(new Greeter()));
        AssertCanCompile(_expression);
        Assert.Equal("worldhello", _expression.GetValue(new Greeter()));

        _expression = Parse("'hello' + World + ' spring'");
        Assert.Equal("helloworld spring", _expression.GetValue(new Greeter()));
        AssertCanCompile(_expression);
        Assert.Equal("helloworld spring", _expression.GetValue(new Greeter()));

        _expression = Parse("'hello' + 3 + ' spring'");
        Assert.Equal("hello3 spring", _expression.GetValue(new Greeter()));
        AssertCantCompile(_expression);

        _expression = Parse("GetObject() + 'a'");
        Assert.Equal("objecta", _expression.GetValue(new Greeter()));
        AssertCanCompile(_expression);
        Assert.Equal("objecta", _expression.GetValue(new Greeter()));

        _expression = Parse("'a'+GetObject()");
        Assert.Equal("aobject", _expression.GetValue(new Greeter()));
        AssertCanCompile(_expression);
        Assert.Equal("aobject", _expression.GetValue(new Greeter()));

        _expression = Parse("'a'+GetObject()+'a'");
        Assert.Equal("aobjecta", _expression.GetValue(new Greeter()));
        AssertCanCompile(_expression);
        Assert.Equal("aobjecta", _expression.GetValue(new Greeter()));

        _expression = Parse("GetObject()+'a'+GetObject()");
        Assert.Equal("objectaobject", _expression.GetValue(new Greeter()));
        AssertCanCompile(_expression);
        Assert.Equal("objectaobject", _expression.GetValue(new Greeter()));

        _expression = Parse("GetObject()+GetObject()");
        Assert.Equal("objectobject", _expression.GetValue(new Greeter()));
        AssertCanCompile(_expression);
        Assert.Equal("objectobject", _expression.GetValue(new Greeter()));
    }

    [Fact]
    public void OpMinus()
    {
        _expression = Parse("2-2");
        Assert.Equal(0, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(0, _expression.GetValue());

        _expression = Parse("4L - 2L");
        Assert.Equal(2L, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(2L, _expression.GetValue());

        _expression = Parse("4.0f-2.0f");
        Assert.Equal(2F, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(2F, _expression.GetValue());

        _expression = Parse("3.0d-4.0d");
        Assert.Equal(-1d, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(-1d, _expression.GetValue());

        _expression = Parse("-1");
        Assert.Equal(-1, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(-1, _expression.GetValue());

        _expression = Parse("-1L");
        Assert.Equal(-1L, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(-1L, _expression.GetValue());

        _expression = Parse("-1.5f");
        Assert.Equal(-1.5f, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(-1.5f, _expression.GetValue());

        _expression = Parse("-2.5d");
        Assert.Equal(-2.5d, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(-2.5d, _expression.GetValue());

        _expression = Parse("T(int).Parse('2')-6");
        Assert.Equal(-4, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(-4, _expression.GetValue());

        _expression = Parse("T(int).Parse('1')-T(Int32).Parse('3')");
        Assert.Equal(-2, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(-2, _expression.GetValue());

        _expression = Parse("4-T(Int32).Parse('3')");
        Assert.Equal(1, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(1, _expression.GetValue());

        _expression = Parse("T(Single).Parse('2.0')-6");
        Assert.Equal(-4.0f, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(-4.0f, _expression.GetValue());

        _expression = Parse("T(float).Parse('8.0')-T(float).Parse('3.0')");
        Assert.Equal(5.0f, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(5.0f, _expression.GetValue());

        _expression = Parse("11L-T(Int64).Parse('4')");
        Assert.Equal(7L, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(7L, _expression.GetValue());

        _expression = Parse("T(long).Parse('9')-6");
        Assert.Equal(3L, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(3L, _expression.GetValue());

        _expression = Parse("T(long).Parse('4')-T(long).Parse('3')");
        Assert.Equal(1L, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(1L, _expression.GetValue());

        _expression = Parse("8L-T(Int64).Parse('2')");
        Assert.Equal(6L, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(6L, _expression.GetValue());
    }

    [Fact]
    public void OpMinus_MixedNumberTypes()
    {
        var p = new PayloadX();

        // This is what you had to do before the changes in order for it to compile:
        // expression = parse("(T(java.lang.Double).parseDouble(payload.valueI.toString()))/60D");

        // right is a double
        CheckCalc(p, "payload.valueSB-60D", 60d);
        CheckCalc(p, "payload.valueBB-60D", 60d);
        CheckCalc(p, "payload.valueFB-60D", 60d);
        CheckCalc(p, "payload.valueDB-60D", 60d);
        CheckCalc(p, "payload.valueJB-60D", 60d);
        CheckCalc(p, "payload.valueIB-60D", 60d);

        CheckCalc(p, "payload.valueS-60D", 60d);
        CheckCalc(p, "payload.valueB-60D", 60d);
        CheckCalc(p, "payload.valueF-60D", 60d);
        CheckCalc(p, "payload.valueD-60D", 60d);
        CheckCalc(p, "payload.valueJ-60D", 60d);
        CheckCalc(p, "payload.valueI-60D", 60d);

        CheckCalc(p, "payload.valueSB-payload.valueDB60", 60d);
        CheckCalc(p, "payload.valueBB-payload.valueDB60", 60d);
        CheckCalc(p, "payload.valueFB-payload.valueDB60", 60d);
        CheckCalc(p, "payload.valueDB-payload.valueDB60", 60d);
        CheckCalc(p, "payload.valueJB-payload.valueDB60", 60d);
        CheckCalc(p, "payload.valueIB-payload.valueDB60", 60d);

        CheckCalc(p, "payload.valueS-payload.valueDB60", 60d);
        CheckCalc(p, "payload.valueB-payload.valueDB60", 60d);
        CheckCalc(p, "payload.valueF-payload.valueDB60", 60d);
        CheckCalc(p, "payload.valueD-payload.valueDB60", 60d);
        CheckCalc(p, "payload.valueJ-payload.valueDB60", 60d);
        CheckCalc(p, "payload.valueI-payload.valueDB60", 60d);

        // right is a float
        CheckCalc(p, "payload.valueSB-60F", 60F);
        CheckCalc(p, "payload.valueBB-60F", 60F);
        CheckCalc(p, "payload.valueFB-60F", 60f);
        CheckCalc(p, "payload.valueDB-60F", 60d);
        CheckCalc(p, "payload.valueJB-60F", 60F);
        CheckCalc(p, "payload.valueIB-60F", 60F);

        CheckCalc(p, "payload.valueS-60F", 60F);
        CheckCalc(p, "payload.valueB-60F", 60F);
        CheckCalc(p, "payload.valueF-60F", 60f);
        CheckCalc(p, "payload.valueD-60F", 60d);
        CheckCalc(p, "payload.valueJ-60F", 60F);
        CheckCalc(p, "payload.valueI-60F", 60F);

        CheckCalc(p, "payload.valueSB-payload.valueFB60", 60F);
        CheckCalc(p, "payload.valueBB-payload.valueFB60", 60F);
        CheckCalc(p, "payload.valueFB-payload.valueFB60", 60f);
        CheckCalc(p, "payload.valueDB-payload.valueFB60", 60d);
        CheckCalc(p, "payload.valueJB-payload.valueFB60", 60F);
        CheckCalc(p, "payload.valueIB-payload.valueFB60", 60F);

        CheckCalc(p, "payload.valueS-payload.valueFB60", 60F);
        CheckCalc(p, "payload.valueB-payload.valueFB60", 60F);
        CheckCalc(p, "payload.valueF-payload.valueFB60", 60f);
        CheckCalc(p, "payload.valueD-payload.valueFB60", 60d);
        CheckCalc(p, "payload.valueJ-payload.valueFB60", 60F);
        CheckCalc(p, "payload.valueI-payload.valueFB60", 60F);

        // right is a long
        CheckCalc(p, "payload.valueSB-60L", 60L);
        CheckCalc(p, "payload.valueBB-60L", 60L);
        CheckCalc(p, "payload.valueFB-60L", 60f);
        CheckCalc(p, "payload.valueDB-60L", 60d);
        CheckCalc(p, "payload.valueJB-60L", 60L);
        CheckCalc(p, "payload.valueIB-60L", 60L);

        CheckCalc(p, "payload.valueS-60L", 60L);
        CheckCalc(p, "payload.valueB-60L", 60L);
        CheckCalc(p, "payload.valueF-60L", 60f);
        CheckCalc(p, "payload.valueD-60L", 60d);
        CheckCalc(p, "payload.valueJ-60L", 60L);
        CheckCalc(p, "payload.valueI-60L", 60L);

        CheckCalc(p, "payload.valueSB-payload.valueJB60", 60L);
        CheckCalc(p, "payload.valueBB-payload.valueJB60", 60L);
        CheckCalc(p, "payload.valueFB-payload.valueJB60", 60f);
        CheckCalc(p, "payload.valueDB-payload.valueJB60", 60d);
        CheckCalc(p, "payload.valueJB-payload.valueJB60", 60L);
        CheckCalc(p, "payload.valueIB-payload.valueJB60", 60L);

        CheckCalc(p, "payload.valueS-payload.valueJB60", 60L);
        CheckCalc(p, "payload.valueB-payload.valueJB60", 60L);
        CheckCalc(p, "payload.valueF-payload.valueJB60", 60f);
        CheckCalc(p, "payload.valueD-payload.valueJB60", 60d);
        CheckCalc(p, "payload.valueJ-payload.valueJB60", 60L);
        CheckCalc(p, "payload.valueI-payload.valueJB60", 60L);

        // right is an int
        CheckCalc(p, "payload.valueSB-60", 60);
        CheckCalc(p, "payload.valueBB-60", 60);
        CheckCalc(p, "payload.valueFB-60", 60f);
        CheckCalc(p, "payload.valueDB-60", 60d);
        CheckCalc(p, "payload.valueJB-60", 60L);
        CheckCalc(p, "payload.valueIB-60", 60);

        CheckCalc(p, "payload.valueS-60", 60);
        CheckCalc(p, "payload.valueB-60", 60);
        CheckCalc(p, "payload.valueF-60", 60f);
        CheckCalc(p, "payload.valueD-60", 60d);
        CheckCalc(p, "payload.valueJ-60", 60L);
        CheckCalc(p, "payload.valueI-60", 60);

        CheckCalc(p, "payload.valueSB-payload.valueIB60", 60);
        CheckCalc(p, "payload.valueBB-payload.valueIB60", 60);
        CheckCalc(p, "payload.valueFB-payload.valueIB60", 60f);
        CheckCalc(p, "payload.valueDB-payload.valueIB60", 60d);
        CheckCalc(p, "payload.valueJB-payload.valueIB60", 60L);
        CheckCalc(p, "payload.valueIB-payload.valueIB60", 60);

        CheckCalc(p, "payload.valueS-payload.valueIB60", 60);
        CheckCalc(p, "payload.valueB-payload.valueIB60", 60);
        CheckCalc(p, "payload.valueF-payload.valueIB60", 60f);
        CheckCalc(p, "payload.valueD-payload.valueIB60", 60d);
        CheckCalc(p, "payload.valueJ-payload.valueIB60", 60L);
        CheckCalc(p, "payload.valueI-payload.valueIB60", 60);

        // right is a short
        CheckCalc(p, "payload.valueSB-payload.valueS20", 100);
        CheckCalc(p, "payload.valueBB-payload.valueS20", 100);
        CheckCalc(p, "payload.valueFB-payload.valueS20", 100f);
        CheckCalc(p, "payload.valueDB-payload.valueS20", 100d);
        CheckCalc(p, "payload.valueJB-payload.valueS20", 100L);
        CheckCalc(p, "payload.valueIB-payload.valueS20", 100);

        CheckCalc(p, "payload.valueS-payload.valueS20", 100);
        CheckCalc(p, "payload.valueB-payload.valueS20", 100);
        CheckCalc(p, "payload.valueF-payload.valueS20", 100f);
        CheckCalc(p, "payload.valueD-payload.valueS20", 100d);
        CheckCalc(p, "payload.valueJ-payload.valueS20", 100L);
        CheckCalc(p, "payload.valueI-payload.valueS20", 100);

        CheckCalc(p, "payload.valueSB-payload.valueSB20", 100);
        CheckCalc(p, "payload.valueBB-payload.valueSB20", 100);
        CheckCalc(p, "payload.valueFB-payload.valueSB20", 100f);
        CheckCalc(p, "payload.valueDB-payload.valueSB20", 100d);
        CheckCalc(p, "payload.valueJB-payload.valueSB20", 100L);
        CheckCalc(p, "payload.valueIB-payload.valueSB20", 100);

        CheckCalc(p, "payload.valueS-payload.valueSB20", 100);
        CheckCalc(p, "payload.valueB-payload.valueSB20", 100);
        CheckCalc(p, "payload.valueF-payload.valueSB20", 100f);
        CheckCalc(p, "payload.valueD-payload.valueSB20", 100d);
        CheckCalc(p, "payload.valueJ-payload.valueSB20", 100L);
        CheckCalc(p, "payload.valueI-payload.valueSB20", 100);

        // right is a byte
        CheckCalc(p, "payload.valueSB-payload.valueB20", 100);
        CheckCalc(p, "payload.valueBB-payload.valueB20", 100);
        CheckCalc(p, "payload.valueFB-payload.valueB20", 100f);
        CheckCalc(p, "payload.valueDB-payload.valueB20", 100d);
        CheckCalc(p, "payload.valueJB-payload.valueB20", 100L);
        CheckCalc(p, "payload.valueIB-payload.valueB20", 100);

        CheckCalc(p, "payload.valueS-payload.valueB20", 100);
        CheckCalc(p, "payload.valueB-payload.valueB20", 100);
        CheckCalc(p, "payload.valueF-payload.valueB20", 100f);
        CheckCalc(p, "payload.valueD-payload.valueB20", 100d);
        CheckCalc(p, "payload.valueJ-payload.valueB20", 100L);
        CheckCalc(p, "payload.valueI-payload.valueB20", 100);

        CheckCalc(p, "payload.valueSB-payload.valueBB20", 100);
        CheckCalc(p, "payload.valueBB-payload.valueBB20", 100);
        CheckCalc(p, "payload.valueFB-payload.valueBB20", 100f);
        CheckCalc(p, "payload.valueDB-payload.valueBB20", 100d);
        CheckCalc(p, "payload.valueJB-payload.valueBB20", 100L);
        CheckCalc(p, "payload.valueIB-payload.valueBB20", 100);

        CheckCalc(p, "payload.valueS-payload.valueBB20", 100);
        CheckCalc(p, "payload.valueB-payload.valueBB20", 100);
        CheckCalc(p, "payload.valueF-payload.valueBB20", 100f);
        CheckCalc(p, "payload.valueD-payload.valueBB20", 100d);
        CheckCalc(p, "payload.valueJ-payload.valueBB20", 100L);
        CheckCalc(p, "payload.valueI-payload.valueBB20", 100);
    }

    [Fact]
    public void OpMultiply_MixedNumberTypes()
    {
        var p = new PayloadX();

        // This is what you had to do before the changes in order for it to compile:
        // expression = parse("(T(java.lang.Double).parseDouble(payload.valueI.toString()))/60D");

        // right is a double
        CheckCalc(p, "payload.valueSB*60D", 7200d);
        CheckCalc(p, "payload.valueBB*60D", 7200d);
        CheckCalc(p, "payload.valueFB*60D", 7200d);
        CheckCalc(p, "payload.valueDB*60D", 7200d);
        CheckCalc(p, "payload.valueJB*60D", 7200d);
        CheckCalc(p, "payload.valueIB*60D", 7200d);

        CheckCalc(p, "payload.valueS*60D", 7200d);
        CheckCalc(p, "payload.valueB*60D", 7200d);
        CheckCalc(p, "payload.valueF*60D", 7200d);
        CheckCalc(p, "payload.valueD*60D", 7200d);
        CheckCalc(p, "payload.valueJ*60D", 7200d);
        CheckCalc(p, "payload.valueI*60D", 7200d);

        CheckCalc(p, "payload.valueSB*payload.valueDB60", 7200d);
        CheckCalc(p, "payload.valueBB*payload.valueDB60", 7200d);
        CheckCalc(p, "payload.valueFB*payload.valueDB60", 7200d);
        CheckCalc(p, "payload.valueDB*payload.valueDB60", 7200d);
        CheckCalc(p, "payload.valueJB*payload.valueDB60", 7200d);
        CheckCalc(p, "payload.valueIB*payload.valueDB60", 7200d);

        CheckCalc(p, "payload.valueS*payload.valueDB60", 7200d);
        CheckCalc(p, "payload.valueB*payload.valueDB60", 7200d);
        CheckCalc(p, "payload.valueF*payload.valueDB60", 7200d);
        CheckCalc(p, "payload.valueD*payload.valueDB60", 7200d);
        CheckCalc(p, "payload.valueJ*payload.valueDB60", 7200d);
        CheckCalc(p, "payload.valueI*payload.valueDB60", 7200d);

        // right is a float
        CheckCalc(p, "payload.valueSB*60F", 7200F);
        CheckCalc(p, "payload.valueBB*60F", 7200F);
        CheckCalc(p, "payload.valueFB*60F", 7200f);
        CheckCalc(p, "payload.valueDB*60F", 7200d);
        CheckCalc(p, "payload.valueJB*60F", 7200F);
        CheckCalc(p, "payload.valueIB*60F", 7200F);

        CheckCalc(p, "payload.valueS*60F", 7200F);
        CheckCalc(p, "payload.valueB*60F", 7200F);
        CheckCalc(p, "payload.valueF*60F", 7200f);
        CheckCalc(p, "payload.valueD*60F", 7200d);
        CheckCalc(p, "payload.valueJ*60F", 7200F);
        CheckCalc(p, "payload.valueI*60F", 7200F);

        CheckCalc(p, "payload.valueSB*payload.valueFB60", 7200F);
        CheckCalc(p, "payload.valueBB*payload.valueFB60", 7200F);
        CheckCalc(p, "payload.valueFB*payload.valueFB60", 7200f);
        CheckCalc(p, "payload.valueDB*payload.valueFB60", 7200d);
        CheckCalc(p, "payload.valueJB*payload.valueFB60", 7200F);
        CheckCalc(p, "payload.valueIB*payload.valueFB60", 7200F);

        CheckCalc(p, "payload.valueS*payload.valueFB60", 7200F);
        CheckCalc(p, "payload.valueB*payload.valueFB60", 7200F);
        CheckCalc(p, "payload.valueF*payload.valueFB60", 7200f);
        CheckCalc(p, "payload.valueD*payload.valueFB60", 7200d);
        CheckCalc(p, "payload.valueJ*payload.valueFB60", 7200F);
        CheckCalc(p, "payload.valueI*payload.valueFB60", 7200F);

        // right is a long
        CheckCalc(p, "payload.valueSB*60L", 7200L);
        CheckCalc(p, "payload.valueBB*60L", 7200L);
        CheckCalc(p, "payload.valueFB*60L", 7200f);
        CheckCalc(p, "payload.valueDB*60L", 7200d);
        CheckCalc(p, "payload.valueJB*60L", 7200L);
        CheckCalc(p, "payload.valueIB*60L", 7200L);

        CheckCalc(p, "payload.valueS*60L", 7200L);
        CheckCalc(p, "payload.valueB*60L", 7200L);
        CheckCalc(p, "payload.valueF*60L", 7200f);
        CheckCalc(p, "payload.valueD*60L", 7200d);
        CheckCalc(p, "payload.valueJ*60L", 7200L);
        CheckCalc(p, "payload.valueI*60L", 7200L);

        CheckCalc(p, "payload.valueSB*payload.valueJB60", 7200L);
        CheckCalc(p, "payload.valueBB*payload.valueJB60", 7200L);
        CheckCalc(p, "payload.valueFB*payload.valueJB60", 7200f);
        CheckCalc(p, "payload.valueDB*payload.valueJB60", 7200d);
        CheckCalc(p, "payload.valueJB*payload.valueJB60", 7200L);
        CheckCalc(p, "payload.valueIB*payload.valueJB60", 7200L);

        CheckCalc(p, "payload.valueS*payload.valueJB60", 7200L);
        CheckCalc(p, "payload.valueB*payload.valueJB60", 7200L);
        CheckCalc(p, "payload.valueF*payload.valueJB60", 7200f);
        CheckCalc(p, "payload.valueD*payload.valueJB60", 7200d);
        CheckCalc(p, "payload.valueJ*payload.valueJB60", 7200L);
        CheckCalc(p, "payload.valueI*payload.valueJB60", 7200L);

        // right is an int
        CheckCalc(p, "payload.valueSB*60", 7200);
        CheckCalc(p, "payload.valueBB*60", 7200);
        CheckCalc(p, "payload.valueFB*60", 7200f);
        CheckCalc(p, "payload.valueDB*60", 7200d);
        CheckCalc(p, "payload.valueJB*60", 7200L);
        CheckCalc(p, "payload.valueIB*60", 7200);

        CheckCalc(p, "payload.valueS*60", 7200);
        CheckCalc(p, "payload.valueB*60", 7200);
        CheckCalc(p, "payload.valueF*60", 7200f);
        CheckCalc(p, "payload.valueD*60", 7200d);
        CheckCalc(p, "payload.valueJ*60", 7200L);
        CheckCalc(p, "payload.valueI*60", 7200);

        CheckCalc(p, "payload.valueSB*payload.valueIB60", 7200);
        CheckCalc(p, "payload.valueBB*payload.valueIB60", 7200);
        CheckCalc(p, "payload.valueFB*payload.valueIB60", 7200f);
        CheckCalc(p, "payload.valueDB*payload.valueIB60", 7200d);
        CheckCalc(p, "payload.valueJB*payload.valueIB60", 7200L);
        CheckCalc(p, "payload.valueIB*payload.valueIB60", 7200);

        CheckCalc(p, "payload.valueS*payload.valueIB60", 7200);
        CheckCalc(p, "payload.valueB*payload.valueIB60", 7200);
        CheckCalc(p, "payload.valueF*payload.valueIB60", 7200f);
        CheckCalc(p, "payload.valueD*payload.valueIB60", 7200d);
        CheckCalc(p, "payload.valueJ*payload.valueIB60", 7200L);
        CheckCalc(p, "payload.valueI*payload.valueIB60", 7200);

        // right is a short
        CheckCalc(p, "payload.valueSB*payload.valueS20", 2400);
        CheckCalc(p, "payload.valueBB*payload.valueS20", 2400);
        CheckCalc(p, "payload.valueFB*payload.valueS20", 2400f);
        CheckCalc(p, "payload.valueDB*payload.valueS20", 2400d);
        CheckCalc(p, "payload.valueJB*payload.valueS20", 2400L);
        CheckCalc(p, "payload.valueIB*payload.valueS20", 2400);

        CheckCalc(p, "payload.valueS*payload.valueS20", 2400);
        CheckCalc(p, "payload.valueB*payload.valueS20", 2400);
        CheckCalc(p, "payload.valueF*payload.valueS20", 2400f);
        CheckCalc(p, "payload.valueD*payload.valueS20", 2400d);
        CheckCalc(p, "payload.valueJ*payload.valueS20", 2400L);
        CheckCalc(p, "payload.valueI*payload.valueS20", 2400);

        CheckCalc(p, "payload.valueSB*payload.valueSB20", 2400);
        CheckCalc(p, "payload.valueBB*payload.valueSB20", 2400);
        CheckCalc(p, "payload.valueFB*payload.valueSB20", 2400f);
        CheckCalc(p, "payload.valueDB*payload.valueSB20", 2400d);
        CheckCalc(p, "payload.valueJB*payload.valueSB20", 2400L);
        CheckCalc(p, "payload.valueIB*payload.valueSB20", 2400);

        CheckCalc(p, "payload.valueS*payload.valueSB20", 2400);
        CheckCalc(p, "payload.valueB*payload.valueSB20", 2400);
        CheckCalc(p, "payload.valueF*payload.valueSB20", 2400f);
        CheckCalc(p, "payload.valueD*payload.valueSB20", 2400d);
        CheckCalc(p, "payload.valueJ*payload.valueSB20", 2400L);
        CheckCalc(p, "payload.valueI*payload.valueSB20", 2400);

        // right is a byte
        CheckCalc(p, "payload.valueSB*payload.valueB20", 2400);
        CheckCalc(p, "payload.valueBB*payload.valueB20", 2400);
        CheckCalc(p, "payload.valueFB*payload.valueB20", 2400f);
        CheckCalc(p, "payload.valueDB*payload.valueB20", 2400d);
        CheckCalc(p, "payload.valueJB*payload.valueB20", 2400L);
        CheckCalc(p, "payload.valueIB*payload.valueB20", 2400);

        CheckCalc(p, "payload.valueS*payload.valueB20", 2400);
        CheckCalc(p, "payload.valueB*payload.valueB20", 2400);
        CheckCalc(p, "payload.valueF*payload.valueB20", 2400f);
        CheckCalc(p, "payload.valueD*payload.valueB20", 2400d);
        CheckCalc(p, "payload.valueJ*payload.valueB20", 2400L);
        CheckCalc(p, "payload.valueI*payload.valueB20", 2400);

        CheckCalc(p, "payload.valueSB*payload.valueBB20", 2400);
        CheckCalc(p, "payload.valueBB*payload.valueBB20", 2400);
        CheckCalc(p, "payload.valueFB*payload.valueBB20", 2400f);
        CheckCalc(p, "payload.valueDB*payload.valueBB20", 2400d);
        CheckCalc(p, "payload.valueJB*payload.valueBB20", 2400L);
        CheckCalc(p, "payload.valueIB*payload.valueBB20", 2400);

        CheckCalc(p, "payload.valueS*payload.valueBB20", 2400);
        CheckCalc(p, "payload.valueB*payload.valueBB20", 2400);
        CheckCalc(p, "payload.valueF*payload.valueBB20", 2400f);
        CheckCalc(p, "payload.valueD*payload.valueBB20", 2400d);
        CheckCalc(p, "payload.valueJ*payload.valueBB20", 2400L);
        CheckCalc(p, "payload.valueI*payload.valueBB20", 2400);
    }

    [Fact]
    public void OpModulus_MixedNumberTypes()
    {
        var p = new PayloadX();

        // This is what you had to do before the changes in order for it to compile:
        // expression = parse("(T(java.lang.Double).parseDouble(payload.valueI.toString()))/60D");

        // right is a double
        CheckCalc(p, "payload.valueSB%58D", 4d);
        CheckCalc(p, "payload.valueBB%58D", 4d);
        CheckCalc(p, "payload.valueFB%58D", 4d);
        CheckCalc(p, "payload.valueDB%58D", 4d);
        CheckCalc(p, "payload.valueJB%58D", 4d);
        CheckCalc(p, "payload.valueIB%58D", 4d);

        CheckCalc(p, "payload.valueS%58D", 4d);
        CheckCalc(p, "payload.valueB%58D", 4d);
        CheckCalc(p, "payload.valueF%58D", 4d);
        CheckCalc(p, "payload.valueD%58D", 4d);
        CheckCalc(p, "payload.valueJ%58D", 4d);
        CheckCalc(p, "payload.valueI%58D", 4d);

        CheckCalc(p, "payload.valueSB%payload.valueDB58", 4d);
        CheckCalc(p, "payload.valueBB%payload.valueDB58", 4d);
        CheckCalc(p, "payload.valueFB%payload.valueDB58", 4d);
        CheckCalc(p, "payload.valueDB%payload.valueDB58", 4d);
        CheckCalc(p, "payload.valueJB%payload.valueDB58", 4d);
        CheckCalc(p, "payload.valueIB%payload.valueDB58", 4d);

        CheckCalc(p, "payload.valueS%payload.valueDB58", 4d);
        CheckCalc(p, "payload.valueB%payload.valueDB58", 4d);
        CheckCalc(p, "payload.valueF%payload.valueDB58", 4d);
        CheckCalc(p, "payload.valueD%payload.valueDB58", 4d);
        CheckCalc(p, "payload.valueJ%payload.valueDB58", 4d);
        CheckCalc(p, "payload.valueI%payload.valueDB58", 4d);

        // right is a float
        CheckCalc(p, "payload.valueSB%58F", 4F);
        CheckCalc(p, "payload.valueBB%58F", 4F);
        CheckCalc(p, "payload.valueFB%58F", 4f);
        CheckCalc(p, "payload.valueDB%58F", 4d);
        CheckCalc(p, "payload.valueJB%58F", 4F);
        CheckCalc(p, "payload.valueIB%58F", 4F);

        CheckCalc(p, "payload.valueS%58F", 4F);
        CheckCalc(p, "payload.valueB%58F", 4F);
        CheckCalc(p, "payload.valueF%58F", 4f);
        CheckCalc(p, "payload.valueD%58F", 4d);
        CheckCalc(p, "payload.valueJ%58F", 4F);
        CheckCalc(p, "payload.valueI%58F", 4F);

        CheckCalc(p, "payload.valueSB%payload.valueFB58", 4F);
        CheckCalc(p, "payload.valueBB%payload.valueFB58", 4F);
        CheckCalc(p, "payload.valueFB%payload.valueFB58", 4f);
        CheckCalc(p, "payload.valueDB%payload.valueFB58", 4d);
        CheckCalc(p, "payload.valueJB%payload.valueFB58", 4F);
        CheckCalc(p, "payload.valueIB%payload.valueFB58", 4F);

        CheckCalc(p, "payload.valueS%payload.valueFB58", 4F);
        CheckCalc(p, "payload.valueB%payload.valueFB58", 4F);
        CheckCalc(p, "payload.valueF%payload.valueFB58", 4f);
        CheckCalc(p, "payload.valueD%payload.valueFB58", 4d);
        CheckCalc(p, "payload.valueJ%payload.valueFB58", 4F);
        CheckCalc(p, "payload.valueI%payload.valueFB58", 4F);

        // right is a long
        CheckCalc(p, "payload.valueSB%58L", 4L);
        CheckCalc(p, "payload.valueBB%58L", 4L);
        CheckCalc(p, "payload.valueFB%58L", 4f);
        CheckCalc(p, "payload.valueDB%58L", 4d);
        CheckCalc(p, "payload.valueJB%58L", 4L);
        CheckCalc(p, "payload.valueIB%58L", 4L);

        CheckCalc(p, "payload.valueS%58L", 4L);
        CheckCalc(p, "payload.valueB%58L", 4L);
        CheckCalc(p, "payload.valueF%58L", 4f);
        CheckCalc(p, "payload.valueD%58L", 4d);
        CheckCalc(p, "payload.valueJ%58L", 4L);
        CheckCalc(p, "payload.valueI%58L", 4L);

        CheckCalc(p, "payload.valueSB%payload.valueJB58", 4L);
        CheckCalc(p, "payload.valueBB%payload.valueJB58", 4L);
        CheckCalc(p, "payload.valueFB%payload.valueJB58", 4f);
        CheckCalc(p, "payload.valueDB%payload.valueJB58", 4d);
        CheckCalc(p, "payload.valueJB%payload.valueJB58", 4L);
        CheckCalc(p, "payload.valueIB%payload.valueJB58", 4L);

        CheckCalc(p, "payload.valueS%payload.valueJB58", 4L);
        CheckCalc(p, "payload.valueB%payload.valueJB58", 4L);
        CheckCalc(p, "payload.valueF%payload.valueJB58", 4f);
        CheckCalc(p, "payload.valueD%payload.valueJB58", 4d);
        CheckCalc(p, "payload.valueJ%payload.valueJB58", 4L);
        CheckCalc(p, "payload.valueI%payload.valueJB58", 4L);

        // right is an int
        CheckCalc(p, "payload.valueSB%58", 4);
        CheckCalc(p, "payload.valueBB%58", 4);
        CheckCalc(p, "payload.valueFB%58", 4f);
        CheckCalc(p, "payload.valueDB%58", 4d);
        CheckCalc(p, "payload.valueJB%58", 4L);
        CheckCalc(p, "payload.valueIB%58", 4);

        CheckCalc(p, "payload.valueS%58", 4);
        CheckCalc(p, "payload.valueB%58", 4);
        CheckCalc(p, "payload.valueF%58", 4f);
        CheckCalc(p, "payload.valueD%58", 4d);
        CheckCalc(p, "payload.valueJ%58", 4L);
        CheckCalc(p, "payload.valueI%58", 4);

        CheckCalc(p, "payload.valueSB%payload.valueIB58", 4);
        CheckCalc(p, "payload.valueBB%payload.valueIB58", 4);
        CheckCalc(p, "payload.valueFB%payload.valueIB58", 4f);
        CheckCalc(p, "payload.valueDB%payload.valueIB58", 4d);
        CheckCalc(p, "payload.valueJB%payload.valueIB58", 4L);
        CheckCalc(p, "payload.valueIB%payload.valueIB58", 4);

        CheckCalc(p, "payload.valueS%payload.valueIB58", 4);
        CheckCalc(p, "payload.valueB%payload.valueIB58", 4);
        CheckCalc(p, "payload.valueF%payload.valueIB58", 4f);
        CheckCalc(p, "payload.valueD%payload.valueIB58", 4d);
        CheckCalc(p, "payload.valueJ%payload.valueIB58", 4L);
        CheckCalc(p, "payload.valueI%payload.valueIB58", 4);

        // right is a short
        CheckCalc(p, "payload.valueSB%payload.valueS18", 12);
        CheckCalc(p, "payload.valueBB%payload.valueS18", 12);
        CheckCalc(p, "payload.valueFB%payload.valueS18", 12f);
        CheckCalc(p, "payload.valueDB%payload.valueS18", 12d);
        CheckCalc(p, "payload.valueJB%payload.valueS18", 12L);
        CheckCalc(p, "payload.valueIB%payload.valueS18", 12);

        CheckCalc(p, "payload.valueS%payload.valueS18", 12);
        CheckCalc(p, "payload.valueB%payload.valueS18", 12);
        CheckCalc(p, "payload.valueF%payload.valueS18", 12f);
        CheckCalc(p, "payload.valueD%payload.valueS18", 12d);
        CheckCalc(p, "payload.valueJ%payload.valueS18", 12L);
        CheckCalc(p, "payload.valueI%payload.valueS18", 12);

        CheckCalc(p, "payload.valueSB%payload.valueSB18", 12);
        CheckCalc(p, "payload.valueBB%payload.valueSB18", 12);
        CheckCalc(p, "payload.valueFB%payload.valueSB18", 12f);
        CheckCalc(p, "payload.valueDB%payload.valueSB18", 12d);
        CheckCalc(p, "payload.valueJB%payload.valueSB18", 12L);
        CheckCalc(p, "payload.valueIB%payload.valueSB18", 12);

        CheckCalc(p, "payload.valueS%payload.valueSB18", 12);
        CheckCalc(p, "payload.valueB%payload.valueSB18", 12);
        CheckCalc(p, "payload.valueF%payload.valueSB18", 12f);
        CheckCalc(p, "payload.valueD%payload.valueSB18", 12d);
        CheckCalc(p, "payload.valueJ%payload.valueSB18", 12L);
        CheckCalc(p, "payload.valueI%payload.valueSB18", 12);

        // right is a byte
        CheckCalc(p, "payload.valueSB%payload.valueB18", 12);
        CheckCalc(p, "payload.valueBB%payload.valueB18", 12);
        CheckCalc(p, "payload.valueFB%payload.valueB18", 12f);
        CheckCalc(p, "payload.valueDB%payload.valueB18", 12d);
        CheckCalc(p, "payload.valueJB%payload.valueB18", 12L);
        CheckCalc(p, "payload.valueIB%payload.valueB18", 12);

        CheckCalc(p, "payload.valueS%payload.valueB18", 12);
        CheckCalc(p, "payload.valueB%payload.valueB18", 12);
        CheckCalc(p, "payload.valueF%payload.valueB18", 12f);
        CheckCalc(p, "payload.valueD%payload.valueB18", 12d);
        CheckCalc(p, "payload.valueJ%payload.valueB18", 12L);
        CheckCalc(p, "payload.valueI%payload.valueB18", 12);

        CheckCalc(p, "payload.valueSB%payload.valueBB18", 12);
        CheckCalc(p, "payload.valueBB%payload.valueBB18", 12);
        CheckCalc(p, "payload.valueFB%payload.valueBB18", 12f);
        CheckCalc(p, "payload.valueDB%payload.valueBB18", 12d);
        CheckCalc(p, "payload.valueJB%payload.valueBB18", 12L);
        CheckCalc(p, "payload.valueIB%payload.valueBB18", 12);

        CheckCalc(p, "payload.valueS%payload.valueBB18", 12);
        CheckCalc(p, "payload.valueB%payload.valueBB18", 12);
        CheckCalc(p, "payload.valueF%payload.valueBB18", 12f);
        CheckCalc(p, "payload.valueD%payload.valueBB18", 12d);
        CheckCalc(p, "payload.valueJ%payload.valueBB18", 12L);
        CheckCalc(p, "payload.valueI%payload.valueBB18", 12);
    }

    [Fact]
    public void OpMultiply()
    {
        _expression = Parse("2*2");
        Assert.Equal(4, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(4, _expression.GetValue());

        _expression = Parse("2L*2L");
        Assert.Equal(4L, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(4L, _expression.GetValue());

        _expression = Parse("2.0f*2.0f");
        Assert.Equal(4F, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(4F, _expression.GetValue());

        _expression = Parse("3.0d*4.0d");
        Assert.Equal(12D, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(12D, _expression.GetValue());

        _expression = Parse("T(float).Parse('2.0')*6");
        Assert.Equal(12.0f, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(12.0f, _expression.GetValue());

        _expression = Parse("T(Single).Parse('8.0')*T(float).Parse('3.0')");
        Assert.Equal(24.0f, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(24.0f, _expression.GetValue());

        _expression = Parse("11L*T(long).Parse('4')");
        Assert.Equal(44L, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(44L, _expression.GetValue());

        _expression = Parse("T(long).Parse('9')*6");
        Assert.Equal(54L, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(54L, _expression.GetValue());

        _expression = Parse("T(long).Parse('4')*T(long).Parse('3')");
        Assert.Equal(12L, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(12L, _expression.GetValue());

        _expression = Parse("8L*T(long).Parse('2')");
        Assert.Equal(16L, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(16L, _expression.GetValue());

        _expression = Parse("T(float).Parse('8.0')*-T(Single).Parse('3.0')");
        Assert.Equal(-24.0f, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(-24.0f, _expression.GetValue());
    }

    [Fact]
    public void OpDivide()
    {
        _expression = Parse("2/2");
        Assert.Equal(1, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(1, _expression.GetValue());

        _expression = Parse("2L/2L");
        Assert.Equal(1L, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(1L, _expression.GetValue());

        _expression = Parse("2.0f/2.0f");
        Assert.Equal(1f, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(1f, _expression.GetValue());

        _expression = Parse("4.0d/4.0d");
        Assert.Equal(1d, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(1d, _expression.GetValue());

        _expression = Parse("T(float).Parse('6.0')/2");
        Assert.Equal(3.0f, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(3.0f, _expression.GetValue());

        _expression = Parse("T(Single).Parse('8.0')/T(float).Parse('2.0')");
        Assert.Equal(4.0f, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(4.0f, _expression.GetValue());

        _expression = Parse("12L/T(long).Parse('4')");
        Assert.Equal(3L, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(3L, _expression.GetValue());

        _expression = Parse("T(long).Parse('44')/11");
        Assert.Equal(4L, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(4L, _expression.GetValue());

        _expression = Parse("T(long).Parse('4')/T(long).Parse('2')");
        Assert.Equal(2L, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(2L, _expression.GetValue());

        _expression = Parse("8L/T(long).Parse('2')");
        Assert.Equal(4L, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(4L, _expression.GetValue());

        _expression = Parse("T(float).Parse('8.0')/-T(Single).Parse('4.0')");
        Assert.Equal(-2.0f, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(-2.0f, _expression.GetValue());
    }

    [Fact]
    public void OpModulus_12041()
    {
        _expression = Parse("2%2");
        Assert.Equal(0, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(0, _expression.GetValue());

        _expression = Parse("Payload%2==0");
        Assert.True(_expression.GetValue<bool>(new GenericMessageTestHelper<int>(4)));
        Assert.False(_expression.GetValue<bool>(new GenericMessageTestHelper<int>(5)));
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>(new GenericMessageTestHelper<int>(4)));
        Assert.False(_expression.GetValue<bool>(new GenericMessageTestHelper<int>(5)));
        AssertCanCompile(_expression);

        _expression = Parse("8%3");
        Assert.Equal(2, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(2, _expression.GetValue());

        _expression = Parse("17L%5L");
        Assert.Equal(2L, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(2L, _expression.GetValue());

        _expression = Parse("3.0f%2.0f");
        Assert.Equal(1.0f, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(1.0f, _expression.GetValue());

        _expression = Parse("3.0d%4.0d");
        Assert.Equal(3.0d, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(3.0d, _expression.GetValue());

        _expression = Parse("T(float).Parse('6.0')%2");
        Assert.Equal(0.0f, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(0.0f, _expression.GetValue());

        _expression = Parse("T(Single).Parse('6.0')%4");
        Assert.Equal(2.0f, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(2.0f, _expression.GetValue());

        _expression = Parse("T(Single).Parse('8.0')%T(float).Parse('3.0')");
        Assert.Equal(2.0f, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(2.0f, _expression.GetValue());

        _expression = Parse("13L%T(long).Parse('4')");
        Assert.Equal(1L, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(1L, _expression.GetValue());

        _expression = Parse("T(Int64).Parse('44')%12");
        Assert.Equal(8L, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(8L, _expression.GetValue());

        _expression = Parse("T(long).Parse('9')%T(long).Parse('2')");
        Assert.Equal(1L, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(1L, _expression.GetValue());

        _expression = Parse("7L%T(long).Parse('2')");
        Assert.Equal(1L, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(1L, _expression.GetValue());

        _expression = Parse("T(float).Parse('9.0')%-T(float).Parse('4.0')");
        Assert.Equal(1.0f, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(1.0f, _expression.GetValue());
    }

    [Fact]
    public void FailsWhenSettingContextForExpression_SPR12326()
    {
        var parser = new SpelExpressionParser(new SpelParserOptions(SpelCompilerMode.OFF));
        var person = new Person3("foo", 1);
        var expression = parser.ParseRaw("#it?.Age?.Equals([0])") as SpelExpression;
        var context = new StandardEvaluationContext(new object[] { 1 });
        context.SetVariable("it", person);
        expression.EvaluationContext = context;
        Assert.True(expression.GetValue<bool>());

        // This will trigger compilation (second usage)
        Assert.True(expression.GetValue<bool>());
        context.SetVariable("it", null);
        Assert.Null(expression.GetValue());

        AssertCanCompile(expression);

        context.SetVariable("it", person);
        Assert.True(expression.GetValue<bool>());
        context.SetVariable("it", null);
        Assert.Null(expression.GetValue());
    }

    /**
        * Test variants of using T(...) and static/non-static method/property/field references.
        */
    [Fact]
    public void ConstructorReference_SPR13781()
    {
        // Static const field access on a T() referenced type
        _expression = _parser.ParseExpression("T(Int32).MaxValue");
        Assert.Equal(2147483647, _expression.GetValue<int>());
        AssertCanCompile(_expression);
        Assert.Equal(2147483647, _expression.GetValue<int>());

        // Static field access on a T() referenced type
        _expression = _parser.ParseExpression("T(String).Empty");
        Assert.Equal(string.Empty, _expression.GetValue<string>());
        AssertCanCompile(_expression);
        Assert.Equal(string.Empty, _expression.GetValue<string>());

        // Property access on an instance of System.Type object
        _expression = _parser.ParseExpression("T(String).Name");
        Assert.Equal("String", _expression.GetValue<string>());
        AssertCanCompile(_expression);
        Assert.Equal("String", _expression.GetValue<string>());

        // Now the type reference isn't on the stack, and needs loading
        var context = new StandardEvaluationContext(typeof(string));
        _expression = _parser.ParseExpression("Name");
        Assert.Equal("String", _expression.GetValue<string>(context));
        AssertCanCompile(_expression);
        Assert.Equal("String", _expression.GetValue<string>(context));

        _expression = _parser.ParseExpression("T(String).get_Name()");
        Assert.Equal("String", _expression.GetValue<string>());
        AssertCanCompile(_expression);
        Assert.Equal("String", _expression.GetValue<string>());

        // These tests below verify that the chain of static accesses (either method/property or field)
        // leave the right thing on top of the stack for processing by any outer consuming code.
        // Here the consuming code is the String.valueOf() function.  If the wrong thing were on
        // the stack (for example if the compiled code for static methods wasn't popping the
        // previous thing off the stack) the valueOf() would operate on the wrong value.
        _expression = _parser.ParseExpression("T(String).Format('Format:{0}', T(String).Name.Format('Format:{0}', 1))");
        Assert.Equal("Format:Format:1", _expression.GetValue<string>());
        AssertCanCompile(_expression);
        Assert.Equal("Format:Format:1", _expression.GetValue<string>());

        _expression = _parser.ParseExpression("T(String).Format('Format:{0}', T(Steeltoe.Common.Expression.Internal.Spring.SpelCompilationCoverageTests$StaticsHelper).MethodA().MethodA().MethodB())");
        Assert.Equal("Format:mb", _expression.GetValue<string>());
        AssertCanCompile(_expression);
        Assert.Equal("Format:mb", _expression.GetValue<string>());

        _expression = _parser.ParseExpression("T(String).Format('Format:{0}', T(Steeltoe.Common.Expression.Internal.Spring.SpelCompilationCoverageTests$StaticsHelper).Fielda.Fielda.Fieldb)");
        Assert.Equal("Format:fb", _expression.GetValue<string>());
        AssertCanCompile(_expression);
        Assert.Equal("Format:fb", _expression.GetValue<string>());

        _expression = _parser.ParseExpression("T(String).Format('Format:{0}', T(Steeltoe.Common.Expression.Internal.Spring.SpelCompilationCoverageTests$StaticsHelper).PropertyA.PropertyA.PropertyB)");
        Assert.Equal("Format:pb", _expression.GetValue<string>());
        AssertCanCompile(_expression);
        Assert.Equal("Format:pb", _expression.GetValue<string>());

        _expression = _parser.ParseExpression("T(String).Format('Format:{0}', T(Steeltoe.Common.Expression.Internal.Spring.SpelCompilationCoverageTests$StaticsHelper).Fielda.MethodA().PropertyA.Fieldb)");
        Assert.Equal("Format:fb", _expression.GetValue<string>());
        AssertCanCompile(_expression);
        Assert.Equal("Format:fb", _expression.GetValue<string>());

        _expression = _parser.ParseExpression("T(String).Format('Format:{0}', Fielda.Fieldb)");
        Assert.Equal("Format:fb", _expression.GetValue<string>(StaticsHelper.sh));
        AssertCanCompile(_expression);
        Assert.Equal("Format:fb", _expression.GetValue<string>(StaticsHelper.sh));

        _expression = _parser.ParseExpression("T(String).Format('Format:{0}', PropertyA.PropertyB)");
        Assert.Equal("Format:pb", _expression.GetValue<string>(StaticsHelper.sh));
        AssertCanCompile(_expression);
        Assert.Equal("Format:pb", _expression.GetValue<string>(StaticsHelper.sh));

        _expression = _parser.ParseExpression("T(String).Format('Format:{0}', MethodA().MethodB())");
        Assert.Equal("Format:mb", _expression.GetValue<string>(StaticsHelper.sh));
        AssertCanCompile(_expression);
        Assert.Equal("Format:mb", _expression.GetValue<string>(StaticsHelper.sh));
    }

    [Fact]
    public void ConstructorReference_SPR12326()
    {
        var type = GetType().FullName;
        var prefix = $"new {type}$Obj";

        _expression = _parser.ParseExpression($"{prefix}([0])");
        Assert.Equal("test", ((Obj)_expression.GetValue(new object[] { "test" })).Param1);
        AssertCanCompile(_expression);
        Assert.Equal("test", ((Obj)_expression.GetValue(new object[] { "test" })).Param1);

        _expression = _parser.ParseExpression($"{prefix}2('foo','bar').Output");
        Assert.Equal("foobar", _expression.GetValue<string>());
        AssertCanCompile(_expression);
        Assert.Equal("foobar", _expression.GetValue<string>());

        _expression = _parser.ParseExpression($"{prefix}2('foo').Output");
        Assert.Equal("foo", _expression.GetValue<string>());
        AssertCanCompile(_expression);
        Assert.Equal("foo", _expression.GetValue<string>());

        _expression = _parser.ParseExpression($"{prefix}2().Output");
        Assert.Equal(string.Empty, _expression.GetValue<string>());
        AssertCanCompile(_expression);
        Assert.Equal(string.Empty, _expression.GetValue<string>());

        _expression = _parser.ParseExpression($"{prefix}3(1,2,3).Output");
        Assert.Equal("123", _expression.GetValue<string>());
        AssertCanCompile(_expression);
        Assert.Equal("123", _expression.GetValue<string>());

        _expression = _parser.ParseExpression($"{prefix}3(1).Output");
        Assert.Equal("1", _expression.GetValue<string>());
        AssertCanCompile(_expression);
        Assert.Equal("1", _expression.GetValue<string>());

        _expression = _parser.ParseExpression($"{prefix}3().Output");
        Assert.Equal(string.Empty, _expression.GetValue<string>());
        AssertCanCompile(_expression);
        Assert.Equal(string.Empty, _expression.GetValue<string>());

        _expression = _parser.ParseExpression($"{prefix}3('abc',5.0f,1,2,3).Output");
        Assert.Equal("abc:5:123", _expression.GetValue<string>());
        AssertCanCompile(_expression);
        Assert.Equal("abc:5:123", _expression.GetValue<string>());

        _expression = _parser.ParseExpression($"{prefix}3('abc',5.0f,1).Output");
        Assert.Equal("abc:5:1", _expression.GetValue<string>());
        AssertCanCompile(_expression);
        Assert.Equal("abc:5:1", _expression.GetValue<string>());

        _expression = _parser.ParseExpression($"{prefix}3('abc',5.0f).Output");
        Assert.Equal("abc:5:", _expression.GetValue<string>());
        AssertCanCompile(_expression);
        Assert.Equal("abc:5:", _expression.GetValue<string>());

        _expression = _parser.ParseExpression($"{prefix}4(#root).Output");
        Assert.Equal("123", _expression.GetValue<string>(new[] { 1, 2, 3 }));
        AssertCanCompile(_expression);
        Assert.Equal("123", _expression.GetValue<string>(new[] { 1, 2, 3 }));
    }

    [Fact]
    public void MethodReferenceMissingCastAndRootObjectAccessing_SPR12326()
    {
        // Need boxing code on the 1 so that toString() can be called
        _expression = _parser.ParseExpression("1.ToString()");
        Assert.Equal("1", _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal("1", _expression.GetValue());

        _expression = _parser.ParseExpression("#it?.Age.Equals([0])");
        var person = new Person(1);
        var context = new StandardEvaluationContext(new object[] { person.Age });
        context.SetVariable("it", person);
        Assert.True(_expression.GetValue<bool>(context));
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>(context));

        var parser2 = new SpelExpressionParser(new SpelParserOptions(SpelCompilerMode.IMMEDIATE));
        var ex = parser2.ParseExpression("#it?.Age.Equals([0])");
        context = new StandardEvaluationContext(new object[] { person.Age });
        context.SetVariable("it", person);
        Assert.True(ex.GetValue<bool>(context));
        Assert.True(ex.GetValue<bool>(context));

        var person2 = new PersonInOtherPackage(1);
        ex = parser2.ParseRaw("#it?.Age.Equals([0])");
        context = new StandardEvaluationContext(new object[] { person2.Age });
        context.SetVariable("it", person2);
        Assert.True(ex.GetValue<bool>(context));
        Assert.True(ex.GetValue<bool>(context));

        ex = parser2.ParseRaw("#it?.Age.Equals([0])");
        context = new StandardEvaluationContext(new object[] { person2.Age });
        context.SetVariable("it", person2);
        Assert.True(ex.GetValue<bool>(context));
        Assert.True(ex.GetValue<bool>(context));
    }

    [Fact]
    public void ConstructorReference()
    {
        // There is no String('') in .NET
        // _expression = parser.ParseExpression("new String('123')");
        // Assert.Equal("123", _expression.GetValue());
        // AssertCanCompile(_expression);
        // Assert.Equal("123", _expression.GetValue());
        var testclass8 = "Steeltoe.Common.Expression.Internal.Spring.SpelCompilationCoverageTests$TestClass8";

        // multi arg ctor that includes primitives
        _expression = _parser.ParseExpression($"new {testclass8}(42,'123',4.0d,True)");
        Assert.IsType<TestClass8>(_expression.GetValue());
        AssertCanCompile(_expression);
        var o = _expression.GetValue();
        Assert.IsType<TestClass8>(o);
        var tc8 = (TestClass8)o;
        Assert.Equal(42, tc8.I);
        Assert.Equal("123", tc8.S);
        Assert.Equal(4.0d, tc8.D);
        Assert.True(tc8.Z);

        // pass primitive to reference type ctor
        _expression = _parser.ParseExpression($"new {testclass8}(42)");
        Assert.IsType<TestClass8>(_expression.GetValue());
        AssertCanCompile(_expression);
        o = _expression.GetValue();
        Assert.IsType<TestClass8>(o);
        tc8 = (TestClass8)o;
        Assert.Equal(42, tc8.I);

        // private class, can't compile it
        var testclass9 = "Steeltoe.Common.Expression.Internal.Spring.SpelCompilationCoverageTests$TestClass9";
        _expression = _parser.ParseExpression($"new {testclass9}(42)");
        Assert.IsType<TestClass9>(_expression.GetValue());
        AssertCantCompile(_expression);
    }

    [Fact]
    public void MethodReferenceReflectiveMethodSelectionWithVarargs()
    {
        var tc = new TestClass10();

        // Should call the non varargs version of Concat1
        // (which causes the '::' prefix in test output)
        _expression = _parser.ParseExpression("Concat1('test')");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal("::test", tc.S);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal("::test", tc.S);
        tc.Reset();

        // This will call the varargs Concat1 with an empty array
        _expression = _parser.ParseExpression("Concat1()");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal(string.Empty, tc.S);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal(string.Empty, tc.S);
        tc.Reset();

        // Should call the non varargs version of Concat2
        // (which causes the '::' prefix in test output)
        _expression = _parser.ParseExpression("Concat2('test')");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal("::test", tc.S);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal("::test", tc.S);
        tc.Reset();

        // This will call the varargs Concat2 with an empty array
        _expression = _parser.ParseExpression("Concat2()");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal(string.Empty, tc.S);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal(string.Empty, tc.S);
        tc.Reset();
    }

    [Fact]
    public void MethodReferenceVarargs()
    {
        var tc = new TestClass5();

        // varargs string
        _expression = _parser.ParseExpression("Eleven()");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal(string.Empty, tc.S);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal(string.Empty, tc.S);
        tc.Reset();

        // varargs string
        _expression = _parser.ParseExpression("Eleven('aaa')");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal("aaa", tc.S);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal("aaa", tc.S);
        tc.Reset();

        // varargs string
        _expression = _parser.ParseExpression("Eleven(StringArray)");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal("aaabbbccc", tc.S);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal("aaabbbccc", tc.S);
        tc.Reset();

        // varargs string
        _expression = _parser.ParseExpression("Eleven('aaa','bbb','ccc')");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal("aaabbbccc", tc.S);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal("aaabbbccc", tc.S);
        tc.Reset();

        _expression = _parser.ParseExpression("Sixteen('aaa','bbb','ccc')");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal("aaabbbccc", tc.S);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal("aaabbbccc", tc.S);
        tc.Reset();

        _expression = _parser.ParseExpression("Twelve(1,2,3)");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal(6, tc.I);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal(6, tc.I);
        tc.Reset();

        _expression = _parser.ParseExpression("Twelve(1)");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal(1, tc.I);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal(1, tc.I);
        tc.Reset();

        _expression = _parser.ParseExpression("Thirteen('aaa','bbb','ccc')");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal("aaa::bbbccc", tc.S);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal("aaa::bbbccc", tc.S);
        tc.Reset();

        _expression = _parser.ParseExpression("Thirteen('aaa')");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal("aaa::", tc.S);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal("aaa::", tc.S);
        tc.Reset();

        _expression = _parser.ParseExpression("Fourteen('aaa',StringArray,StringArray)");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal("aaa::{aaabbbccc}{aaabbbccc}", tc.S);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal("aaa::{aaabbbccc}{aaabbbccc}", tc.S);
        tc.Reset();

        _expression = _parser.ParseExpression("Fifteen('aaa',IntArray,IntArray)");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal("aaa::{112233}{112233}", tc.S);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal("aaa::{112233}{112233}", tc.S);
        tc.Reset();

        _expression = _parser.ParseExpression("Arrayz(True,True,False)");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal("TrueTrueFalse", tc.S);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal("TrueTrueFalse", tc.S);
        tc.Reset();

        _expression = _parser.ParseExpression("Arrayz(True)");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal("True", tc.S);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal("True", tc.S);
        tc.Reset();

        _expression = _parser.ParseExpression("Arrays(S1,S2,S3)");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal("123", tc.S);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal("123", tc.S);
        tc.Reset();

        _expression = _parser.ParseExpression("Arrays(S1)");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal("1", tc.S);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal("1", tc.S);
        tc.Reset();

        _expression = _parser.ParseExpression("Arrayd(1.0d,2.0d,3.0d)");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal("123", tc.S);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal("123", tc.S);
        tc.Reset();

        _expression = _parser.ParseExpression("Arrayd(1.0d)");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal("1", tc.S);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal("1", tc.S);
        tc.Reset();

        _expression = _parser.ParseExpression("Arrayj(L1,L2,L3)");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal("123", tc.S);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal("123", tc.S);
        tc.Reset();

        _expression = _parser.ParseExpression("Arrayj(L1)");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal("1", tc.S);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal("1", tc.S);
        tc.Reset();

        _expression = _parser.ParseExpression("Arrayc(C1,C2,C3)");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal("abc", tc.S);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal("abc", tc.S);
        tc.Reset();

        _expression = _parser.ParseExpression("Arrayc(C1)");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal("a", tc.S);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal("a", tc.S);
        tc.Reset();

        _expression = _parser.ParseExpression("Arrayb(B1,B2,B3)");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal("656667", tc.S);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal("656667", tc.S);
        tc.Reset();

        _expression = _parser.ParseExpression("Arrayb(B1)");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal("65", tc.S);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal("65", tc.S);
        tc.Reset();

        _expression = _parser.ParseExpression("Arrayf(F1,F2,F3)");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal("123", tc.S);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal("123", tc.S);
        tc.Reset();

        _expression = _parser.ParseExpression("Arrayf(F1)");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal("1", tc.S);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal("1", tc.S);
        tc.Reset();
    }

    [Fact]
    public void MethodReference()
    {
        var tc = new TestClass5();

        _expression = _parser.ParseExpression("One()");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal(1, tc.I);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal(1, tc.I);
        tc.Reset();

        _expression = _parser.ParseExpression("Two()");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal(1, TestClass5._I);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal(1, TestClass5._I);
        tc.Reset();

        _expression = _parser.ParseExpression("Three()");
        AssertCantCompile(_expression);
        Assert.Equal("hello", _expression.GetValue(tc));
        AssertCanCompile(_expression);
        tc.Reset();
        Assert.Equal("hello", _expression.GetValue(tc));
        tc.Reset();

        _expression = _parser.ParseExpression("Four()");
        AssertCantCompile(_expression);
        Assert.Equal(3277700L, _expression.GetValue(tc));
        AssertCanCompile(_expression);
        tc.Reset();
        Assert.Equal(3277700L, _expression.GetValue(tc));
        tc.Reset();

        // static method, reference type return
        _expression = _parser.ParseExpression("Five()");
        AssertCantCompile(_expression);
        Assert.Equal("hello", _expression.GetValue(tc));
        AssertCanCompile(_expression);
        tc.Reset();
        Assert.Equal("hello", _expression.GetValue(tc));
        tc.Reset();

        // static method, primitive type return
        _expression = _parser.ParseExpression("Six()");
        AssertCantCompile(_expression);
        Assert.Equal(3277700L, _expression.GetValue(tc));
        AssertCanCompile(_expression);
        tc.Reset();
        Assert.Equal(3277700L, _expression.GetValue(tc));
        tc.Reset();

        // non-static method, one parameter of reference type
        _expression = _parser.ParseExpression("Seven(\"foo\")");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal("foo", tc.S);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal("foo", tc.S);
        tc.Reset();

        // static method, one parameter of reference type
        _expression = _parser.ParseExpression("Eight(\"bar\")");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal("bar", TestClass5._S);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal("bar", TestClass5._S);
        tc.Reset();

        // non-static method, one parameter of primitive type
        _expression = _parser.ParseExpression("Nine(231)");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal(231, tc.I);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal(231, tc.I);
        tc.Reset();

        // static method, one parameter of reference type
        _expression = _parser.ParseExpression("Ten(111)");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal(111, TestClass5._I);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal(111, TestClass5._I);
        tc.Reset();

        // method that gets type converted parameters

        // Converting from an int to a string
        _expression = _parser.ParseExpression("Seven(123)");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal("123", tc.S);
        AssertCantCompile(_expression); // Uncompilable as argument conversion is occurring
        tc.Reset();

        var expression = _parser.ParseExpression("'abcd'.Substring(Index1,Index2)");
        var resultI = expression.GetValue<string>(new TestClass1());
        AssertCanCompile(expression);
        var resultC = expression.GetValue<string>(new TestClass1());
        Assert.Equal("bcd", resultI);
        Assert.Equal("bcd", resultC);

        // Converting from an int to a Number
        _expression = _parser.ParseExpression("TakeNumber(123)");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal("123", tc.S);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal("123", tc.S);
        tc.Reset();

        // Passing a subtype
        _expression = _parser.ParseExpression("TakeNumber(T(int).Parse('42'))");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal("42", tc.S);
        AssertCanCompile(_expression);
        tc.Reset();
        _expression.GetValue(tc);
        Assert.Equal("42", tc.S);
        tc.Reset();

        _expression = _parser.ParseExpression("TakeString(T(int).Parse('42'))");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal("42", tc.S);
        AssertCantCompile(_expression);
        tc.Reset();
        AssertCantCompile(_expression);
    }

    [Fact]
    public void ErrorHandling()
    {
        var tc = new TestClass5();

        // changing target
        // from primitive array to reference type array
        var intss = new[] { 1, 2, 3 };
        var strings = new[] { "a", "b", "c" };
        _expression = _parser.ParseExpression("[1]");
        Assert.Equal(2, _expression.GetValue(intss));
        AssertCanCompile(_expression);
        Assert.Throws<SpelEvaluationException>(() => _expression.GetValue(strings));
        SpelCompiler.RevertToInterpreted(_expression);
        Assert.Equal("b", _expression.GetValue(strings));
        AssertCanCompile(_expression);
        Assert.Equal("b", _expression.GetValue(strings));

        tc.Field = "foo";
        _expression = _parser.ParseExpression("Seven(Field)");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal("foo", tc.S);
        AssertCanCompile(_expression);
        tc.Reset();
        tc.Field = "bar";
        _expression.GetValue(tc);

        // method with changing parameter types (change reference type)
        tc.Obj = "foo";
        _expression = _parser.ParseExpression("Seven(Obj)");
        AssertCantCompile(_expression);
        _expression.GetValue(tc);
        Assert.Equal("foo", tc.S);
        AssertCanCompile(_expression);
        tc.Reset();
        tc.Obj = 42;
        Assert.Throws<SpelEvaluationException>(() => _expression.GetValue(tc));

        // method with changing target
        _expression = _parser.ParseExpression("#root.get_Chars(0)");
        Assert.Equal('a', _expression.GetValue("abc"));
        AssertCanCompile(_expression);
        Assert.Throws<SpelEvaluationException>(() => _expression.GetValue(42));
    }

    [Fact]
    public void MethodReference_StaticMethod()
    {
        var expression = _parser.ParseExpression("T(int).Parse('42')");
        var resultI = expression.GetValue<int>(new TestClass1());
        AssertCanCompile(expression);
        var resultC = expression.GetValue<int>(new TestClass1());
        Assert.Equal(42, resultI);
        Assert.Equal(42, resultC);
    }

    [Fact]
    public void MethodReference_LiteralArguments_int()
    {
        var expression = _parser.ParseExpression("'abcd'.Substring(1,3)");
        var resultI = expression.GetValue<string>(new TestClass1());
        AssertCanCompile(expression);
        var resultC = expression.GetValue<string>(new TestClass1());
        Assert.Equal("bcd", resultI);
        Assert.Equal("bcd", resultC);
    }

    [Fact]
    public void MethodReference_SimpleInstanceMethodNoArg()
    {
        var expression = _parser.ParseExpression("ToString()");
        var resultI = expression.GetValue<string>(42);
        AssertCanCompile(expression);
        var resultC = expression.GetValue<string>(42);
        Assert.Equal("42", resultI);
        Assert.Equal("42", resultC);
    }

    [Fact]
    public void MethodReference_SimpleInstanceMethodNoArgReturnPrimitive()
    {
        var expression = _parser.ParseExpression("GetHashCode()");
        var resultI = expression.GetValue<int>(42);
        AssertCanCompile(expression);
        var resultC = expression.GetValue<int>(42);
        Assert.Equal(resultI, resultC);
    }

    [Fact]
    public void MethodReference_SimpleInstanceMethodOneArgReturnPrimitive1()
    {
        var expression = _parser.ParseExpression("IndexOf('b')");
        var resultI = expression.GetValue<int>("abc");
        AssertCanCompile(expression);
        var resultC = expression.GetValue<int>("abc");
        Assert.Equal(1, resultI);
        Assert.Equal(1, resultC);
    }

    [Fact]
    public void MethodReference_SimpleInstanceMethodOneArgReturnPrimitive2()
    {
        var expression = _parser.ParseExpression("get_Chars(2)");
        var resultI = expression.GetValue<char>("abc");
        AssertCanCompile(expression);
        var resultC = expression.GetValue<char>("abc");
        Assert.Equal('c', resultI);
        Assert.Equal('c', resultC);
    }

    [Fact]
    public void CompoundExpression()
    {
        var payload = new Payload();
        _expression = _parser.ParseExpression("DR[0]");
        Assert.Equal("instanceof Two", _expression.GetValue(payload).ToString());
        AssertCanCompile(_expression);
        Assert.Equal("instanceof Two", _expression.GetValue(payload).ToString());

        _expression = _parser.ParseExpression("Holder.Threeee");
        Assert.IsType<Three>(_expression.GetValue(payload));
        AssertCanCompile(_expression);
        Assert.IsType<Three>(_expression.GetValue(payload));

        _expression = _parser.ParseExpression("DR[0]");
        Assert.IsType<Two>(_expression.GetValue(payload));
        AssertCanCompile(_expression);
        Assert.IsType<Two>(_expression.GetValue(payload));

        _expression = _parser.ParseExpression("DR[0].Threeee");
        Assert.IsType<Three>(_expression.GetValue(payload));
        AssertCanCompile(_expression);
        Assert.IsType<Three>(_expression.GetValue(payload));

        _expression = _parser.ParseExpression("DR[0].Threeee.Four");
        Assert.Equal(.04d, _expression.GetValue(payload));
        AssertCanCompile(_expression);
        Assert.Equal(.04d, _expression.GetValue(payload));
    }

    [Fact]
    public void MixingItUp_IndexerOpEqTernary()
    {
        var m = new Dictionary<string, string>
        {
            { "andy", "778" }
        };

        _expression = Parse("['andy']==null?1:2");
        Assert.Equal(2, _expression.GetValue(m));
        AssertCanCompile(_expression);
        Assert.Equal(2, _expression.GetValue(m));
        m.Remove("andy");
        Assert.Equal(1, _expression.GetValue(m));
    }

    [Fact]
    public void PropertyReference()
    {
        var tc = new TestClass6();

        // non static field
        _expression = _parser.ParseExpression("Orange");
        AssertCantCompile(_expression);
        Assert.Equal("value1", _expression.GetValue(tc));
        AssertCanCompile(_expression);
        Assert.Equal("value1", _expression.GetValue(tc));

        // static field
        _expression = _parser.ParseExpression("Apple");
        AssertCantCompile(_expression);
        Assert.Equal("value2", _expression.GetValue(tc));
        AssertCanCompile(_expression);
        Assert.Equal("value2", _expression.GetValue(tc));

        // non static getter
        _expression = _parser.ParseExpression("Banana");
        AssertCantCompile(_expression);
        Assert.Equal("value3", _expression.GetValue(tc));
        AssertCanCompile(_expression);
        Assert.Equal("value3", _expression.GetValue(tc));

        // static getter
        _expression = _parser.ParseExpression("Plum");
        AssertCantCompile(_expression);
        Assert.Equal("value4", _expression.GetValue(tc));
        AssertCanCompile(_expression);
        Assert.Equal("value4", _expression.GetValue(tc));
    }

    [Fact]
    public void Indexer()
    {
        var sss = new[] { "a", "b", "c" };
        var iss = new[] { 8, 9, 10 };
        var ds = new[] { 3.0d, 4.0d, 5.0d };
        var ls = new[] { 2L, 3L, 4L };
        var ss = new[] { (short)33, (short)44, (short)55 };
        var fs = new[] { 6.0f, 7.0f, 8.0f };
        var bs = new[] { (byte)2, (byte)3, (byte)4 };
        var cs = new[] { 'a', 'b', 'c' };

        _expression = _parser.ParseExpression("[0]");
        Assert.Equal("a", _expression.GetValue(sss));
        AssertCanCompile(_expression);
        Assert.Equal("a", _expression.GetValue(sss));

        _expression = _parser.ParseExpression("[2]");
        Assert.Equal(10, _expression.GetValue(iss));
        AssertCanCompile(_expression);
        Assert.Equal(10, _expression.GetValue(iss));

        _expression = _parser.ParseExpression("[1]");
        Assert.Equal(4.0d, _expression.GetValue(ds));
        AssertCanCompile(_expression);
        Assert.Equal(4.0d, _expression.GetValue(ds));

        _expression = _parser.ParseExpression("[0]");
        Assert.Equal(2L, _expression.GetValue(ls));
        AssertCanCompile(_expression);
        Assert.Equal(2L, _expression.GetValue(ls));

        _expression = _parser.ParseExpression("[2]");
        Assert.Equal((short)55, _expression.GetValue(ss));
        AssertCanCompile(_expression);
        Assert.Equal((short)55, _expression.GetValue(ss));

        _expression = _parser.ParseExpression("[0]");
        Assert.Equal(6.0f, _expression.GetValue(fs));
        AssertCanCompile(_expression);
        Assert.Equal(6.0f, _expression.GetValue(fs));

        _expression = _parser.ParseExpression("[2]");
        Assert.Equal((byte)4, _expression.GetValue(bs));
        AssertCanCompile(_expression);
        Assert.Equal((byte)4, _expression.GetValue(bs));

        _expression = _parser.ParseExpression("[1]");
        Assert.Equal('b', _expression.GetValue(cs));
        AssertCanCompile(_expression);
        Assert.Equal('b', _expression.GetValue(cs));

        // Collections
        var strings = new List<string>
        {
            "aaa",
            "bbb",
            "ccc"
        };
        _expression = _parser.ParseExpression("[1]");
        Assert.Equal("bbb", _expression.GetValue(strings));
        AssertCanCompile(_expression);
        Assert.Equal("bbb", _expression.GetValue(strings));

        var ints = new List<int>
        {
            123,
            456,
            789
        };
        _expression = _parser.ParseExpression("[2]");
        Assert.Equal(789, _expression.GetValue(ints));
        AssertCanCompile(_expression);
        Assert.Equal(789, _expression.GetValue(ints));

        var map1 = new Dictionary<string, int>
        {
            { "aaa", 111 },
            { "bbb", 222 },
            { "ccc", 333 }
        };
        _expression = _parser.ParseExpression("['aaa']");
        Assert.Equal(111, _expression.GetValue(map1));
        AssertCanCompile(_expression);
        Assert.Equal(111, _expression.GetValue(map1));

        // Object TODO: Fix
        var tc = new TestClass6();
        _expression = _parser.ParseExpression("['Orange']");
        Assert.Equal("value1", _expression.GetValue(tc));
        AssertCanCompile(_expression);
        Assert.Equal("value1", _expression.GetValue(tc));

        _expression = _parser.ParseExpression("['Peach']");
        Assert.Equal(34L, _expression.GetValue(tc));
        AssertCanCompile(_expression);
        Assert.Equal(34L, _expression.GetValue(tc));

        _expression = _parser.ParseExpression("['Banana']");
        Assert.Equal("value3", _expression.GetValue(tc));
        AssertCanCompile(_expression);
        Assert.Equal("value3", _expression.GetValue(tc));

        // list of arrays
        var listOfStringArrays = new List<string[]>
        {
            new[] { "a", "b", "c" },
            new[] { "d", "e", "f" }
        };
        _expression = _parser.ParseExpression("[1]");
        Assert.Equal("d e f", Stringify(_expression.GetValue(listOfStringArrays)));
        AssertCanCompile(_expression);
        Assert.Equal("d e f", Stringify(_expression.GetValue(listOfStringArrays)));

        _expression = _parser.ParseExpression("[1][0]");
        Assert.Equal("d", Stringify(_expression.GetValue(listOfStringArrays)));
        AssertCanCompile(_expression);
        Assert.Equal("d", Stringify(_expression.GetValue(listOfStringArrays)));

        var listOfIntegerArrays = new List<int[]>
        {
            new[] { 1, 2, 3 },
            new[] { 4, 5, 6 }
        };
        _expression = _parser.ParseExpression("[0]");
        Assert.Equal("1 2 3", Stringify(_expression.GetValue(listOfIntegerArrays)));
        AssertCanCompile(_expression);
        Assert.Equal("1 2 3", Stringify(_expression.GetValue(listOfIntegerArrays)));

        _expression = _parser.ParseExpression("[0][1]");
        Assert.Equal(2, _expression.GetValue(listOfIntegerArrays));
        AssertCanCompile(_expression);
        Assert.Equal(2, _expression.GetValue(listOfIntegerArrays));

        // array of lists
        var stringArrayOfLists = new List<string>[2];
        stringArrayOfLists[0] = new List<string>
        {
            "a",
            "b",
            "c"
        };
        stringArrayOfLists[1] = new List<string>
        {
            "d",
            "e",
            "f"
        };

        _expression = _parser.ParseExpression("[1]");
        Assert.Equal("d e f", Stringify(_expression.GetValue(stringArrayOfLists)));
        AssertCanCompile(_expression);
        Assert.Equal("d e f", Stringify(_expression.GetValue(stringArrayOfLists)));

        _expression = _parser.ParseExpression("[1][2]");
        Assert.Equal("f", Stringify(_expression.GetValue(stringArrayOfLists)));
        AssertCanCompile(_expression);
        Assert.Equal("f", Stringify(_expression.GetValue(stringArrayOfLists)));

        // array of arrays
        var referenceTypeArrayOfArrays = new[] { new[] { "a", "b", "c" }, new[] { "d", "e", "f" } };
        _expression = _parser.ParseExpression("[1]");
        Assert.Equal("d e f", Stringify(_expression.GetValue(referenceTypeArrayOfArrays)));
        AssertCanCompile(_expression);
        Assert.Equal("d e f", Stringify(_expression.GetValue(referenceTypeArrayOfArrays)));

        _expression = _parser.ParseExpression("[1][2]");
        Assert.Equal("f", Stringify(_expression.GetValue(referenceTypeArrayOfArrays)));
        AssertCanCompile(_expression);
        Assert.Equal("f", Stringify(_expression.GetValue(referenceTypeArrayOfArrays)));

        var primitiveTypeArrayOfArrays = new[] { new[] { 1, 2, 3 }, new[] { 4, 5, 6 } };
        _expression = _parser.ParseExpression("[1]");
        Assert.Equal("4 5 6", Stringify(_expression.GetValue(primitiveTypeArrayOfArrays)));
        AssertCanCompile(_expression);
        Assert.Equal("4 5 6", Stringify(_expression.GetValue(primitiveTypeArrayOfArrays)));

        _expression = _parser.ParseExpression("[1][2]");
        Assert.Equal("6", Stringify(_expression.GetValue(primitiveTypeArrayOfArrays)));
        AssertCanCompile(_expression);
        Assert.Equal("6", Stringify(_expression.GetValue(primitiveTypeArrayOfArrays)));

        // list of lists of reference types
        var listOfListOfStrings = new List<List<string>>();
        var list = new List<string>
        {
            "a",
            "b",
            "c"
        };
        listOfListOfStrings.Add(list);
        list = new List<string>
        {
            "d",
            "e",
            "f"
        };
        listOfListOfStrings.Add(list);
        _expression = _parser.ParseExpression("[1]");
        Assert.Equal("d e f", Stringify(_expression.GetValue(listOfListOfStrings)));
        AssertCanCompile(_expression);
        Assert.Equal("d e f", Stringify(_expression.GetValue(listOfListOfStrings)));

        _expression = _parser.ParseExpression("[1][2]");
        Assert.Equal("f", Stringify(_expression.GetValue(listOfListOfStrings)));
        AssertCanCompile(_expression);
        Assert.Equal("f", Stringify(_expression.GetValue(listOfListOfStrings)));

        // Map of lists
        var mapToLists = new Dictionary<string, List<string>>();
        list = new List<string>
        {
            "a",
            "b",
            "c"
        };
        mapToLists.Add("foo", list);

        _expression = _parser.ParseExpression("['foo']");
        Assert.Equal("a b c", Stringify(_expression.GetValue(mapToLists)));
        AssertCanCompile(_expression);
        Assert.Equal("a b c", Stringify(_expression.GetValue(mapToLists)));

        _expression = _parser.ParseExpression("['foo'][2]");
        Assert.Equal("c", Stringify(_expression.GetValue(mapToLists)));
        AssertCanCompile(_expression);
        Assert.Equal("c", Stringify(_expression.GetValue(mapToLists)));

        // Map to array
        var mapToIntArray = new Dictionary<string, int[]>();
        var ctx = new StandardEvaluationContext();
        ctx.AddPropertyAccessor(new CompilableMapAccessor());
        mapToIntArray.Add("foo", new[] { 1, 2, 3 });

        _expression = _parser.ParseExpression("['foo']");
        Assert.Equal("1 2 3", Stringify(_expression.GetValue(mapToIntArray)));
        AssertCanCompile(_expression);
        Assert.Equal("1 2 3", Stringify(_expression.GetValue(mapToIntArray)));

        _expression = _parser.ParseExpression("['foo'][1]");
        Assert.Equal(2, _expression.GetValue(mapToIntArray));
        AssertCanCompile(_expression);
        Assert.Equal(2, _expression.GetValue(mapToIntArray));

        _expression = _parser.ParseExpression("foo");
        Assert.Equal("1 2 3", Stringify(_expression.GetValue(ctx, mapToIntArray)));
        AssertCanCompile(_expression);
        Assert.Equal("1 2 3", Stringify(_expression.GetValue(ctx, mapToIntArray)));

        _expression = _parser.ParseExpression("foo[1]");
        Assert.Equal(2, _expression.GetValue(ctx, mapToIntArray));
        AssertCanCompile(_expression);
        Assert.Equal(2, _expression.GetValue(ctx, mapToIntArray));

        _expression = _parser.ParseExpression("['foo'][2]");
        Assert.Equal("3", Stringify(_expression.GetValue(ctx, mapToIntArray)));
        AssertCanCompile(_expression);
        Assert.Equal("3", Stringify(_expression.GetValue(ctx, mapToIntArray)));

        // Map array
        var mapArray = new Dictionary<string, string>[1];
        mapArray[0] = new Dictionary<string, string>
        {
            { "key", "value1" }
        };

        _expression = _parser.ParseExpression("[0]");
        Assert.Equal("{key=value1}", Stringify(_expression.GetValue(mapArray)));
        AssertCanCompile(_expression);
        Assert.Equal("{key=value1}", Stringify(_expression.GetValue(mapArray)));

        _expression = _parser.ParseExpression("[0]['key']");
        Assert.Equal("value1", Stringify(_expression.GetValue(mapArray)));
        AssertCanCompile(_expression);
        Assert.Equal("value1", Stringify(_expression.GetValue(mapArray)));
    }

    [Fact]
    public void PlusNeedingCheckcast_SPR12426()
    {
        _expression = _parser.ParseExpression("Object + ' world'");
        var v = _expression.GetValue(new FooObject());
        Assert.Equal("hello world", v);
        AssertCanCompile(_expression);
        Assert.Equal("hello world", v);

        _expression = _parser.ParseExpression("Object + ' world'");
        v = _expression.GetValue(new FooString());
        Assert.Equal("hello world", v);
        AssertCanCompile(_expression);
        Assert.Equal("hello world", v);
    }

    [Fact]
    public void MixingItUp_PropertyAccessIndexerOpLtTernaryRootNull()
    {
        var payload = new Payload();
        _expression = _parser.ParseExpression("DR[0].Threeee");
        var v = _expression.GetValue(payload);

        var expression = _parser.ParseExpression("DR[0].Threeee.Four lt 0.1d?#root:null");
        v = expression.GetValue(payload);

        AssertCanCompile(expression);
        var vc = expression.GetValue(payload);
        Assert.Equal(payload, v);
        Assert.Equal(payload, vc);
        payload.DR[0].Threeee.four = 0.13d;
        vc = expression.GetValue(payload);
        Assert.Null(vc);
    }

    [Fact]
    public void VariantGetter()
    {
        var holder = new Payload2Holder();
        var ctx = new StandardEvaluationContext();
        ctx.AddPropertyAccessor(new MyAccessor());
        _expression = _parser.ParseExpression("Payload2.Var1");
        var v = _expression.GetValue(ctx, holder);
        Assert.Equal("abc", v);

        AssertCanCompile(_expression);
        v = _expression.GetValue(ctx, holder);
        Assert.Equal("abc", v);
    }

    [Fact]
    public void CompilerWithGenerics_12040()
    {
        _expression = _parser.ParseExpression("Payload!=2");
        Assert.True(_expression.GetValue<bool>(new GenericMessageTestHelper<int>(4)));
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>(new GenericMessageTestHelper<int>(2)));

        _expression = _parser.ParseExpression("2!=Payload");
        Assert.True(_expression.GetValue<bool>(new GenericMessageTestHelper<int>(4)));
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>(new GenericMessageTestHelper<int>(2)));

        _expression = _parser.ParseExpression("Payload!=6L");
        Assert.True(_expression.GetValue<bool>(new GenericMessageTestHelper<long>(4L)));
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>(new GenericMessageTestHelper<long>(6L)));

        _expression = _parser.ParseExpression("Payload==2");
        Assert.False(_expression.GetValue<bool>(new GenericMessageTestHelper<int>(4)));
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>(new GenericMessageTestHelper<int>(2)));

        _expression = _parser.ParseExpression("2==Payload");
        Assert.False(_expression.GetValue<bool>(new GenericMessageTestHelper<int>(4)));
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>(new GenericMessageTestHelper<int>(2)));

        _expression = _parser.ParseExpression("Payload==6L");
        Assert.False(_expression.GetValue<bool>(new GenericMessageTestHelper<long>(4L)));
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>(new GenericMessageTestHelper<long>(6L)));

        _expression = _parser.ParseExpression("2==Payload");
        Assert.False(_expression.GetValue<bool>(new GenericMessageTestHelper<int>(4)));
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>(new GenericMessageTestHelper<int>(2)));

        _expression = _parser.ParseExpression("Payload/2");
        Assert.Equal(2, _expression.GetValue<int>(new GenericMessageTestHelper<int>(4)));
        AssertCanCompile(_expression);
        Assert.Equal(3, _expression.GetValue<int>(new GenericMessageTestHelper<int>(6)));

        _expression = _parser.ParseExpression("100/Payload");
        Assert.Equal(25, _expression.GetValue<int>(new GenericMessageTestHelper<int>(4)));
        AssertCanCompile(_expression);
        Assert.Equal(10, _expression.GetValue<int>(new GenericMessageTestHelper<int>(10)));

        _expression = _parser.ParseExpression("Payload+2");
        Assert.Equal(6, _expression.GetValue<int>(new GenericMessageTestHelper<int>(4)));
        AssertCanCompile(_expression);
        Assert.Equal(8, _expression.GetValue<int>(new GenericMessageTestHelper<int>(6)));

        _expression = _parser.ParseExpression("100+Payload");
        Assert.Equal(104, _expression.GetValue<int>(new GenericMessageTestHelper<int>(4)));
        AssertCanCompile(_expression);
        Assert.Equal(110, _expression.GetValue<int>(new GenericMessageTestHelper<int>(10)));

        _expression = _parser.ParseExpression("Payload-2");
        Assert.Equal(2, _expression.GetValue<int>(new GenericMessageTestHelper<int>(4)));
        AssertCanCompile(_expression);
        Assert.Equal(4, _expression.GetValue<int>(new GenericMessageTestHelper<int>(6)));

        _expression = _parser.ParseExpression("100-Payload");
        Assert.Equal(96, _expression.GetValue<int>(new GenericMessageTestHelper<int>(4)));
        AssertCanCompile(_expression);
        Assert.Equal(90, _expression.GetValue<int>(new GenericMessageTestHelper<int>(10)));

        _expression = _parser.ParseExpression("Payload*2");
        Assert.Equal(8, _expression.GetValue<int>(new GenericMessageTestHelper<int>(4)));
        AssertCanCompile(_expression);
        Assert.Equal(12, _expression.GetValue<int>(new GenericMessageTestHelper<int>(6)));

        _expression = _parser.ParseExpression("100*Payload");
        Assert.Equal(400, _expression.GetValue<int>(new GenericMessageTestHelper<int>(4)));
        AssertCanCompile(_expression);
        Assert.Equal(1000, _expression.GetValue<int>(new GenericMessageTestHelper<int>(10)));

        _expression = _parser.ParseExpression("Payload/2L");
        Assert.Equal(2L, _expression.GetValue<long>(new GenericMessageTestHelper<long>(4L)));
        AssertCanCompile(_expression);
        Assert.Equal(3L, _expression.GetValue<long>(new GenericMessageTestHelper<long>(6L)));

        _expression = _parser.ParseExpression("100L/Payload");
        Assert.Equal(25L, _expression.GetValue<long>(new GenericMessageTestHelper<long>(4L)));
        AssertCanCompile(_expression);
        Assert.Equal(10L, _expression.GetValue<long>(new GenericMessageTestHelper<long>(10L)));

        _expression = _parser.ParseExpression("Payload/2f");
        Assert.Equal(2f, _expression.GetValue<float>(new GenericMessageTestHelper<float>(4f)));
        AssertCanCompile(_expression);
        Assert.Equal(3f, _expression.GetValue<float>(new GenericMessageTestHelper<float>(6f)));

        _expression = _parser.ParseExpression("100f/Payload");
        Assert.Equal(25f, _expression.GetValue<float>(new GenericMessageTestHelper<float>(4f)));
        AssertCanCompile(_expression);
        Assert.Equal(10f, _expression.GetValue<float>(new GenericMessageTestHelper<float>(10f)));

        _expression = _parser.ParseExpression("Payload/2d");
        Assert.Equal(2d, _expression.GetValue<double>(new GenericMessageTestHelper<double>(4d)));
        AssertCanCompile(_expression);
        Assert.Equal(3d, _expression.GetValue<double>(new GenericMessageTestHelper<double>(6d)));

        _expression = _parser.ParseExpression("100d/Payload");
        Assert.Equal(25d, _expression.GetValue<double>(new GenericMessageTestHelper<double>(4d)));
        AssertCanCompile(_expression);
        Assert.Equal(10d, _expression.GetValue<double>(new GenericMessageTestHelper<double>(10d)));
    }

    [Fact]
    public void CompilerWithGenerics_12040_2()
    {
        _expression = _parser.ParseExpression("Payload/2");
        Assert.Equal(2, _expression.GetValue<int>(new GenericMessageTestHelper2<int>(4)));
        AssertCanCompile(_expression);
        Assert.Equal(3, _expression.GetValue<int>(new GenericMessageTestHelper2<int>(6)));

        _expression = _parser.ParseExpression("9/Payload");
        Assert.Equal(1, _expression.GetValue<int>(new GenericMessageTestHelper2<int>(9)));
        AssertCanCompile(_expression);
        Assert.Equal(3, _expression.GetValue<int>(new GenericMessageTestHelper2<int>(3)));

        _expression = _parser.ParseExpression("Payload+2");
        Assert.Equal(6, _expression.GetValue<int>(new GenericMessageTestHelper2<int>(4)));
        AssertCanCompile(_expression);
        Assert.Equal(8, _expression.GetValue<int>(new GenericMessageTestHelper2<int>(6)));

        _expression = _parser.ParseExpression("100+Payload");
        Assert.Equal(104, _expression.GetValue<int>(new GenericMessageTestHelper2<int>(4)));
        AssertCanCompile(_expression);
        Assert.Equal(110, _expression.GetValue<int>(new GenericMessageTestHelper2<int>(10)));

        _expression = _parser.ParseExpression("Payload-2");
        Assert.Equal(2, _expression.GetValue<int>(new GenericMessageTestHelper2<int>(4)));
        AssertCanCompile(_expression);
        Assert.Equal(4, _expression.GetValue<int>(new GenericMessageTestHelper2<int>(6)));

        _expression = _parser.ParseExpression("100-Payload");
        Assert.Equal(96, _expression.GetValue<int>(new GenericMessageTestHelper2<int>(4)));
        AssertCanCompile(_expression);
        Assert.Equal(90, _expression.GetValue<int>(new GenericMessageTestHelper2<int>(10)));

        _expression = _parser.ParseExpression("Payload*2");
        Assert.Equal(8, _expression.GetValue<int>(new GenericMessageTestHelper2<int>(4)));
        AssertCanCompile(_expression);
        Assert.Equal(12, _expression.GetValue<int>(new GenericMessageTestHelper2<int>(6)));

        _expression = _parser.ParseExpression("100*Payload");
        Assert.Equal(400, _expression.GetValue<int>(new GenericMessageTestHelper2<int>(4)));
        AssertCanCompile(_expression);
        Assert.Equal(1000, _expression.GetValue<int>(new GenericMessageTestHelper2<int>(10)));
    }

    [Fact]
    public void CompilerWithGenerics_12040_3()
    {
        _expression = _parser.ParseExpression("Payload >= 2");
        Assert.True(_expression.GetValue<bool>(new GenericMessageTestHelper<int>(4)));
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>(new GenericMessageTestHelper<int>(1)));

        _expression = _parser.ParseExpression("2 >= Payload");
        Assert.False(_expression.GetValue<bool>(new GenericMessageTestHelper<int>(5)));
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>(new GenericMessageTestHelper<int>(1)));

        _expression = _parser.ParseExpression("Payload > 2");
        Assert.True(_expression.GetValue<bool>(new GenericMessageTestHelper<int>(4)));
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>(new GenericMessageTestHelper<int>(1)));

        _expression = _parser.ParseExpression("2 > Payload");
        Assert.False(_expression.GetValue<bool>(new GenericMessageTestHelper<int>(5)));
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>(new GenericMessageTestHelper<int>(1)));

        _expression = _parser.ParseExpression("Payload <=2");
        Assert.True(_expression.GetValue<bool>(new GenericMessageTestHelper<int>(1)));
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>(new GenericMessageTestHelper<int>(6)));

        _expression = _parser.ParseExpression("2 <= Payload");
        Assert.False(_expression.GetValue<bool>(new GenericMessageTestHelper<int>(1)));
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>(new GenericMessageTestHelper<int>(6)));

        _expression = _parser.ParseExpression("Payload < 2");
        Assert.True(_expression.GetValue<bool>(new GenericMessageTestHelper<int>(1)));
        AssertCanCompile(_expression);
        Assert.False(_expression.GetValue<bool>(new GenericMessageTestHelper<int>(6)));

        _expression = _parser.ParseExpression("2 < Payload");
        Assert.False(_expression.GetValue<bool>(new GenericMessageTestHelper<int>(1)));
        AssertCanCompile(_expression);
        Assert.True(_expression.GetValue<bool>(new GenericMessageTestHelper<int>(6)));
    }

    [Fact]
    public void IndexerMapAccessor_12045()
    {
        var spc = new SpelParserOptions(SpelCompilerMode.IMMEDIATE);
        var sep = new SpelExpressionParser(spc);
        _expression = sep.ParseExpression("Headers[command]");
        var root = new MyMessage();
        Assert.Equal("wibble", _expression.GetValue(root));

        // This next call was failing because the isCompilable check in Indexer
        // did not check on the key being compilable (and also generateCode in the
        // Indexer was missing the optimization that it didn't need necessarily
        // need to call generateCode for that accessor)
        Assert.Equal("wibble", _expression.GetValue(root));
        AssertCanCompile(_expression);
        _expression = sep.ParseExpression("Headers[GetKey()]");
        Assert.Equal("wobble", _expression.GetValue(root));
        Assert.Equal("wobble", _expression.GetValue(root));

        _expression = sep.ParseExpression("List[GetKey2()]");
        Assert.Equal("wobble", _expression.GetValue(root));
        Assert.Equal("wobble", _expression.GetValue(root));

        _expression = sep.ParseExpression("IArray[GetKey2()]");
        Assert.Equal(3, _expression.GetValue(root));
        Assert.Equal(3, _expression.GetValue(root));
    }

    [Fact]
    public void ElvisOperator_SPR15192()
    {
        var configuration = new SpelParserOptions(SpelCompilerMode.IMMEDIATE);
        var exp = new SpelExpressionParser(configuration).ParseExpression("Bar()") as SpelExpression;
        Assert.Equal("BAR", exp.GetValue<string>(new Foo()));
        AssertCanCompile(exp);
        Assert.Equal("BAR", exp.GetValue<string>(new Foo()));
        AssertIsCompiled(exp);

        exp = new SpelExpressionParser(configuration).ParseExpression("Bar('baz')") as SpelExpression;
        Assert.Equal("BAZ", exp.GetValue<string>(new Foo()));
        AssertCanCompile(exp);
        Assert.Equal("BAZ", exp.GetValue<string>(new Foo()));
        AssertIsCompiled(exp);

        var context = new StandardEvaluationContext();
        context.SetVariable("map", new Dictionary<string, string> { { "foo", "qux" } });

        exp = new SpelExpressionParser(configuration).ParseExpression("Bar(#map['foo'])") as SpelExpression;
        Assert.Equal("QUX", exp.GetValue<string>(context, new Foo()));
        AssertCanCompile(exp);
        Assert.Equal("QUX", exp.GetValue<string>(context, new Foo()));
        AssertIsCompiled(exp);

        exp = new SpelExpressionParser(configuration).ParseExpression("Bar(#map['foo'] ?: 'qux')") as SpelExpression;
        Assert.Equal("QUX", exp.GetValue<string>(context, new Foo()));
        AssertCanCompile(exp);
        Assert.Equal("QUX", exp.GetValue<string>(context, new Foo()));
        AssertIsCompiled(exp);

        // When the condition is a primitive
        exp = new SpelExpressionParser(configuration).ParseExpression("3?:'foo'") as SpelExpression;
        Assert.Equal("3", exp.GetValue<string>(context, new Foo()));
        AssertCanCompile(exp);
        Assert.Equal("3", exp.GetValue<string>(context, new Foo()));
        AssertIsCompiled(exp);

        // When the condition is a long primitive
        exp = new SpelExpressionParser(configuration).ParseExpression("3L?:'foo'") as SpelExpression;
        Assert.Equal("3", exp.GetValue<string>(context, new Foo()));
        AssertCanCompile(exp);
        Assert.Equal("3", exp.GetValue<string>(context, new Foo()));
        AssertIsCompiled(exp);

        // When the condition is an empty string
        exp = new SpelExpressionParser(configuration).ParseExpression("''?:4L") as SpelExpression;
        Assert.Equal("4", exp.GetValue<string>(context, new Foo()));
        AssertCanCompile(exp);
        Assert.Equal("4", exp.GetValue<string>(context, new Foo()));
        AssertIsCompiled(exp);

        // null condition
        exp = new SpelExpressionParser(configuration).ParseExpression("null?:4L") as SpelExpression;
        Assert.Equal("4", exp.GetValue<string>(context, new Foo()));
        AssertCanCompile(exp);
        Assert.Equal("4", exp.GetValue<string>(context, new Foo()));
        AssertIsCompiled(exp);

        // variable access returning primitive
        exp = new SpelExpressionParser(configuration).ParseExpression("#x?:'foo'") as SpelExpression;
        context.SetVariable("x", 50);
        Assert.Equal("50", exp.GetValue<string>(context, new Foo()));
        AssertCanCompile(exp);
        Assert.Equal("50", exp.GetValue<string>(context, new Foo()));
        AssertIsCompiled(exp);

        exp = new SpelExpressionParser(configuration).ParseExpression("#x?:'foo'") as SpelExpression;
        context.SetVariable("x", null);
        Assert.Equal("foo", exp.GetValue<string>(context, new Foo()));
        AssertCanCompile(exp);
        Assert.Equal("foo", exp.GetValue<string>(context, new Foo()));
        AssertIsCompiled(exp);

        // variable access returning array
        exp = new SpelExpressionParser(configuration).ParseExpression("#x?:'foo'") as SpelExpression;
        context.SetVariable("x", new[] { 1, 2, 3 });
        Assert.Equal("1,2,3", exp.GetValue<string>(context, new Foo()));
        AssertCanCompile(exp);
        Assert.Equal("1,2,3", exp.GetValue<string>(context, new Foo()));
        AssertIsCompiled(exp);
    }

    [Fact]
    public void ElvisOperator_SPR17214()
    {
        var configuration = new SpelParserOptions(SpelCompilerMode.IMMEDIATE);
        var sep = new SpelExpressionParser(configuration);

        _expression = sep.ParseExpression("Record['abc']?:Record.Add('abc',Expression.SomeLong)");
        var rh = new RecordHolder();
        Assert.Null(_expression.GetValue(rh));
        Assert.Equal(3L, _expression.GetValue(rh));
        AssertCanCompile(_expression);
        rh = new RecordHolder();
        Assert.Null(_expression.GetValue(rh));
        Assert.Equal(3L, _expression.GetValue(rh));

        _expression = sep.ParseExpression("Record['abc']?:Record.Add('abc',3L)");
        rh = new RecordHolder();
        Assert.Null(_expression.GetValue(rh));
        Assert.Equal(3L, _expression.GetValue(rh));
        AssertCanCompile(_expression);
        rh = new RecordHolder();
        Assert.Null(_expression.GetValue(rh));
        Assert.Equal(3L, _expression.GetValue(rh));

        _expression = sep.ParseExpression("Record['abc']?:Record.Add('abc',Expression.SomeLong)");
        rh = new RecordHolder { Expression = { SomeLong = 6L } };
        Assert.Null(_expression.GetValue(rh));
        Assert.Equal(6L, rh.Get("abc"));
        AssertCanCompile(_expression);
        rh = new RecordHolder { Expression = { SomeLong = 10L } };
        Assert.Null(_expression.GetValue(rh));
        Assert.Equal(10L, rh.Get("abc"));
    }

    [Fact]
    public void TestNullComparison_SPR22358()
    {
        var configuration = new SpelParserOptions(SpelCompilerMode.OFF);
        var parser = new SpelExpressionParser(configuration);
        var ctx = new StandardEvaluationContext();
        ctx.SetRootObject(new Reg(1));
        VerifyCompilationAndBehaviourWithNull("Value>1", parser, ctx);
        VerifyCompilationAndBehaviourWithNull("Value<1", parser, ctx);
        VerifyCompilationAndBehaviourWithNull("Value>=1", parser, ctx);
        VerifyCompilationAndBehaviourWithNull("Value<=1", parser, ctx);

        VerifyCompilationAndBehaviourWithNull2("Value>Value2", parser, ctx);
        VerifyCompilationAndBehaviourWithNull2("Value<Value2", parser, ctx);
        VerifyCompilationAndBehaviourWithNull2("Value>=Value2", parser, ctx);
        VerifyCompilationAndBehaviourWithNull2("Value<=Value2", parser, ctx);

        VerifyCompilationAndBehaviourWithNull("ValueD>1.0d", parser, ctx);
        VerifyCompilationAndBehaviourWithNull("ValueD<1.0d", parser, ctx);
        VerifyCompilationAndBehaviourWithNull("ValueD>=1.0d", parser, ctx);
        VerifyCompilationAndBehaviourWithNull("ValueD<=1.0d", parser, ctx);

        VerifyCompilationAndBehaviourWithNull2("ValueD>ValueD2", parser, ctx);
        VerifyCompilationAndBehaviourWithNull2("ValueD<ValueD2", parser, ctx);
        VerifyCompilationAndBehaviourWithNull2("ValueD>=ValueD2", parser, ctx);
        VerifyCompilationAndBehaviourWithNull2("ValueD<=ValueD2", parser, ctx);

        VerifyCompilationAndBehaviourWithNull("ValueL>1L", parser, ctx);
        VerifyCompilationAndBehaviourWithNull("ValueL<1L", parser, ctx);
        VerifyCompilationAndBehaviourWithNull("ValueL>=1L", parser, ctx);
        VerifyCompilationAndBehaviourWithNull("ValueL<=1L", parser, ctx);

        VerifyCompilationAndBehaviourWithNull2("ValueL>ValueL2", parser, ctx);
        VerifyCompilationAndBehaviourWithNull2("ValueL<ValueL2", parser, ctx);
        VerifyCompilationAndBehaviourWithNull2("ValueL>=ValueL2", parser, ctx);
        VerifyCompilationAndBehaviourWithNull2("ValueL<=ValueL2", parser, ctx);

        VerifyCompilationAndBehaviourWithNull("ValueF>1.0f", parser, ctx);
        VerifyCompilationAndBehaviourWithNull("ValueF<1.0f", parser, ctx);
        VerifyCompilationAndBehaviourWithNull("ValueF>=1.0f", parser, ctx);
        VerifyCompilationAndBehaviourWithNull("ValueF<=1.0f", parser, ctx);

        VerifyCompilationAndBehaviourWithNull("ValueF>ValueF2", parser, ctx);
        VerifyCompilationAndBehaviourWithNull("ValueF<ValueF2", parser, ctx);
        VerifyCompilationAndBehaviourWithNull("ValueF>=ValueF2", parser, ctx);
        VerifyCompilationAndBehaviourWithNull("ValueF<=ValueF2", parser, ctx);
    }

    [Fact]
    public void TernaryOperator_SPR15192()
    {
        var configuration = new SpelParserOptions(SpelCompilerMode.IMMEDIATE);
        var context = new StandardEvaluationContext();
        context.SetVariable("map", new Dictionary<string, string> { { "foo", "qux" } });

        var exp = new SpelExpressionParser(configuration).ParseExpression("Bar(#map['foo'] != null ? #map['foo'] : 'qux')") as SpelExpression;
        Assert.Equal("QUX", exp.GetValue<string>(context, new Foo()));
        AssertCanCompile(exp);
        Assert.Equal("QUX", exp.GetValue<string>(context, new Foo()));
        AssertIsCompiled(exp);

        exp = new SpelExpressionParser(configuration).ParseExpression("3==3?3:'foo'") as SpelExpression;
        Assert.Equal("3", exp.GetValue<string>(context, new Foo()));
        AssertCanCompile(exp);
        Assert.Equal("3", exp.GetValue<string>(context, new Foo()));
        AssertIsCompiled(exp);
        exp = new SpelExpressionParser(configuration).ParseExpression("3!=3?3:'foo'") as SpelExpression;
        Assert.Equal("foo", exp.GetValue<string>(context, new Foo()));
        AssertCanCompile(exp);
        Assert.Equal("foo", exp.GetValue<string>(context, new Foo()));
        AssertIsCompiled(exp);

        // When the condition is a long
        exp = new SpelExpressionParser(configuration).ParseExpression("3==3?3L:'foo'") as SpelExpression;
        Assert.Equal("3", exp.GetValue<string>(context, new Foo()));
        AssertCanCompile(exp);
        Assert.Equal("3", exp.GetValue<string>(context, new Foo()));
        AssertIsCompiled(exp);
        exp = new SpelExpressionParser(configuration).ParseExpression("3!=3?3L:'foo'") as SpelExpression;
        Assert.Equal("foo", exp.GetValue<string>(context, new Foo()));
        AssertCanCompile(exp);
        Assert.Equal("foo", exp.GetValue<string>(context, new Foo()));
        AssertIsCompiled(exp);

        // When the condition is an empty string
        exp = new SpelExpressionParser(configuration).ParseExpression("''==''?'abc':4L") as SpelExpression;
        Assert.Equal("abc", exp.GetValue<string>(context, new Foo()));
        AssertCanCompile(exp);
        Assert.Equal("abc", exp.GetValue<string>(context, new Foo()));
        AssertIsCompiled(exp);

        // null condition
        exp = new SpelExpressionParser(configuration).ParseExpression("3==3?null:4L") as SpelExpression;
        Assert.Null(exp.GetValue<string>(context, new Foo()));
        AssertCanCompile(exp);
        Assert.Null(exp.GetValue<string>(context, new Foo()));
        AssertIsCompiled(exp);

        // variable access returning primitive
        exp = new SpelExpressionParser(configuration).ParseExpression("#x==#x?50:'foo'") as SpelExpression;
        context.SetVariable("x", 50);
        Assert.Equal("50", exp.GetValue<string>(context, new Foo()));
        AssertCanCompile(exp);
        Assert.Equal("50", exp.GetValue<string>(context, new Foo()));
        AssertIsCompiled(exp);

        exp = new SpelExpressionParser(configuration).ParseExpression("#x!=#x?50:'foo'") as SpelExpression;
        context.SetVariable("x", null);
        Assert.Equal("foo", exp.GetValue<string>(context, new Foo()));
        AssertCanCompile(exp);
        Assert.Equal("foo", exp.GetValue<string>(context, new Foo()));
        AssertIsCompiled(exp);

        // variable access returning array
        exp = new SpelExpressionParser(configuration).ParseExpression("#x==#x?'1,2,3':'foo'") as SpelExpression;
        context.SetVariable("x", new[] { 1, 2, 3 });
        Assert.Equal("1,2,3", exp.GetValue<string>(context, new Foo()));
        AssertCanCompile(exp);
        Assert.Equal("1,2,3", exp.GetValue<string>(context, new Foo()));
        AssertIsCompiled(exp);
    }

    [Fact]
    public void RepeatedCompilations()
    {
        for (var i = 0; i < 1500; i++)
        {
            var expression = _parser.ParseExpression("4 + 5") as SpelExpression;
            Assert.Equal(9, expression.GetValue<int>());
            AssertCanCompile(expression);
            Assert.Equal(9, expression.GetValue<int>());
            AssertIsCompiled(expression);
        }
    }

    [Fact]
    public void CompilationKicksInAfterThreshold()
    {
        var parser = new SpelExpressionParser(new SpelParserOptions(SpelCompilerMode.MIXED));
        var expression = parser.ParseExpression("4 + 5") as SpelExpression;
        for (var i = 0; i < 200; i++)
        {
            Assert.Equal(9, expression.GetValue<int>());
            if (i < SpelExpression.INTERPRETED_COUNT_THRESHOLD)
            {
                AssertNotCompiled(expression);
            }
            else
            {
                AssertIsCompiled(expression);
            }
        }
    }

    [Fact]
    public void PropertyReferenceValueType()
    {
        // static property on valuetype
        _expression = _parser.ParseExpression("T(DateTime).Now");
        var start = DateTime.Now.Ticks;
        Assert.True(start < _expression.GetValue<DateTime>().Ticks);
        AssertCanCompile(_expression);
        Assert.True(start < _expression.GetValue<DateTime>().Ticks);

        // instance property on valuetype
        _expression = _parser.ParseExpression("T(DateTime).Now.Second");
        Assert.InRange(_expression.GetValue<int>(), 0, 60);
        AssertCanCompile(_expression);
        Assert.InRange(_expression.GetValue<int>(), 0, 60);

        // instance property on boxed valuetype
        _expression = _parser.ParseExpression("#a.ValueProperty");
        _context.SetVariable("a", new A(10));
        Assert.Equal(10, _expression.GetValue(_context));
        AssertCanCompile(_expression);
        Assert.Equal(10, _expression.GetValue(_context));

        // static property on boxed valuetype
        _expression = _parser.ParseExpression("#a.ValuePropertyStatic");
        _context.SetVariable("a", new A(10));
        Assert.Equal(30, _expression.GetValue(_context));
        AssertCanCompile(_expression);
        Assert.Equal(30, _expression.GetValue(_context));
    }

    [Fact]
    public void FieldReferenceValueType()
    {
        // static field on unboxed valuetype
        _expression = _parser.ParseExpression("T(DateTime).MaxValue");
        var resultI = _expression.GetValue<DateTime>();
        AssertCanCompile(_expression);
        var resultC = _expression.GetValue<DateTime>();
        Assert.Equal(resultI, resultC);

        // instance field on unboxed valuetype
        _expression = _parser.ParseExpression("T(Steeltoe.Common.Expression.Internal.Spring.SpelCompilationCoverageTests$AHolder).GetA().Value");
        Assert.Equal(20, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(20, _expression.GetValue());

        // instance field on boxed valuetype
        _expression = _parser.ParseExpression("#a.Value");
        _context.SetVariable("a", new A(10));
        Assert.Equal(10, _expression.GetValue(_context));
        AssertCanCompile(_expression);
        Assert.Equal(10, _expression.GetValue(_context));

        // static field on boxed valuetype
        _expression = _parser.ParseExpression("#a.ValueFieldStatic");
        _context.SetVariable("a", new A(10));
        Assert.Equal(40, _expression.GetValue(_context));
        AssertCanCompile(_expression);
        Assert.Equal(40, _expression.GetValue(_context));
    }

    [Fact]
    public void MethodReferenceValueType()
    {
        // static method on unboxed valuetype
        _expression = _parser.ParseExpression("T(DateTime).Parse('2/16/2008 12:15:12 PM')");
        var resultI = _expression.GetValue<DateTime>();
        AssertCanCompile(_expression);
        var resultC = _expression.GetValue<DateTime>();
        Assert.Equal(resultI, resultC);

        // instance method on unboxed valuetype
        _expression = _parser.ParseExpression("T(Steeltoe.Common.Expression.Internal.Spring.SpelCompilationCoverageTests$AHolder).GetA().Method()");
        Assert.Equal(20, _expression.GetValue());
        AssertCanCompile(_expression);
        Assert.Equal(20, _expression.GetValue());

        // instance method on boxed valuetype
        _expression = _parser.ParseExpression("#a.Method()");
        _context.SetVariable("a", new A(20));
        Assert.Equal(20, _expression.GetValue(_context));
        AssertCanCompile(_expression);
        Assert.Equal(20, _expression.GetValue(_context));

        // static method on boxed valuetype
        _expression = _parser.ParseExpression("#a.StaticMethod()");
        _context.SetVariable("a", new A(10));
        Assert.Equal(40, _expression.GetValue(_context));
        AssertCanCompile(_expression);
        Assert.Equal(40, _expression.GetValue(_context));
    }

    [Fact]
    public void MethodArgumentsValueTypes()
    {
        var testArgs = new TestAArguments();
        _expression = _parser.ParseExpression("TestUnboxed(#a)");
        _context.SetVariable("a", new A(10));
        Assert.Null(_expression.GetValue(_context, testArgs));
        Assert.Equal(10, testArgs.Value);
        AssertCanCompile(_expression);
        testArgs.Value = 0;
        Assert.Null(_expression.GetValue(_context, testArgs));
        Assert.Equal(10, testArgs.Value);

        testArgs = new TestAArguments();
        _expression = _parser.ParseExpression("TestBoxed(#a)");
        _context.SetVariable("a", new A(20));
        Assert.Null(_expression.GetValue(_context, testArgs));
        Assert.Equal(20, testArgs.Value);
        AssertCanCompile(_expression);
        testArgs.Value = 0;
        Assert.Null(_expression.GetValue(_context, testArgs));
        Assert.Equal(20, testArgs.Value);
    }

    private void VerifyCompilationAndBehaviourWithNull(string expressionText, SpelExpressionParser parser, StandardEvaluationContext ctx)
    {
        var r = (Reg)ctx.RootObject.Value;
        r.SetValue2(1);  // having a value in value2 fields will enable compilation to succeed, then can switch it to null
        var fast = (SpelExpression)parser.ParseExpression(expressionText);
        var slow = (SpelExpression)parser.ParseExpression(expressionText);
        fast.GetValue(ctx);
        Assert.True(fast.CompileExpression());
        r.SetValue2(null);

        // try the numbers 0,1,2,null
        for (var i = 0; i < 4; i++)
        {
            r.SetValue(i < 3 ? i : null);
            var slowResult = slow.GetValue<bool>(ctx);
            var fastResult = fast.GetValue<bool>(ctx);

            Assert.Equal(slowResult, fastResult);
        }
    }

    private void VerifyCompilationAndBehaviourWithNull2(string expressionText, SpelExpressionParser parser, StandardEvaluationContext ctx)
    {
        var fast = (SpelExpression)parser.ParseExpression(expressionText);
        var slow = (SpelExpression)parser.ParseExpression(expressionText);
        fast.GetValue(ctx);
        Assert.True(fast.CompileExpression());
        var r = (Reg)ctx.RootObject.Value;

        // try the numbers 0,1,2,null
        for (var i = 0; i < 4; i++)
        {
            r.SetValue(i < 3 ? i : null);
            var slowResult = slow.GetValue<bool>(ctx);
            var fastResult = fast.GetValue<bool>(ctx);

            Assert.Equal(slowResult, fastResult);
        }
    }

    private void AssertNotCompiled(SpelExpression expression)
    {
        Assert.Null(expression._compiledAst);
    }

    private void AssertIsCompiled(SpelExpression expression)
    {
        Assert.NotNull(expression._compiledAst);
    }

    private void AssertCanCompile(IExpression expression)
    {
        Assert.True(SpelCompiler.Compile(expression));
    }

    private void AssertCantCompile(IExpression expression)
    {
        Assert.False(SpelCompiler.Compile(expression));
    }

    private void AssertGetValueFail(IExpression expression)
    {
        Assert.Throws<SpelEvaluationException>(() => _expression.GetValue());
    }

    private IExpression Parse(string expression)
    {
        return _parser.ParseExpression(expression);
    }

    private string Stringify(object obj)
    {
        var s = new StringBuilder();
        if (obj is IList ls)
        {
            foreach (var l in ls)
            {
                s.Append(l);
                s.Append(' ');
            }
        }
        else if (obj is object[] os)
        {
            foreach (var o in os)
            {
                s.Append(o);
                s.Append(' ');
            }
        }
        else if (obj is int[] iss)
        {
            foreach (var i in iss)
            {
                s.Append(i);
                s.Append(' ');
            }
        }
        else if (obj is IDictionary dict)
        {
            foreach (DictionaryEntry kvp in dict)
            {
                s.Append('{');
                s.Append(kvp.Key);
                s.Append('=');
                s.Append(kvp.Value);
                s.Append('}');
                s.Append(' ');
            }
        }
        else
        {
            s.Append(obj);
        }

        return s.ToString().Trim();
    }

    private void CheckCalc(PayloadX p, string expression, int expectedResult)
    {
        var expr = Parse(expression);
        Assert.Equal(expectedResult, expr.GetValue(p));
        AssertCanCompile(expr);
        Assert.Equal(expectedResult, expr.GetValue(p));
    }

    private void CheckCalc(PayloadX p, string expression, float expectedResult)
    {
        var expr = Parse(expression);
        Assert.Equal(expectedResult, expr.GetValue(p));
        AssertCanCompile(expr);
        Assert.Equal(expectedResult, expr.GetValue(p));
    }

    private void CheckCalc(PayloadX p, string expression, long expectedResult)
    {
        var expr = Parse(expression);
        Assert.Equal(expectedResult, expr.GetValue(p));
        AssertCanCompile(expr);
        Assert.Equal(expectedResult, expr.GetValue(p));
    }

    private void CheckCalc(PayloadX p, string expression, double expectedResult)
    {
        var expr = Parse(expression);
        Assert.Equal(expectedResult, expr.GetValue(p));
        AssertCanCompile(expr);
        Assert.Equal(expectedResult, expr.GetValue(p));
    }

    #region Test Classes
#pragma warning disable IDE1006
#pragma warning disable IDE0044
#pragma warning disable IDE0051
#pragma warning disable SA1307

    public class AHolder
    {
        public static A GetA()
        {
            return new A(20);
        }
    }

    public struct A
    {
        public int Value;

        public static int ValueFieldStatic => 40;

        public static int ValuePropertyStatic => 30;

        public int ValueProperty => Value;

        public A(int value)
        {
            Value = value;
        }

        public int Method()
        {
            return Value;
        }

        public int StaticMethod()
        {
            return ValueFieldStatic;
        }
    }

    public class TestAArguments
    {
        public int Value;

        public void TestUnboxed(A a)
        {
            Value = a.Value;
        }

        public void TestBoxed(object a)
        {
            Value = ((A)a).Value;
        }
    }

    public class MyAccessor : ICompilablePropertyAccessor
    {
        private static readonly MethodInfo _method = typeof(Payload2).GetMethod("GetField", new[] { typeof(string) });

        public bool CanRead(IEvaluationContext context, object target, string name)
        {
            return true;
        }

        public ITypedValue Read(IEvaluationContext context, object target, string name)
        {
            var payload2 = (Payload2)target;
            return new TypedValue(payload2.GetField(name));
        }

        public bool CanWrite(IEvaluationContext context, object target, string name)
        {
            return false;
        }

        public void Write(IEvaluationContext context, object target, string name, object newValue)
        {
            // Ignore
        }

        public IList<Type> GetSpecificTargetClasses()
        {
            return new List<Type> { typeof(Payload2) };
        }

        public bool IsCompilable()
        {
            return true;
        }

        public Type GetPropertyType()
        {
            return typeof(object);
        }

        public void GenerateCode(string propertyName, ILGenerator gen, CodeFlow cf)
        {
            var descriptor = cf.LastDescriptor();
            if (descriptor == null)
            {
                CodeFlow.LoadTarget(gen);
            }

            if (descriptor == null || descriptor.Value != _method.DeclaringType)
            {
                gen.Emit(OpCodes.Castclass, _method.DeclaringType);
            }

            gen.Emit(OpCodes.Ldstr, propertyName);
            gen.Emit(OpCodes.Callvirt, _method);
        }
    }

    public class CompilableMapAccessor : ICompilablePropertyAccessor
    {
        private static readonly MethodInfo _getItem = typeof(IDictionary).GetMethod("get_Item", new[] { typeof(object) });

        public bool CanRead(IEvaluationContext context, object target, string name)
        {
            var map = (IDictionary)target;
            return map.Contains(name);
        }

        public ITypedValue Read(IEvaluationContext context, object target, string name)
        {
            var map = (IDictionary)target;
            var value = map[name];
            if (value == null && !map.Contains(name))
            {
                throw new AccessException(name);
            }

            return new TypedValue(value);
        }

        public bool CanWrite(IEvaluationContext context, object target, string name)
        {
            return true;
        }

        public void Write(IEvaluationContext context, object target, string name, object newValue)
        {
            var map = (IDictionary)target;
            map.Add(name, newValue);
        }

        public IList<Type> GetSpecificTargetClasses()
        {
            return new List<Type> { typeof(IDictionary) };
        }

        public bool IsCompilable()
        {
            return true;
        }

        public Type GetPropertyType()
        {
            return typeof(object);
        }

        public void GenerateCode(string propertyName, ILGenerator gen, CodeFlow cf)
        {
            var descriptor = cf.LastDescriptor();
            if (descriptor == null)
            {
                CodeFlow.LoadTarget(gen);
            }

            gen.Emit(OpCodes.Ldstr, propertyName);
            gen.Emit(OpCodes.Callvirt, _getItem);
        }
    }

    public class Reg
    {
        public Reg(int v)
        {
            Value = v;
            ValueL = v;
            ValueD = v;
            ValueF = v;
        }

        public void SetValue(object value)
        {
            Value = value == null ? default : (int)value;
            ValueL = value == null ? default : (long)(int)value;
            ValueD = value == null ? default : (double)(int)value;
            ValueF = value == null ? default : (float)(int)value;
        }

        public int Value { get; private set; }

        public long ValueL { get; private set; }

        public double ValueD { get; private set; }

        public float ValueF { get; private set; }

        public void SetValue2(object value)
        {
            Value2 = value == null ? default : (int)value;
            ValueL2 = value == null ? default : (long)(int)value;
            ValueD2 = value == null ? default : (double)(int)value;
            ValueF2 = value == null ? default : (float)(int)value;
        }

        public int Value2 { get; private set; }

        public long ValueL2 { get; private set; }

        public double ValueD2 { get; private set; }

        public float ValueF2 { get; private set; }
    }

    public class LongHolder
    {
        public long SomeLong = 3L;
    }

    public class RecordHolder
    {
        public Dictionary<string, long> Record = new ();

        public LongHolder Expression = new ();

        public void Add(string key, long value)
        {
            Record.Add(key, value);
        }

        public long Get(string key)
        {
            return Record[key];
        }
    }

    public class Foo
    {
        public string Bar()
        {
            return "BAR";
        }

        public string Bar(string arg)
        {
            return arg.ToUpper();
        }
    }

    public class MessageHeaders : Dictionary<string, object>
    {
    }

#pragma warning disable S2326 // Unused type parameters should be removed
    public interface IMessage<T>
#pragma warning restore S2326 // Unused type parameters should be removed
    {
        MessageHeaders Headers { get; }

        IList List { get; }

        int[] IArray { get; }
    }

    public class MyMessage : IMessage<string>
    {
        public MessageHeaders Headers
        {
            get
            {
                var mh = new MessageHeaders
                {
                    { "command", "wibble" },
                    { "command2", "wobble" }
                };
                return mh;
            }
        }

        public int[] IArray
        {
            get
            {
                return new[] { 5, 3 };
            }
        }

        public IList List
        {
            get
            {
                var l = new List<string>
                {
                    "wibble",
                    "wobble"
                };
                return l;
            }
        }

        public string GetKey()
        {
            return "command2";
        }

        public int GetKey2()
        {
            return 1;
        }
    }

    public class Payload2
    {
        private string _var2 = "def";

        public string Var1 { get; } = "abc";

        public object GetField(string name)
        {
            if (name.Equals("Var1"))
            {
                return Var1;
            }
            else if (name.Equals("Var2"))
            {
                return _var2;
            }

            return null;
        }
    }

    public class Payload2Holder
    {
        public Payload2 Payload2 = new ();
    }

    public class FooString
    {
        public string Object { get; } = "hello";
    }

    public class TestClass6
    {
        public static string Apple = "value2";

        public static string Plum { get; } = "value4";

        public string Orange = "value1";

        public long Peach = 34L;

        public string Banana { get; } = "value3";
    }

    public class Two
    {
        public Three Threeee { get; } = new ();

        public override string ToString()
        {
            return "instanceof Two";
        }
    }

    public class Payload
    {
        public Two[] DR { get; } = new[] { new Two() };

        public Two Holder = new ();
    }

    public class TestClass5
    {
        public static int _I;
        public static string _S;

        public static short S1 = 1;
        public static short S2 = 2;
        public static short S3 = 3;

        public static long L1 = 1L;

        public static long L2 = 2L;
        public static long L3 = 3L;

        public static float F1 = 1f;
        public static float F2 = 2f;
        public static float F3 = 3f;

        public static char C1 = 'a';
        public static char C2 = 'b';
        public static char C3 = 'c';

        public static byte B1 = 65;
        public static byte B2 = 66;
        public static byte B3 = 67;

        public static string[] StringArray = new[] { "aaa", "bbb", "ccc" };
        public static int[] IntArray = new[] { 11, 22, 33 };

        public int I;
        public string S;

        public object Obj;

        public string Field;

        public static void Two()
        {
            _I = 1;
        }

        public static string Five()
        {
            return "hello";
        }

        public static long Six()
        {
            return 3277700L;
        }

        public static void Ten(int toset)
        {
            _I = toset;
        }

        public static void Eight(string toset)
        {
            _S = toset;
        }

        public void Reset()
        {
            I = 0;
            _I = 0;
            S = null;
            _S = null;
            Field = null;
        }

        public void One()
        {
            I = 1;
        }

        public string Three()
        {
            return "hello";
        }

        public long Four()
        {
            return 3277700L;
        }

        public void Seven(string toset)
        {
            S = toset;
        }

        public void TakeNumber(object n)
        {
            S = n.ToString();
        }

        public void TakeString(string s)
        {
            S = s;
        }

        public void Nine(int toset)
        {
            I = toset;
        }

        public void Eleven(params string[] vargs)
        {
            if (vargs == null)
            {
                S = string.Empty;
            }
            else
            {
                S = string.Empty;
                foreach (var varg in vargs)
                {
                    S += varg;
                }
            }
        }

        public void Twelve(params int[] vargs)
        {
            if (vargs == null)
            {
                I = 0;
            }
            else
            {
                I = 0;
                foreach (var varg in vargs)
                {
                    I += varg;
                }
            }
        }

        public void Thirteen(string a, params string[] vargs)
        {
            if (vargs == null)
            {
                S = $"{a}::";
            }
            else
            {
                S = $"{a}::";
                foreach (var varg in vargs)
                {
                    S += varg;
                }
            }
        }

        public void Arrayz(params bool[] bs)
        {
            S = string.Empty;
            if (bs != null)
            {
                S = string.Empty;
                foreach (var b in bs)
                {
                    S += b.ToString();
                }
            }
        }

        public void Arrays(params short[] ss)
        {
            S = string.Empty;
            if (ss != null)
            {
                S = string.Empty;
                foreach (var s in ss)
                {
                    S += s.ToString();
                }
            }
        }

        public void Arrayd(params double[] vargs)
        {
            S = string.Empty;
            if (vargs != null)
            {
                S = string.Empty;
                foreach (var v in vargs)
                {
                    S += v.ToString();
                }
            }
        }

        public void Arrayf(params float[] vargs)
        {
            S = string.Empty;
            if (vargs != null)
            {
                S = string.Empty;
                foreach (var v in vargs)
                {
                    S += v.ToString();
                }
            }
        }

        public void Arrayj(params long[] vargs)
        {
            S = string.Empty;
            if (vargs != null)
            {
                S = string.Empty;
                foreach (var v in vargs)
                {
                    S += v.ToString();
                }
            }
        }

        public void Arrayb(params byte[] vargs)
        {
            S = string.Empty;
            if (vargs != null)
            {
                S = string.Empty;
                foreach (var v in vargs)
                {
                    S += v.ToString();
                }
            }
        }

        public void Arrayc(params char[] vargs)
        {
            S = string.Empty;
            if (vargs != null)
            {
                S = string.Empty;
                foreach (var v in vargs)
                {
                    S += v.ToString();
                }
            }
        }

        public void Fourteen(string a, params string[][] vargs)
        {
            if (vargs == null)
            {
                S = $"{a}::";
            }
            else
            {
                S = $"{a}::";
                foreach (var varg in vargs)
                {
                    S += "{";
                    foreach (var v in varg)
                    {
                        S += v;
                    }

                    S += "}";
                }
            }
        }

        public void Fifteen(string a, params int[][] vargs)
        {
            if (vargs == null)
            {
                S = $"{a}::";
            }
            else
            {
                S = $"{a}::";
                foreach (var varg in vargs)
                {
                    S += "{";
                    foreach (var v in varg)
                    {
                        S += v;
                    }

                    S += "}";
                }
            }
        }

        public void Sixteen(params object[] vargs)
        {
            if (vargs == null)
            {
                S = string.Empty;
            }
            else
            {
                S = string.Empty;
                foreach (var varg in vargs)
                {
                    S += varg;
                }
            }
        }
    }

    public class TestClass10
    {
        public string S;

        public void Reset()
        {
            S = null;
        }

        public void Concat1(string arg)
        {
            S = $"::{arg}";
        }

        public void Concat1(params string[] vargs)
        {
            if (vargs == null)
            {
                S = string.Empty;
            }
            else
            {
                S = string.Empty;
                foreach (var varg in vargs)
                {
                    S += varg;
                }
            }
        }

        public void Concat2(object arg)
        {
            S = $"::{arg}";
        }

        public void Concat2(params object[] vargs)
        {
            if (vargs == null)
            {
                S = string.Empty;
            }
            else
            {
                S = string.Empty;
                foreach (var varg in vargs)
                {
                    S += varg;
                }
            }
        }
    }

    public class TestClass8
    {
        public int I { get; }

        public string S { get; }

        public double D { get; }

        public bool Z { get; }

        public TestClass8(int i, string s, double d, bool z)
        {
            I = i;
            S = s;
            D = d;
            Z = z;
        }

        public TestClass8()
        {
        }

        public TestClass8(object i)
        {
            I = (int)i;
        }

        private TestClass8(string a, string b)
        {
            S = a + b;
        }
    }

    public class Obj
    {
        public readonly string Param1;

        public Obj(string param1)
        {
            Param1 = param1;
        }
    }

    public class Obj2
    {
        public readonly string Output;

        public Obj2(params string[] strs)
        {
            var b = new StringBuilder();
            foreach (var p in strs)
            {
                b.Append(p);
            }

            Output = b.ToString();
        }
    }

    public class Obj3
    {
        public readonly string Output;

        public Obj3(params int[] ints)
        {
            var b = new StringBuilder();
            foreach (var p in ints)
            {
                b.Append(p);
            }

            Output = b.ToString();
        }

        public Obj3(string s, float f, params int[] ints)
        {
            var b = new StringBuilder();
            b.Append(s);
            b.Append(':');
            b.Append(f);
            b.Append(':');
            foreach (var p in ints)
            {
                b.Append(p);
            }

            Output = b.ToString();
        }
    }

    public class Obj4
    {
        public readonly string Output;

        public Obj4(int[] ints)
        {
            var b = new StringBuilder();
            foreach (var p in ints)
            {
                b.Append(p);
            }

            Output = b.ToString();
        }
    }

    public class Person
    {
        public int Age { get; set; }

        public Person(int age)
        {
            Age = age;
        }
    }

    public class Person3
    {
        public Person3(string name, int age)
        {
            Age = age;
        }

        public object Age { get; }
    }

    public class Apple : IComparable
    {
        public object GotComparedTo;
        public int I;

        public Apple(int i)
        {
            I = i;
        }

        public void SetValue(int i)
        {
            I = i;
        }

        public int CompareTo(object obj)
        {
            GotComparedTo = obj;
            var that = obj as Apple;
            if (I < that.I)
            {
                return -1;
            }
            else if (I > that.I)
            {
                return +1;
            }
            else
            {
                return 0;
            }
        }
    }

    // For OpNe_SPR14863
    public class MyContext
    {
        public MyContext(Dictionary<string, string> data)
        {
            Data = data;
        }

        public Dictionary<string, string> Data { get; }
    }

    public class TestClass7
    {
        public static string Property = "UK 123".Split(' ')[0];

        public static void Reset()
        {
            var s = "UK 123";
            Property = s.Split(' ')[0];
        }
    }

    public class GenericMessageTestHelper<T>
    {
        public T Payload { get; }

        public GenericMessageTestHelper(T value)
        {
            Payload = value;
        }
    }

    public class GenericMessageTestHelper2<T>
        where T : struct
    {
        public object Payload { get; }

        public GenericMessageTestHelper2(T value)
        {
            Payload = value;
        }
    }

    public class Greeter
    {
        public string World => "world";

        public object GetObject()
        {
            return "object";
        }
    }

    public class PayloadX
    {
        public int valueI = 120;

        public object valueIB = 120;
        public object valueIB58 = 58;
        public object valueIB60 = 60;
        public long valueJ = 120L;
        public object valueJB = 120L;
        public object valueJB58 = 58L;
        public object valueJB60 = 60L;
        public double valueD = 120D;
        public object valueDB = 120D;
        public object valueDB58 = 58D;
        public object valueDB60 = 60D;
        public float valueF = 120F;
        public object valueFB = 120F;
        public object valueFB58 = 58F;
        public object valueFB60 = 60F;
        public byte valueB = 120;
        public byte valueB18 = 18;
        public byte valueB20 = 20;
        public object valueBB = (byte)120;
        public object valueBB18 = (byte)18;
        public object valueBB20 = (byte)20;
        public char valueC = (char)120;
        public object valueCB = (char)120;
        public short valueS = 120;
        public short valueS18 = 18;
        public short valueS20 = 20;
        public object valueSB = (short)120;
        public object valueSB18 = (short)18;
        public object valueSB20 = (short)20;

        public PayloadX payload;

        public PayloadX()
        {
            payload = this;
        }
    }

    public class SomeCompareMethod2
    {
        public static int Negate(int i1)
        {
            return -i1;
        }

        public static string Append(params string[] strings)
        {
            var b = new StringBuilder();
            foreach (var str in strings)
            {
                b.Append(str);
            }

            return b.ToString();
        }

        public static string Append2(params object[] objects)
        {
            var b = new StringBuilder();
            foreach (var obj in objects)
            {
                b.Append(obj);
            }

            return b.ToString();
        }

        public static string Append3(string[] strings)
        {
            var b = new StringBuilder();
            foreach (var str in strings)
            {
                b.Append(str);
            }

            return b.ToString();
        }

        public static string Append4(string s, params string[] strings)
        {
            var b = new StringBuilder();
            b.Append(s).Append("::");
            foreach (var str in strings)
            {
                b.Append(str);
            }

            return b.ToString();
        }

        public static string AppendChar(params char[] values)
        {
            var b = new StringBuilder();
            foreach (var ch in values)
            {
                b.Append(ch);
            }

            return b.ToString();
        }

        public static int Sum(params int[] ints)
        {
            var total = 0;
            foreach (var i in ints)
            {
                total += i;
            }

            return total;
        }

        public static int SumDouble(params double[] values)
        {
            var total = 0;
            foreach (var i in values)
            {
                total += (int)i;
            }

            return total;
        }

        public static int SumFloat(params float[] values)
        {
            var total = 0;
            foreach (var i in values)
            {
                total += (int)i;
            }

            return total;
        }
    }

    public class DelegatingStringFormat
    {
        public static string Format(string s, params object[] args)
        {
            return string.Format(s, args);
        }
    }

    public class Three
    {
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307:Accessible fields should begin with upper-case letter", Justification = "Used in Test")]
        public double four = 0.04d;

        public double Four => four;
    }

    public class StaticsHelper
    {
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307:Accessible fields should begin with upper-case letter", Justification = "Used in Test")]
        public static StaticsHelper sh = new ();
        public static StaticsHelper Fielda = sh;
        public static string Fieldb = "fb";

        public static StaticsHelper MethodA()
        {
            return sh;
        }

        public static string MethodB()
        {
            return "mb";
        }

        public static StaticsHelper PropertyA => sh;

        public static string PropertyB => "pb";

        public override string ToString()
        {
            return "sh";
        }
    }

    public class TestClass1
    {
        public int Index1 = 1;
        public int Index2 = 3;
        public string Word = "abcd";
    }

    public class FooObjectHolder
    {
        public FooObject Foo { get; set; } = new ();
    }

    public class FooObject
    {
        public object GetObject()
        {
            return Object;
        }

        public object Object => "hello";
    }

    public class TestClass4
    {
        public bool GetTrue() => true;

        public bool GetFalse() => false;

        public bool A { get; set; }

        public bool B { get; set; }
    }

    private class TestClass9
    {
        public TestClass9(int i)
        {
        }
    }

    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1400:Access modifier should be declared", Justification = "Used in Test")]
    private sealed class SomeCompareMethod
    {
        // public
        public static int Compare2(object o1, object o2)
        {
            return -1;
        }

        // method not public
        static int Compare(object o1, object o2)
        {
            return -1;
        }
    }
#pragma warning restore SA1307
#pragma warning restore IDE1006
#pragma warning restore IDE0044
#pragma warning restore IDE0051
    #endregion Test Classes
}
