// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Spring;

public class LiteralTests : AbstractExpressionTests
{
    [Fact]
    public void TestLiteralBoolean01()
    {
        Evaluate("false", "False", typeof(bool));
    }

    [Fact]
    public void TestLiteralBoolean02()
    {
        Evaluate("true", "True", typeof(bool));
    }

    [Fact]
    public void TestLiteralInteger01()
    {
        Evaluate("1", "1", typeof(int));
    }

    [Fact]
    public void TestLiteralInteger02()
    {
        Evaluate("1415", "1415", typeof(int));
    }

    [Fact]
    public void TestLiteralString01()
    {
        Evaluate("'Hello World'", "Hello World", typeof(string));
    }

    [Fact]
    public void TestLiteralString02()
    {
        Evaluate("'joe bloggs'", "joe bloggs", typeof(string));
    }

    [Fact]
    public void TestLiteralString03()
    {
        Evaluate("'hello'", "hello", typeof(string));
    }

    [Fact]
    public void TestLiteralString04()
    {
        Evaluate("'Tony''s Pizza'", "Tony's Pizza", typeof(string));
        Evaluate("'Tony\\r''s Pizza'", "Tony\\r's Pizza", typeof(string));
    }

    [Fact]
    public void TestLiteralString05()
    {
        Evaluate("\"Hello World\"", "Hello World", typeof(string));
    }

    [Fact]
    public void TestLiteralString06()
    {
        Evaluate("\"Hello ' World\"", "Hello ' World", typeof(string));
    }

    [Fact]
    public void TestHexIntLiteral01()
    {
        Evaluate("0x7FFFF", "524287", typeof(int));
        Evaluate("0x7FFFFL", 524_287L, typeof(long));
        Evaluate("0X7FFFF", "524287", typeof(int));
        Evaluate("0X7FFFFl", 524_287L, typeof(long));
    }

    [Fact]
    public void TestLongIntLiteral01()
    {
        Evaluate("0xCAFEBABEL", 3_405_691_582L, typeof(long));
    }

    [Fact]
    public void TestLongIntInteractions01()
    {
        Evaluate("0x20 * 2L", 64L, typeof(long));

        // ask for the result to be made into an Integer
        EvaluateAndAskForReturnType("0x20 * 2L", 64, typeof(int));

        // ask for the result to be made into an Integer knowing that it will not fit
        EvaluateAndCheckError("0x1220 * 0xffffffffL", typeof(int), SpelMessage.TYPE_CONVERSION_ERROR, 0);
    }

    [Fact]
    public void TestSignedIntLiterals()
    {
        Evaluate("-1", -1, typeof(int));
        Evaluate("-0xa", -10, typeof(int));
        Evaluate("-1L", -1L, typeof(long));
        Evaluate("-0x20l", -32L, typeof(long));
    }

    [Fact]
    public void TestLiteralReal01_CreatingDoubles()
    {
        Evaluate("1.25", 1.25d, typeof(double));
        Evaluate("2.99", 2.99d, typeof(double));
        Evaluate("-3.141", -3.141d, typeof(double));
        Evaluate("1.25d", 1.25d, typeof(double));
        Evaluate("2.99d", 2.99d, typeof(double));
        Evaluate("-3.141d", -3.141d, typeof(double));
        Evaluate("1.25D", 1.25d, typeof(double));
        Evaluate("2.99D", 2.99d, typeof(double));
        Evaluate("-3.141D", -3.141d, typeof(double));
    }

    [Fact]
    public void TestLiteralReal02_CreatingFloats()
    {
        // For now, everything becomes a double...
        Evaluate("1.25f", 1.25f, typeof(float));
        Evaluate("2.5f", 2.5f, typeof(float));
        Evaluate("-3.5f", -3.5f, typeof(float));
        Evaluate("1.25F", 1.25f, typeof(float));
        Evaluate("2.5F", 2.5f, typeof(float));
        Evaluate("-3.5F", -3.5f, typeof(float));
    }

    [Fact]
    public void TestLiteralReal03_UsingExponents()
    {
        Evaluate("6.0221415E+23", "6.0221415E+23", typeof(double));
        Evaluate("6.0221415e+23", "6.0221415E+23", typeof(double));
        Evaluate("6.0221415E+23d", "6.0221415E+23", typeof(double));
        Evaluate("6.0221415e+23D", "6.0221415E+23", typeof(double));
        Evaluate("6E2f", 6E2f, typeof(float));
    }

    [Fact]
    public void TestLiteralReal04_BadExpressions()
    {
        ParseAndCheckError("6.1e23e22", SpelMessage.MORE_INPUT, 6, "e22");
        ParseAndCheckError("6.1f23e22", SpelMessage.MORE_INPUT, 4, "23e22");
    }

    [Fact]
    public void TestLiteralNull01()
    {
        Evaluate("null", null, null);
    }

    [Fact]
    public void TestConversions()
    {
        // getting the expression type to be what we want - either:
        Evaluate("T(Convert).ToByte(new Int32(37L))", (byte)37, typeof(byte)); // calling byteValue() on typeof(int)
        EvaluateAndAskForReturnType("new Int32(37)", (byte)37, typeof(byte)); // relying on registered type converters
    }

    [Fact]
    public void TestNotWritable()
    {
        var expr = (SpelExpression)_parser.ParseExpression("37");
        Assert.False(expr.IsWritable(new StandardEvaluationContext()));
        expr = (SpelExpression)_parser.ParseExpression("37L");
        Assert.False(expr.IsWritable(new StandardEvaluationContext()));
        expr = (SpelExpression)_parser.ParseExpression("true");
        Assert.False(expr.IsWritable(new StandardEvaluationContext()));
    }
}
