// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Support;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Spring;

public class BooleanExpressionTests : AbstractExpressionTests
{
    [Fact]
    public void TestBooleanTrue()
    {
        Evaluate("true", true, typeof(bool));
    }

    [Fact]
    public void TestBooleanFalse()
    {
        Evaluate("false", false, typeof(bool));
    }

    [Fact]
    public void TestOr()
    {
        Evaluate("false or false", false, typeof(bool));
        Evaluate("false or true", true, typeof(bool));
        Evaluate("true or false", true, typeof(bool));
        Evaluate("true or true", true, typeof(bool));
    }

    [Fact]
    public void TestAnd()
    {
        Evaluate("false and false", false, typeof(bool));
        Evaluate("false and true", false, typeof(bool));
        Evaluate("true and false", false, typeof(bool));
        Evaluate("true and true", true, typeof(bool));
    }

    [Fact]
    public void TestNot()
    {
        Evaluate("!false", true, typeof(bool));
        Evaluate("!true", false, typeof(bool));

        Evaluate("not false", true, typeof(bool));
        Evaluate("NoT true", false, typeof(bool));
    }

    [Fact]
    public void TestCombinations01()
    {
        Evaluate("false and false or true", true, typeof(bool));
        Evaluate("true and false or true", true, typeof(bool));
        Evaluate("true and false or false", false, typeof(bool));
    }

    [Fact]
    public void TestWritability()
    {
        Evaluate("true and true", true, typeof(bool), false);
        Evaluate("true or true", true, typeof(bool), false);
        Evaluate("!false", true, typeof(bool), false);
    }

    [Fact]
    public void TestBooleanErrors01()
    {
        EvaluateAndCheckError("1.0 or false", SpelMessage.TypeConversionError, 0);
        EvaluateAndCheckError("false or 39.4", SpelMessage.TypeConversionError, 9);
        EvaluateAndCheckError("true and 'hello'", SpelMessage.TypeConversionError, 9);
        EvaluateAndCheckError(" 'hello' and 'goodbye'", SpelMessage.TypeConversionError, 1);
        EvaluateAndCheckError("!35.2", SpelMessage.TypeConversionError, 1);
        EvaluateAndCheckError("! 'foob'", SpelMessage.TypeConversionError, 2);
    }

    [Fact]
    public void TestConvertAndHandleNull()
    {
        // SPR-9445
        // without null conversion
        EvaluateAndCheckError("null or true", SpelMessage.TypeConversionError, 0, "null", "System.Boolean");
        EvaluateAndCheckError("null and true", SpelMessage.TypeConversionError, 0, "null", "System.Boolean");
        EvaluateAndCheckError("!null", SpelMessage.TypeConversionError, 1, "null", "System.Boolean");
        EvaluateAndCheckError("null ? 'foo' : 'bar'", SpelMessage.TypeConversionError, 0, "null", "System.Boolean");

        Context.TypeConverter = new StandardTypeConverter(new TestGenericConversionService());

        Evaluate("null or true", true, typeof(bool), false);
        Evaluate("null and true", false, typeof(bool), false);
        Evaluate("!null", true, typeof(bool), false);
        Evaluate("null ? 'foo' : 'bar'", "bar", typeof(string), false);
    }
}
