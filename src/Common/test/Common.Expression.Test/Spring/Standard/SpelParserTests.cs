// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Ast;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using System;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Spring.Standard
{
    public class SpelParserTests
    {
        [Fact]
        public void TheMostBasic()
        {
            var parser = new SpelExpressionParser();
            var expr = parser.ParseRaw("2") as SpelExpression;
            Assert.NotNull(expr);
            Assert.NotNull(expr.AST);
            Assert.Equal(2, expr.GetValue());
            Assert.Equal(typeof(int), expr.GetValueType());
            Assert.Equal(2, expr.AST.GetValue(null));
        }

        [Fact]
        public void ValueType()
        {
            var parser = new SpelExpressionParser();
            var ctx = new StandardEvaluationContext();
            var c = parser.ParseRaw("2").GetValueType();
            Assert.Equal(typeof(int), c);
            c = parser.ParseRaw("12").GetValueType(ctx);
            Assert.Equal(typeof(int), c);
            c = parser.ParseRaw("null").GetValueType();
            Assert.Null(c);
            c = parser.ParseRaw("null").GetValueType(ctx);
            Assert.Null(c);
            var o = parser.ParseRaw("null").GetValue(ctx, typeof(object));
            Assert.Null(o);
        }

        [Fact]
        public void Whitespace()
        {
            var parser = new SpelExpressionParser();
            var expr = parser.ParseRaw("2      +    3");
            Assert.Equal(5, expr.GetValue());
            expr = parser.ParseRaw("2	+	3");
            Assert.Equal(5, expr.GetValue());
            expr = parser.ParseRaw("2\n+\t3");
            Assert.Equal(5, expr.GetValue());
            expr = parser.ParseRaw("2\r\n+\t3");
            Assert.Equal(5, expr.GetValue());
        }

        [Fact]
        public void ArithmeticPlus1()
        {
            var parser = new SpelExpressionParser();
            var expr = parser.ParseRaw("2+2") as SpelExpression;
            Assert.NotNull(expr);
            Assert.NotNull(expr.AST);
            Assert.Equal(4, expr.GetValue());
        }

        [Fact]
        public void ArithmeticPlus2()
        {
            var parser = new SpelExpressionParser();
            var expr = parser.ParseRaw("37+41");
            Assert.Equal(78, expr.GetValue());
        }

        [Fact]
        public void ArithmeticMultiply1()
        {
            var parser = new SpelExpressionParser();
            var expr = parser.ParseRaw("2*3") as SpelExpression;
            Assert.NotNull(expr);
            Assert.NotNull(expr.AST);

            // printAst(expr.getAST(),0);
            Assert.Equal(6, expr.GetValue());
        }

        [Fact]
        public void ArithmeticPrecedence1()
        {
            var parser = new SpelExpressionParser();
            var expr = parser.ParseRaw("2*3+5");
            Assert.Equal(11, expr.GetValue());
        }

        [Fact]
        public void GeneralExpressions()
        {
            var ex = Assert.Throws<SpelParseException>(() =>
           {
               var parser = new SpelExpressionParser();
               parser.ParseRaw("new String");
           });
            ParseExceptionRequirements(ex, SpelMessage.MISSING_CONSTRUCTOR_ARGS, 10);

            ex = Assert.Throws<SpelParseException>(() =>
            {
                var parser = new SpelExpressionParser();
                parser.ParseRaw("new String(3,");
            });

            ParseExceptionRequirements(ex, SpelMessage.RUN_OUT_OF_ARGUMENTS, 10);

            ex = Assert.Throws<SpelParseException>(() =>
            {
                var parser = new SpelExpressionParser();
                parser.ParseRaw("new String(3");
            });

            ParseExceptionRequirements(ex, SpelMessage.RUN_OUT_OF_ARGUMENTS, 10);

            ex = Assert.Throws<SpelParseException>(() =>
            {
                var parser = new SpelExpressionParser();
                parser.ParseRaw("new String(");
            });

            ParseExceptionRequirements(ex, SpelMessage.RUN_OUT_OF_ARGUMENTS, 10);

            ex = Assert.Throws<SpelParseException>(() =>
            {
                var parser = new SpelExpressionParser();
                parser.ParseRaw("\"abc");
            });
            ParseExceptionRequirements(ex, SpelMessage.NON_TERMINATING_DOUBLE_QUOTED_STRING, 0);

            ex = Assert.Throws<SpelParseException>(() =>
            {
                var parser = new SpelExpressionParser();
                parser.ParseRaw("'abc");
            });
            ParseExceptionRequirements(ex, SpelMessage.NON_TERMINATING_QUOTED_STRING, 0);
        }

        [Fact]
        public void ArithmeticPrecedence2()
        {
            var parser = new SpelExpressionParser();
            var expr = parser.ParseRaw("2+3*5");
            Assert.Equal(17, expr.GetValue());
        }

        [Fact]
        public void ArithmeticPrecedence3()
        {
            var expr = new SpelExpressionParser().ParseRaw("3+10/2");
            Assert.Equal(8, expr.GetValue());
        }

        [Fact]
        public void ArithmeticPrecedence4()
        {
            var expr = new SpelExpressionParser().ParseRaw("10/2+3");
            Assert.Equal(8, expr.GetValue());
        }

        [Fact]
        public void ArithmeticPrecedence5()
        {
            var expr = new SpelExpressionParser().ParseRaw("(4+10)/2");
            Assert.Equal(7, expr.GetValue());
        }

        [Fact]
        public void ArithmeticPrecedence6()
        {
            var expr = new SpelExpressionParser().ParseRaw("(3+2)*2");
            Assert.Equal(10, expr.GetValue());
        }

        [Fact]
        public void BooleanOperators()
        {
            var expr = new SpelExpressionParser().ParseRaw("true");
            Assert.True(expr.GetValue<bool>());
            expr = new SpelExpressionParser().ParseRaw("false");
            Assert.False(expr.GetValue<bool>());
            expr = new SpelExpressionParser().ParseRaw("false and false");
            Assert.False(expr.GetValue<bool>());
            expr = new SpelExpressionParser().ParseRaw("true and (true or false)");
            Assert.True(expr.GetValue<bool>());
            expr = new SpelExpressionParser().ParseRaw("true and true or false");
            Assert.True(expr.GetValue<bool>());
            expr = new SpelExpressionParser().ParseRaw("!true");
            Assert.False(expr.GetValue<bool>());
            expr = new SpelExpressionParser().ParseRaw("!(false or true)");
            Assert.False(expr.GetValue<bool>());
        }

        [Fact]
        public void BooleanOperators_symbolic_spr9614()
        {
            var expr = new SpelExpressionParser().ParseRaw("true");
            Assert.True(expr.GetValue<bool>());
            expr = new SpelExpressionParser().ParseRaw("false");
            Assert.False(expr.GetValue<bool>());
            expr = new SpelExpressionParser().ParseRaw("false && false");
            Assert.False(expr.GetValue<bool>());
            expr = new SpelExpressionParser().ParseRaw("true && (true || false)");
            Assert.True(expr.GetValue<bool>());
            expr = new SpelExpressionParser().ParseRaw("true && true || false");
            Assert.True(expr.GetValue<bool>());
            expr = new SpelExpressionParser().ParseRaw("!true");
            Assert.False(expr.GetValue<bool>());
            expr = new SpelExpressionParser().ParseRaw("!(false || true)");
            Assert.False(expr.GetValue<bool>());
        }

        [Fact]
        public void StringLiterals()
        {
            var expr = new SpelExpressionParser().ParseRaw("'howdy'");
            Assert.Equal("howdy", expr.GetValue());
            expr = new SpelExpressionParser().ParseRaw("'hello '' world'");
            Assert.Equal("hello ' world", expr.GetValue());
        }

        [Fact]
        public void StringLiterals2()
        {
            var expr = new SpelExpressionParser().ParseRaw("'howdy'.Substring(0,2)");
            Assert.Equal("ho", expr.GetValue());
        }

        [Fact]
        public void TestStringLiterals_DoubleQuotes_spr9620()
        {
            var expr = new SpelExpressionParser().ParseRaw("\"double quote: \"\".\"");
            Assert.Equal("double quote: \".", expr.GetValue());
            expr = new SpelExpressionParser().ParseRaw("\"hello \"\" world\"");
            Assert.Equal("hello \" world", expr.GetValue());
        }

        [Fact]
        public void TestStringLiterals_DoubleQuotes_spr9620_2()
        {
            var ex = Assert.Throws<SpelParseException>(() => new SpelExpressionParser().ParseRaw("\"double quote: \\\"\\\".\""));

            Assert.Equal(17, ex.Position);
            Assert.Equal(SpelMessage.UNEXPECTED_ESCAPE_CHAR, ex.MessageCode);
        }

        [Fact]
        public void PositionalInformation()
        {
            var expr = new SpelExpressionParser().ParseRaw("true and true or false") as SpelExpression;
            var rootAst = expr.AST;
            var operatorOr = (OpOr)rootAst;
            var operatorAnd = (OpAnd)operatorOr.LeftOperand;
            var rightOrOperand = operatorOr.RightOperand;

            // check position for final 'false'
            Assert.Equal(17, rightOrOperand.StartPosition);
            Assert.Equal(22, rightOrOperand.EndPosition);

            // check position for first 'true'
            Assert.Equal(0, operatorAnd.LeftOperand.StartPosition);
            Assert.Equal(4, operatorAnd.LeftOperand.EndPosition);

            // check position for second 'true'
            Assert.Equal(9, operatorAnd.RightOperand.StartPosition);
            Assert.Equal(13, operatorAnd.RightOperand.EndPosition);

            // check position for OperatorAnd
            Assert.Equal(5, operatorAnd.StartPosition);
            Assert.Equal(8, operatorAnd.EndPosition);

            // check position for OperatorOr
            Assert.Equal(14, operatorOr.StartPosition);
            Assert.Equal(16, operatorOr.EndPosition);
        }

        [Fact]
        public void Test_TokenKind()
        {
            var tk = TokenKind.NOT;
            Assert.False(tk.HasPayload);
            Assert.Equal("NOT(!)", tk.ToString());

            tk = TokenKind.MINUS;
            Assert.False(tk.HasPayload);
            Assert.Equal("MINUS(-)", tk.ToString());

            tk = TokenKind.LITERAL_STRING;
            Assert.Equal("LITERAL_STRING", tk.ToString());
            Assert.True(tk.HasPayload);
        }

        [Fact]
        public void Test_Token()
        {
            var token = new Token(TokenKind.NOT, 0, 3);
            Assert.Equal(TokenKind.NOT, token.Kind);
            Assert.Equal(0, token.StartPos);
            Assert.Equal(3, token.EndPos);
            Assert.Equal("[NOT(!)](0,3)", token.ToString());

            token = new Token(TokenKind.LITERAL_STRING, "abc".ToCharArray(), 0, 3);
            Assert.Equal(TokenKind.LITERAL_STRING, token.Kind);
            Assert.Equal(0, token.StartPos);
            Assert.Equal(3, token.EndPos);
            Assert.Equal("[LITERAL_STRING:abc](0,3)", token.ToString());
        }

        [Fact]
        public void Exceptions()
        {
            var exprEx = new ExpressionException("test");
            Assert.Equal("test", exprEx.SimpleMessage);
            Assert.Equal("test", exprEx.ToDetailedString());
            Assert.Equal("test", exprEx.Message);

            exprEx = new ExpressionException("wibble", "test");
            Assert.Equal("test", exprEx.SimpleMessage);
            Assert.Equal("Expression [wibble]: test", exprEx.ToDetailedString());
            Assert.Equal("Expression [wibble]: test", exprEx.Message);

            exprEx = new ExpressionException("wibble", 3, "test");
            Assert.Equal("test", exprEx.SimpleMessage);
            Assert.Equal("Expression [wibble] @3: test", exprEx.ToDetailedString());
            Assert.Equal("Expression [wibble] @3: test", exprEx.Message);
        }

        [Fact]
        public void ParseMethodsOnNumbers()
        {
            CheckNumber("3.14.ToString()", "3.14", typeof(string));
            CheckNumber("3.ToString()", "3", typeof(string));
        }

        [Fact]
        public void Numerics()
        {
            CheckNumber("2", 2, typeof(int));
            CheckNumber("22", 22, typeof(int));
            CheckNumber("+22", 22, typeof(int));
            CheckNumber("-22", -22, typeof(int));
            CheckNumber("2L", 2L, typeof(long));
            CheckNumber("22l", 22L, typeof(long));

            CheckNumber("0x1", 1, typeof(int));
            CheckNumber("0x1L", 1L, typeof(long));
            CheckNumber("0xa", 10, typeof(int));
            CheckNumber("0xAL", 10L, typeof(long));

            CheckNumberError("0x", SpelMessage.NOT_AN_INTEGER);
            CheckNumberError("0xL", SpelMessage.NOT_A_LONG);
            CheckNumberError(".324", SpelMessage.UNEXPECTED_DATA_AFTER_DOT);
            CheckNumberError("3.4L", SpelMessage.REAL_CANNOT_BE_LONG);

            CheckNumber("3.5f", 3.5f, typeof(float));
            CheckNumber("1.2e3", 1.2e3d, typeof(double));
            CheckNumber("1.2e+3", 1.2e3d, typeof(double));
            CheckNumber("1.2e-3", 1.2e-3d, typeof(double));
            CheckNumber("1.2e3", 1.2e3d, typeof(double));
            CheckNumber("1e+3", 1e3d, typeof(double));
        }

        private void ParseExceptionRequirements(SpelParseException ex, SpelMessage expectedMessage, int expectedPosition)
        {
            Assert.Equal(expectedMessage, ex.MessageCode);
            Assert.Equal(expectedPosition, ex.Position);
            Assert.Contains(ex.ExpressionString, ex.Message);
        }

        private void CheckNumber(string expression, object value, Type type)
        {
            try
            {
                var parser = new SpelExpressionParser();
                var expr = parser.ParseRaw(expression) as SpelExpression;
                var exprVal = expr.GetValue();
                Assert.Equal(value, exprVal);
                Assert.Equal(type, exprVal.GetType());
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        private void CheckNumberError(string expression, SpelMessage expectedMessage)
        {
            var parser = new SpelExpressionParser();
            var ex = Assert.Throws<SpelParseException>(() => parser.ParseRaw(expression));
            Assert.Equal(expectedMessage, ex.MessageCode);
        }
    }
}
