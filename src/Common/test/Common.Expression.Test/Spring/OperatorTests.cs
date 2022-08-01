// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Ast;
using Steeltoe.Common.Expression.Internal.Spring.Standard;
using System;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Spring;

public class OperatorTests : AbstractExpressionTests
{
    [Fact]
    public void TestEqual()
    {
        Evaluate("3 == 5", false, typeof(bool));
        Evaluate("5 == 3", false, typeof(bool));
        Evaluate("6 == 6", true, typeof(bool));
        Evaluate("3.0f == 5.0f", false, typeof(bool));
        Evaluate("3.0f == 3.0f", true, typeof(bool));
        Evaluate("new System.Decimal('5') == new System.Decimal('5')", true, typeof(bool));
        Evaluate("new System.Decimal('3') == new System.Decimal('5')", false, typeof(bool));
        Evaluate("new System.Decimal('5') == new System.Decimal('3')", false, typeof(bool));
        Evaluate("3 == new System.Decimal('5')", false, typeof(bool));
        Evaluate("new System.Decimal('3') == 5", false, typeof(bool));
        Evaluate("3L == new System.Decimal('5')", false, typeof(bool));
        Evaluate("3.0d == new System.Decimal('5')", false, typeof(bool));
        Evaluate("3L == new System.Decimal('3.1')", false, typeof(bool));
        Evaluate("3.0d == new System.Decimal('3.1')", false, typeof(bool));
        Evaluate("3.0d == new System.Decimal('3.0')", true, typeof(bool));
        Evaluate("3.0f == 3.0d", true, typeof(bool));
        Evaluate("10 == '10'", false, typeof(bool));
        Evaluate("'abc' == 'abc'", true, typeof(bool));
        Evaluate("'abc' == new System.Text.StringBuilder('abc').ToString()", true, typeof(bool));
        Evaluate("'abc' == 'def'", false, typeof(bool));
        Evaluate("'abc' == null", false, typeof(bool));
        Evaluate("new Steeltoe.Common.Expression.Internal.Spring.OperatorTests$SubComparable() == new Steeltoe.Common.Expression.Internal.Spring.OperatorTests$OtherSubComparable()", true, typeof(bool));

        Evaluate("3 eq 5", false, typeof(bool));
        Evaluate("5 eQ 3", false, typeof(bool));
        Evaluate("6 Eq 6", true, typeof(bool));
        Evaluate("3.0f eq 5.0f", false, typeof(bool));
        Evaluate("3.0f EQ 3.0f", true, typeof(bool));
        Evaluate("new System.Decimal('5') eq new System.Decimal('5')", true, typeof(bool));
        Evaluate("new System.Decimal('3') eq new System.Decimal('5')", false, typeof(bool));
        Evaluate("new System.Decimal('5') eq new System.Decimal('3')", false, typeof(bool));
        Evaluate("3 eq new System.Decimal('5')", false, typeof(bool));
        Evaluate("new System.Decimal('3') eq 5", false, typeof(bool));
        Evaluate("3L eq new System.Decimal('5')", false, typeof(bool));
        Evaluate("3.0d eq new System.Decimal('5')", false, typeof(bool));
        Evaluate("3L eq new System.Decimal('3.1')", false, typeof(bool));
        Evaluate("3.0d eq new System.Decimal('3.1')", false, typeof(bool));
        Evaluate("3.0d eq new System.Decimal('3.0')", true, typeof(bool));
        Evaluate("3.0f eq 3.0d", true, typeof(bool));
        Evaluate("10 eq '10'", false, typeof(bool));
        Evaluate("'abc' eq 'abc'", true, typeof(bool));
        Evaluate("'abc' eq new System.Text.StringBuilder('abc').ToString()", true, typeof(bool));
        Evaluate("'abc' eq 'def'", false, typeof(bool));
        Evaluate("'abc' eq null", false, typeof(bool));
        Evaluate("new Steeltoe.Common.Expression.Internal.Spring.OperatorTests$SubComparable() eq new Steeltoe.Common.Expression.Internal.Spring.OperatorTests$OtherSubComparable()", true, typeof(bool));
    }

    [Fact]
    public void TestNotEqual()
    {
        Evaluate("3 != 5", true, typeof(bool));
        Evaluate("5 != 3", true, typeof(bool));
        Evaluate("6 != 6", false, typeof(bool));
        Evaluate("3.0f != 5.0f", true, typeof(bool));
        Evaluate("3.0f != 3.0f", false, typeof(bool));
        Evaluate("new Decimal('5') != new Decimal('5')", false, typeof(bool));
        Evaluate("new Decimal('3') != new Decimal('5')", true, typeof(bool));
        Evaluate("new Decimal('5') != new Decimal('3')", true, typeof(bool));
        Evaluate("3 != new Decimal('5')", true, typeof(bool));
        Evaluate("new Decimal('3') != 5", true, typeof(bool));
        Evaluate("3L != new Decimal('5')", true, typeof(bool));
        Evaluate("3.0d != new Decimal('5')", true, typeof(bool));
        Evaluate("3L != new Decimal('3.1')", true, typeof(bool));
        Evaluate("3.0d != new Decimal('3.1')", true, typeof(bool));
        Evaluate("3.0d != new Decimal('3.0')", false, typeof(bool));
        Evaluate("3.0f != 3.0d", false, typeof(bool));
        Evaluate("10 != '10'", true, typeof(bool));
        Evaluate("'abc' != 'abc'", false, typeof(bool));
        Evaluate("'abc' != new System.Text.StringBuilder('abc').ToString()", false, typeof(bool));
        Evaluate("'abc' != 'def'", true, typeof(bool));
        Evaluate("'abc' != null", true, typeof(bool));
        Evaluate("new Steeltoe.Common.Expression.Internal.Spring.OperatorTests$SubComparable() != new Steeltoe.Common.Expression.Internal.Spring.OperatorTests$OtherSubComparable()", false, typeof(bool));

        Evaluate("3 ne 5", true, typeof(bool));
        Evaluate("5 nE 3", true, typeof(bool));
        Evaluate("6 Ne 6", false, typeof(bool));
        Evaluate("3.0f NE 5.0f", true, typeof(bool));
        Evaluate("3.0f ne 3.0f", false, typeof(bool));
        Evaluate("new Decimal('5') ne new Decimal('5')", false, typeof(bool));
        Evaluate("new Decimal('3') ne new Decimal('5')", true, typeof(bool));
        Evaluate("new Decimal('5') ne new Decimal('3')", true, typeof(bool));
        Evaluate("3 ne new Decimal('5')", true, typeof(bool));
        Evaluate("new Decimal('3') ne 5", true, typeof(bool));
        Evaluate("3L ne new Decimal('5')", true, typeof(bool));
        Evaluate("3.0d ne new Decimal('5')", true, typeof(bool));
        Evaluate("3L ne new Decimal('3.1')", true, typeof(bool));
        Evaluate("3.0d ne new Decimal('3.1')", true, typeof(bool));
        Evaluate("3.0d ne new Decimal('3.0')", false, typeof(bool));
        Evaluate("3.0f ne 3.0d", false, typeof(bool));
        Evaluate("10 ne '10'", true, typeof(bool));
        Evaluate("'abc' ne 'abc'", false, typeof(bool));
        Evaluate("'abc' ne new System.Text.StringBuilder('abc').ToString()", false, typeof(bool));
        Evaluate("'abc' ne 'def'", true, typeof(bool));
        Evaluate("'abc' ne null", true, typeof(bool));
        Evaluate("new Steeltoe.Common.Expression.Internal.Spring.OperatorTests$SubComparable() ne new Steeltoe.Common.Expression.Internal.Spring.OperatorTests$OtherSubComparable()", false, typeof(bool));
    }

    [Fact]
    public void TestLessThan()
    {
        Evaluate("5 < 5", false, typeof(bool));
        Evaluate("3 < 5", true, typeof(bool));
        Evaluate("5 < 3", false, typeof(bool));
        Evaluate("3L < 5L", true, typeof(bool));
        Evaluate("5L < 3L", false, typeof(bool));
        Evaluate("3.0d < 5.0d", true, typeof(bool));
        Evaluate("5.0d < 3.0d", false, typeof(bool));
        Evaluate("new Decimal('3') < new Decimal('5')", true, typeof(bool));
        Evaluate("new Decimal('5') < new Decimal('3')", false, typeof(bool));
        Evaluate("3 < new Decimal('5')", true, typeof(bool));
        Evaluate("new Decimal('3') < 5", true, typeof(bool));
        Evaluate("3L < new Decimal('5')", true, typeof(bool));
        Evaluate("3.0d < new Decimal('5')", true, typeof(bool));
        Evaluate("3L < new Decimal('3.1')", true, typeof(bool));
        Evaluate("3.0d < new Decimal('3.1')", true, typeof(bool));
        Evaluate("3.0d < new Decimal('3.0')", false, typeof(bool));
        Evaluate("'abc' < 'def'", true, typeof(bool));
        Evaluate("'abc' < new System.Text.StringBuilder('def').ToString()", true, typeof(bool));
        Evaluate("'def' < 'abc'", false, typeof(bool));

        Evaluate("3 lt 5", true, typeof(bool));
        Evaluate("5 lt 3", false, typeof(bool));
        Evaluate("3L lt 5L", true, typeof(bool));
        Evaluate("5L lt 3L", false, typeof(bool));
        Evaluate("3.0d lT 5.0d", true, typeof(bool));
        Evaluate("5.0d Lt 3.0d", false, typeof(bool));
        Evaluate("new Decimal('3') lt new Decimal('5')", true, typeof(bool));
        Evaluate("new Decimal('5') lt new Decimal('3')", false, typeof(bool));
        Evaluate("3 lt new Decimal('5')", true, typeof(bool));
        Evaluate("new Decimal('3') lt 5", true, typeof(bool));
        Evaluate("3L lt new Decimal('5')", true, typeof(bool));
        Evaluate("3.0d lt new Decimal('5')", true, typeof(bool));
        Evaluate("3L lt new Decimal('3.1')", true, typeof(bool));
        Evaluate("3.0d lt new Decimal('3.1')", true, typeof(bool));
        Evaluate("3.0d lt new Decimal('3.0')", false, typeof(bool));
        Evaluate("'abc' LT 'def'", true, typeof(bool));
        Evaluate("'abc' lt new System.Text.StringBuilder('def').ToString()", true, typeof(bool));
        Evaluate("'def' lt 'abc'", false, typeof(bool));
    }

    [Fact]
    public void TestLessThanOrEqual()
    {
        Evaluate("3 <= 5", true, typeof(bool));
        Evaluate("5 <= 3", false, typeof(bool));
        Evaluate("6 <= 6", true, typeof(bool));
        Evaluate("3L <= 5L", true, typeof(bool));
        Evaluate("5L <= 3L", false, typeof(bool));
        Evaluate("5L <= 5L", true, typeof(bool));
        Evaluate("3.0d <= 5.0d", true, typeof(bool));
        Evaluate("5.0d <= 3.0d", false, typeof(bool));
        Evaluate("5.0d <= 5.0d", true, typeof(bool));
        Evaluate("new Decimal('5') <= new Decimal('5')", true, typeof(bool));
        Evaluate("new Decimal('3') <= new Decimal('5')", true, typeof(bool));
        Evaluate("new Decimal('5') <= new Decimal('3')", false, typeof(bool));
        Evaluate("3 <= new Decimal('5')", true, typeof(bool));
        Evaluate("new Decimal('3') <= 5", true, typeof(bool));
        Evaluate("3L <= new Decimal('5')", true, typeof(bool));
        Evaluate("3.0d <= new Decimal('5')", true, typeof(bool));
        Evaluate("3L <= new Decimal('3.1')", true, typeof(bool));
        Evaluate("3.0d <= new Decimal('3.1')", true, typeof(bool));
        Evaluate("3.0d <= new Decimal('3.0')", true, typeof(bool));
        Evaluate("'abc' <= 'def'", true, typeof(bool));
        Evaluate("'def' <= 'abc'", false, typeof(bool));
        Evaluate("'abc' <= 'abc'", true, typeof(bool));

        Evaluate("3 le 5", true, typeof(bool));
        Evaluate("5 le 3", false, typeof(bool));
        Evaluate("6 Le 6", true, typeof(bool));
        Evaluate("3L lE 5L", true, typeof(bool));
        Evaluate("5L LE 3L", false, typeof(bool));
        Evaluate("5L le 5L", true, typeof(bool));
        Evaluate("3.0d LE 5.0d", true, typeof(bool));
        Evaluate("5.0d lE 3.0d", false, typeof(bool));
        Evaluate("5.0d Le 5.0d", true, typeof(bool));
        Evaluate("new Decimal('5') le new Decimal('5')", true, typeof(bool));
        Evaluate("new Decimal('3') le new Decimal('5')", true, typeof(bool));
        Evaluate("new Decimal('5') le new Decimal('3')", false, typeof(bool));
        Evaluate("3 le new Decimal('5')", true, typeof(bool));
        Evaluate("new Decimal('3') le 5", true, typeof(bool));
        Evaluate("3L le new Decimal('5')", true, typeof(bool));
        Evaluate("3.0d le new Decimal('5')", true, typeof(bool));
        Evaluate("3L le new Decimal('3.1')", true, typeof(bool));
        Evaluate("3.0d le new Decimal('3.1')", true, typeof(bool));
        Evaluate("3.0d le new Decimal('3.0')", true, typeof(bool));
        Evaluate("'abc' Le 'def'", true, typeof(bool));
        Evaluate("'def' LE 'abc'", false, typeof(bool));
        Evaluate("'abc' le 'abc'", true, typeof(bool));
    }

    [Fact]
    public void TestGreaterThan()
    {
        Evaluate("3 > 5", false, typeof(bool));
        Evaluate("5 > 3", true, typeof(bool));
        Evaluate("3L > 5L", false, typeof(bool));
        Evaluate("5L > 3L", true, typeof(bool));
        Evaluate("3.0d > 5.0d", false, typeof(bool));
        Evaluate("5.0d > 3.0d", true, typeof(bool));
        Evaluate("new Decimal('3') > new Decimal('5')", false, typeof(bool));
        Evaluate("new Decimal('5') > new Decimal('3')", true, typeof(bool));
        Evaluate("3 > new Decimal('5')", false, typeof(bool));
        Evaluate("new Decimal('3') > 5", false, typeof(bool));
        Evaluate("3L > new Decimal('5')", false, typeof(bool));
        Evaluate("3.0d > new Decimal('5')", false, typeof(bool));
        Evaluate("3L > new Decimal('3.1')", false, typeof(bool));
        Evaluate("3.0d > new Decimal('3.1')", false, typeof(bool));
        Evaluate("3.0d > new Decimal('3.0')", false, typeof(bool));
        Evaluate("'abc' > 'def'", false, typeof(bool));
        Evaluate("'abc' > new System.Text.StringBuilder('def').ToString()", false, typeof(bool));
        Evaluate("'def' > 'abc'", true, typeof(bool));

        Evaluate("3 gt 5", false, typeof(bool));
        Evaluate("5 gt 3", true, typeof(bool));
        Evaluate("3L gt 5L", false, typeof(bool));
        Evaluate("5L gt 3L", true, typeof(bool));
        Evaluate("3.0d gt 5.0d", false, typeof(bool));
        Evaluate("5.0d gT 3.0d", true, typeof(bool));
        Evaluate("new Decimal('3') gt new Decimal('5')", false, typeof(bool));
        Evaluate("new Decimal('5') gt new Decimal('3')", true, typeof(bool));
        Evaluate("3 gt new Decimal('5')", false, typeof(bool));
        Evaluate("new Decimal('3') gt 5", false, typeof(bool));
        Evaluate("3L gt new Decimal('5')", false, typeof(bool));
        Evaluate("3.0d gt new Decimal('5')", false, typeof(bool));
        Evaluate("3L gt new Decimal('3.1')", false, typeof(bool));
        Evaluate("3.0d gt new Decimal('3.1')", false, typeof(bool));
        Evaluate("3.0d gt new Decimal('3.0')", false, typeof(bool));
        Evaluate("'abc' Gt 'def'", false, typeof(bool));
        Evaluate("'abc' gt new System.Text.StringBuilder('def').ToString()", false, typeof(bool));
        Evaluate("'def' GT 'abc'", true, typeof(bool));
    }

    [Fact]
    public void TestGreaterThanOrEqual()
    {
        Evaluate("3 >= 5", false, typeof(bool));
        Evaluate("5 >= 3", true, typeof(bool));
        Evaluate("6 >= 6", true, typeof(bool));
        Evaluate("3L >= 5L", false, typeof(bool));
        Evaluate("5L >= 3L", true, typeof(bool));
        Evaluate("5L >= 5L", true, typeof(bool));
        Evaluate("3.0d >= 5.0d", false, typeof(bool));
        Evaluate("5.0d >= 3.0d", true, typeof(bool));
        Evaluate("5.0d >= 5.0d", true, typeof(bool));
        Evaluate("new Decimal('5') >= new Decimal('5')", true, typeof(bool));
        Evaluate("new Decimal('3') >= new Decimal('5')", false, typeof(bool));
        Evaluate("new Decimal('5') >= new Decimal('3')", true, typeof(bool));
        Evaluate("3 >= new Decimal('5')", false, typeof(bool));
        Evaluate("new Decimal('3') >= 5", false, typeof(bool));
        Evaluate("3L >= new Decimal('5')", false, typeof(bool));
        Evaluate("3.0d >= new Decimal('5')", false, typeof(bool));
        Evaluate("3L >= new Decimal('3.1')", false, typeof(bool));
        Evaluate("3.0d >= new Decimal('3.1')", false, typeof(bool));
        Evaluate("3.0d >= new Decimal('3.0')", true, typeof(bool));
        Evaluate("'abc' >= 'def'", false, typeof(bool));
        Evaluate("'def' >= 'abc'", true, typeof(bool));
        Evaluate("'abc' >= 'abc'", true, typeof(bool));

        Evaluate("3 GE 5", false, typeof(bool));
        Evaluate("5 gE 3", true, typeof(bool));
        Evaluate("6 Ge 6", true, typeof(bool));
        Evaluate("3L ge 5L", false, typeof(bool));
        Evaluate("5L ge 3L", true, typeof(bool));
        Evaluate("5L ge 5L", true, typeof(bool));
        Evaluate("3.0d ge 5.0d", false, typeof(bool));
        Evaluate("5.0d ge 3.0d", true, typeof(bool));
        Evaluate("5.0d ge 5.0d", true, typeof(bool));
        Evaluate("new Decimal('5') ge new Decimal('5')", true, typeof(bool));
        Evaluate("new Decimal('3') ge new Decimal('5')", false, typeof(bool));
        Evaluate("new Decimal('5') ge new Decimal('3')", true, typeof(bool));
        Evaluate("3 ge new Decimal('5')", false, typeof(bool));
        Evaluate("new Decimal('3') ge 5", false, typeof(bool));
        Evaluate("3L ge new Decimal('5')", false, typeof(bool));
        Evaluate("3.0d ge new Decimal('5')", false, typeof(bool));
        Evaluate("3L ge new Decimal('3.1')", false, typeof(bool));
        Evaluate("3.0d ge new Decimal('3.1')", false, typeof(bool));
        Evaluate("3.0d ge new Decimal('3.0')", true, typeof(bool));
        Evaluate("'abc' ge 'def'", false, typeof(bool));
        Evaluate("'def' ge 'abc'", true, typeof(bool));
        Evaluate("'abc' ge 'abc'", true, typeof(bool));
    }

    [Fact]
    public void TestIntegerLiteral()
    {
        Evaluate("3", 3, typeof(int));
    }

    [Fact]
    public void TestRealLiteral()
    {
        Evaluate("3.5", 3.5d, typeof(double));
    }

    [Fact]
    public void TestMultiplyStringInt()
    {
        Evaluate("'a' * 5", "aaaaa", typeof(string));
    }

    [Fact]
    public void TestMultiplyDoubleDoubleGivesDouble()
    {
        Evaluate("3.0d * 5.0d", 15.0d, typeof(double));
    }

    [Fact]
    public void TestMixedOperandsBigDecimal()
    {
        Evaluate("3 * new Decimal('5')", 15M, typeof(decimal));
        Evaluate("3L * new Decimal('5')", 15M, typeof(decimal));
        Evaluate("3.0d * new Decimal('5')", 15.0M, typeof(decimal));

        Evaluate("3 + new Decimal('5')", 8M, typeof(decimal));
        Evaluate("3L + new Decimal('5')", 8M, typeof(decimal));
        Evaluate("3.0d + new Decimal('5')", 8.0M, typeof(decimal));

        Evaluate("3 - new Decimal('5')", -2M, typeof(decimal));
        Evaluate("3L - new Decimal('5')", -2M, typeof(decimal));
        Evaluate("3.0d - new Decimal('5')", -2.0M, typeof(decimal));

        Evaluate("3 / new Decimal('5')", 0.6M, typeof(decimal));
        Evaluate("3 / new Decimal('5.0')", 0.6M, typeof(decimal));
        Evaluate("3 / new Decimal('5.00')", 0.60M, typeof(decimal));
        Evaluate("3L / new Decimal('5.0')", 0.6M, typeof(decimal));
        Evaluate("3.0d / new Decimal('5.0')", 0.6M, typeof(decimal));

        Evaluate("5 % new Decimal('3')", 2M, typeof(decimal));
        Evaluate("3 % new Decimal('5')", 3M, typeof(decimal));
        Evaluate("3L % new Decimal('5')", 3M, typeof(decimal));
        Evaluate("3.0d % new Decimal('5')", 3.0M, typeof(decimal));
    }

    [Fact]
    public void TestMathOperatorAdd02()
    {
        Evaluate("'hello' + ' ' + 'world'", "hello world", typeof(string));
    }

    [Fact]
    public void TestMathOperatorsInChains()
    {
        Evaluate("1+2+3", 6, typeof(int));
        Evaluate("2*3*4", 24, typeof(int));
        Evaluate("12-1-2", 9, typeof(int));
    }

    [Fact]
    public void TestIntegerArithmetic()
    {
        Evaluate("2 + 4", "6", typeof(int));
        Evaluate("5 - 4", "1", typeof(int));
        Evaluate("3 * 5", 15, typeof(int));
        Evaluate("3.2d * 5", 16.0d, typeof(double));
        Evaluate("3 * 5f", 15f, typeof(float));
        Evaluate("3 / 1", 3, typeof(int));
        Evaluate("3 % 2", 1, typeof(int));
        Evaluate("3 mod 2", 1, typeof(int));
        Evaluate("3 mOd 2", 1, typeof(int));
        Evaluate("3 Mod 2", 1, typeof(int));
        Evaluate("3 MOD 2", 1, typeof(int));
    }

    [Fact]
    public void TestPlus()
    {
        Evaluate("7 + 2", "9", typeof(int));
        Evaluate("3.0f + 5.0f", 8.0f, typeof(float));
        Evaluate("3.0d + 5.0d", 8.0d, typeof(double));
        Evaluate("3 + new Decimal('5')", 8M, typeof(decimal));

        Evaluate("'ab' + 2", "ab2", typeof(string));
        Evaluate("2 + 'a'", "2a", typeof(string));
        Evaluate("'ab' + null", "abnull", typeof(string));
        Evaluate("null + 'ab'", "nullab", typeof(string));

        // AST:
        var expr = (SpelExpression)Parser.ParseExpression("+3");
        Assert.Equal("+3", expr.ToStringAst());
        expr = (SpelExpression)Parser.ParseExpression("2+3");
        Assert.Equal("(2 + 3)", expr.ToStringAst());

        // use as a unary operator
        Evaluate("+5d", 5d, typeof(double));
        Evaluate("+5L", 5L, typeof(long));
        Evaluate("+5", 5, typeof(int));
        Evaluate("+new Decimal('5')", 5M, typeof(decimal));
        EvaluateAndCheckError("+'abc'", SpelMessage.OperatorNotSupportedBetweenTypes);

        // string concatenation
        Evaluate("'abc'+'def'", "abcdef", typeof(string));

        Evaluate("5 + new Int32('37')", 42, typeof(int));
    }

    [Fact]
    public void TestMinus()
    {
        Evaluate("'c' - 2", "a", typeof(string));
        Evaluate("3.0f - 5.0f", -2.0f, typeof(float));
        EvaluateAndCheckError("'ab' - 2", SpelMessage.OperatorNotSupportedBetweenTypes);
        EvaluateAndCheckError("2-'ab'", SpelMessage.OperatorNotSupportedBetweenTypes);
        var expr = (SpelExpression)Parser.ParseExpression("-3");
        Assert.Equal("-3", expr.ToStringAst());
        expr = (SpelExpression)Parser.ParseExpression("2-3");
        Assert.Equal("(2 - 3)", expr.ToStringAst());

        Evaluate("-5d", -5d, typeof(double));
        Evaluate("-5L", -5L, typeof(long));
        Evaluate("-5", -5, typeof(int));
        Evaluate("-new Decimal('5')", -5M, typeof(decimal));
        EvaluateAndCheckError("-'abc'", SpelMessage.OperatorNotSupportedBetweenTypes);
    }

    [Fact]
    public void TestModulus()
    {
        Evaluate("3%2", 1, typeof(int));
        Evaluate("3L%2L", 1L, typeof(long));
        Evaluate("3.0f%2.0f", 1f, typeof(float));
        Evaluate("5.0d % 3.1d", 1.9d, typeof(double));
        Evaluate("new Decimal('5') % new Decimal('3')", 2M, typeof(decimal));
        Evaluate("new Decimal('5') % 3", 2M, typeof(decimal));
        EvaluateAndCheckError("'abc'%'def'", SpelMessage.OperatorNotSupportedBetweenTypes);
    }

    [Fact]
    public void TestDivide()
    {
        Evaluate("3.0f / 5.0f", 0.6f, typeof(float));
        Evaluate("4L/2L", 2L, typeof(long));
        Evaluate("3.0f div 5.0f", 0.6f, typeof(float));
        Evaluate("4L DIV 2L", 2L, typeof(long));
        Evaluate("new Decimal('3') / 5", .6M, typeof(decimal));
        Evaluate("new Decimal('3.0') / 5", 0.6M, typeof(decimal));
        Evaluate("new Decimal('3.00') / 5", 0.60M, typeof(decimal));
        Evaluate("new Decimal('3.00') / new Decimal('5.0000')", 0.6000M, typeof(decimal));
        EvaluateAndCheckError("'abc'/'def'", SpelMessage.OperatorNotSupportedBetweenTypes);
    }

    [Fact]
    public void TestMathOperatorDivide_ConvertToDouble()
    {
        EvaluateAndAskForReturnType("8/4", 2.0d, typeof(double));
    }

    [Fact]
    public void TestMathOperatorDivide04_ConvertToFloat()
    {
        EvaluateAndAskForReturnType("8/4", 2.0f, typeof(float));
    }

    [Fact]
    public void TestDoubles()
    {
        Evaluate("3.0d == 5.0d", false, typeof(bool));
        Evaluate("3.0d == 3.0d", true, typeof(bool));
        Evaluate("3.0d != 5.0d", true, typeof(bool));
        Evaluate("3.0d != 3.0d", false, typeof(bool));
        Evaluate("3.0d + 5.0d", 8.0d, typeof(double));
        Evaluate("3.0d - 5.0d", -2.0d, typeof(double));
        Evaluate("3.0d * 5.0d", 15.0d, typeof(double));
        Evaluate("3.0d / 5.0d", 0.6d, typeof(double));
        Evaluate("6.0d % 3.5d", 2.5d, typeof(double));
    }

    [Fact]
    public void TestBigDecimals()
    {
        Evaluate("3 + new Decimal('5')", 8M, typeof(decimal));
        Evaluate("3 - new Decimal('5')", -2M, typeof(decimal));
        Evaluate("3 * new Decimal('5')", 15M, typeof(decimal));
        Evaluate("3 / new Decimal('5')", .6M, typeof(decimal));
        Evaluate("5 % new Decimal('3')", 2M, typeof(decimal));
        Evaluate("new Decimal('5') % 3", 2M, typeof(decimal));
        Evaluate("new Decimal('5') ^ 3", 125M, typeof(decimal));
    }

    [Fact]
    public void TestOperatorNames()
    {
        var node = GetOperatorNode((SpelExpression)Parser.ParseExpression("1==3"));
        Assert.Equal("==", node.OperatorName);

        node = GetOperatorNode((SpelExpression)Parser.ParseExpression("1!=3"));
        Assert.Equal("!=", node.OperatorName);

        node = GetOperatorNode((SpelExpression)Parser.ParseExpression("3/3"));
        Assert.Equal("/", node.OperatorName);

        node = GetOperatorNode((SpelExpression)Parser.ParseExpression("3+3"));
        Assert.Equal("+", node.OperatorName);

        node = GetOperatorNode((SpelExpression)Parser.ParseExpression("3-3"));
        Assert.Equal("-", node.OperatorName);

        node = GetOperatorNode((SpelExpression)Parser.ParseExpression("3<4"));
        Assert.Equal("<", node.OperatorName);

        node = GetOperatorNode((SpelExpression)Parser.ParseExpression("3<=4"));
        Assert.Equal("<=", node.OperatorName);

        node = GetOperatorNode((SpelExpression)Parser.ParseExpression("3*4"));
        Assert.Equal("*", node.OperatorName);

        node = GetOperatorNode((SpelExpression)Parser.ParseExpression("3%4"));
        Assert.Equal("%", node.OperatorName);

        node = GetOperatorNode((SpelExpression)Parser.ParseExpression("3>=4"));
        Assert.Equal(">=", node.OperatorName);

        node = GetOperatorNode((SpelExpression)Parser.ParseExpression("3 between 4"));
        Assert.Equal("between", node.OperatorName);

        node = GetOperatorNode((SpelExpression)Parser.ParseExpression("3 ^ 4"));
        Assert.Equal("^", node.OperatorName);
    }

    [Fact]
    public void TestOperatorOverloading()
    {
        EvaluateAndCheckError("'a' * '2'", SpelMessage.OperatorNotSupportedBetweenTypes);
        EvaluateAndCheckError("'a' ^ '2'", SpelMessage.OperatorNotSupportedBetweenTypes);
    }

    [Fact]
    public void TestPower()
    {
        Evaluate("3^2", 9, typeof(int));
        Evaluate("3.0d^2.0d", 9.0d, typeof(double));
        Evaluate("3L^2L", 9L, typeof(long));
        Evaluate("(2^32)^2", -9_223_372_036_854_775_808L, typeof(long));
        Evaluate("new Decimal('5') ^ 3", 125M, typeof(decimal));
    }

    [Fact]
    public void TestMixedOperands_FloatsAndDoubles()
    {
        Evaluate("3.0d + 5.0f", 8.0d, typeof(double));
        Evaluate("3.0D - 5.0f", -2.0d, typeof(double));
        Evaluate("3.0f * 5.0d", 15.0d, typeof(double));
        Evaluate("3.0f / 5.0D", 0.6d, typeof(double));
        Evaluate("5.0D % 3f", 2.0d, typeof(double));
    }

    [Fact]
    public void TestMixedOperands_DoublesAndIntegers()
    {
        Evaluate("3.0d + 5", 8.0d, typeof(double));
        Evaluate("3.0D - 5", -2.0d, typeof(double));
        Evaluate("3.0f * 5", 15.0f, typeof(float));
        Evaluate("6.0f / 2", 3.0f, typeof(float));
        Evaluate("6.0f / 4", 1.5f, typeof(float));
        Evaluate("5.0D % 3", 2.0d, typeof(double));
        Evaluate("5.5D % 3", 2.5, typeof(double));
    }

    [Fact]
    public void TestStrings()
    {
        Evaluate("'abc' == 'abc'", true, typeof(bool));
        Evaluate("'abc' == 'def'", false, typeof(bool));
        Evaluate("'abc' != 'abc'", false, typeof(bool));
        Evaluate("'abc' != 'def'", true, typeof(bool));
    }

    [Fact]
    public void TestLongs()
    {
        Evaluate("3L == 4L", false, typeof(bool));
        Evaluate("3L == 3L", true, typeof(bool));
        Evaluate("3L != 4L", true, typeof(bool));
        Evaluate("3L != 3L", false, typeof(bool));
        Evaluate("3L * 50L", 150L, typeof(long));
        Evaluate("3L + 50L", 53L, typeof(long));
        Evaluate("3L - 50L", -47L, typeof(long));
    }

    private Operator GetOperatorNode(SpelExpression expr)
    {
        var node = expr.Ast;
        return FindOperator(node);
    }

    private Operator FindOperator(ISpelNode node)
    {
        if (node is Operator operatorNode)
        {
            return operatorNode;
        }

        var childCount = node.ChildCount;
        for (var i = 0; i < childCount; i++)
        {
            var possible = FindOperator(node.GetChild(i));
            if (possible != null)
            {
                return possible;
            }
        }

        return null;
    }

    public class BaseComparable : IComparable
    {
        public int CompareTo(object obj)
        {
            return 0;
        }
    }

    public class SubComparable : BaseComparable
    {
    }

    public class OtherSubComparable : BaseComparable
    {
    }
}
