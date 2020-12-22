// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Spring.Support;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Steeltoe.Common.Expression.Spring.Ast
{
    public class OpPlusTests
    {
        [Fact]
        public void Test_EmptyOperands()
        {
            Assert.Throws<ArgumentException>(() => new OpPlus(-1, -1));
        }

        [Fact]
        public void Test_UnaryPlusWithStringLiteral()
        {
            var expressionState = new ExpressionState(new StandardEvaluationContext());

            var str = new StringLiteral("word", -1, -1, "word");

            var o = new OpPlus(-1, -1, str);
            Assert.Throws<SpelEvaluationException>(() => o.GetValueInternal(expressionState));
        }

        [Fact]
        public void Test_UnaryPlusWithNumberOperand()
        {
            var expressionState = new ExpressionState(new StandardEvaluationContext());
            {
                var realLiteral = new RealLiteral("123.00", -1, -1, 123.0);
                var o = new OpPlus(-1, -1, realLiteral);
                var value = o.GetValueInternal(expressionState);

                Assert.Equal(typeof(double), value.TypeDescriptor);
                Assert.Equal(typeof(double), value.TypeDescriptor);
                Assert.Equal(realLiteral.GetLiteralValue().Value, value.Value);
            }

            {
                var intLiteral = new IntLiteral("123", -1, -1, 123);
                var o = new OpPlus(-1, -1, intLiteral);
                var value = o.GetValueInternal(expressionState);

                Assert.Equal(typeof(int), value.TypeDescriptor);
                Assert.Equal(typeof(int), value.TypeDescriptor);
                Assert.Equal(intLiteral.GetLiteralValue().Value, value.Value);
            }

            {
                var longLiteral = new LongLiteral("123", -1, -1, 123L);
                var o = new OpPlus(-1, -1, longLiteral);
                var value = o.GetValueInternal(expressionState);

                Assert.Equal(typeof(long), value.TypeDescriptor);
                Assert.Equal(typeof(long), value.TypeDescriptor);
                Assert.Equal(longLiteral.GetLiteralValue().Value, value.Value);
            }
        }

        [Fact]
        public void Test_BinaryPlusWithNumberOperands()
        {
            var expressionState = new ExpressionState(new StandardEvaluationContext());
            {
                var n1 = new RealLiteral("123.00", -1, -1, 123.0);
                var n2 = new RealLiteral("456.00", -1, -1, 456.0);
                var o = new OpPlus(-1, -1, n1, n2);
                var value = o.GetValueInternal(expressionState);

                Assert.Equal(typeof(double), value.TypeDescriptor);
                Assert.Equal(typeof(double), value.TypeDescriptor);
                Assert.Equal(123.0d + 456.0d, value.Value);
            }

            {
                var n1 = new LongLiteral("123", -1, -1, 123L);
                var n2 = new LongLiteral("456", -1, -1, 456L);
                var o = new OpPlus(-1, -1, n1, n2);
                var value = o.GetValueInternal(expressionState);
                Assert.Equal(typeof(long), value.TypeDescriptor);
                Assert.Equal(typeof(long), value.TypeDescriptor);
                Assert.Equal(123L + 456L, value.Value);
            }

            {
                var n1 = new IntLiteral("123", -1, -1, 123);
                var n2 = new IntLiteral("456", -1, -1, 456);
                var o = new OpPlus(-1, -1, n1, n2);
                var value = o.GetValueInternal(expressionState);
                Assert.Equal(typeof(int), value.TypeDescriptor);
                Assert.Equal(typeof(int), value.TypeDescriptor);
                Assert.Equal(123 + 456, value.Value);
            }
        }

        [Fact]
        public void Test_BinaryPlusWithStringOperands()
        {
            var expressionState = new ExpressionState(new StandardEvaluationContext());

            var n1 = new StringLiteral("\"foo\"", -1, -1, "\"foo\"");
            var n2 = new StringLiteral("\"bar\"", -1, -1, "\"bar\"");
            var o = new OpPlus(-1, -1, n1, n2);
            var value = o.GetValueInternal(expressionState);

            Assert.Equal(typeof(string), value.TypeDescriptor);
            Assert.Equal(typeof(string), value.TypeDescriptor);
            Assert.Equal("foobar", value.Value);
        }

        [Fact]
        public void Test_BinaryPlusWithLeftStringOperand()
        {
            var expressionState = new ExpressionState(new StandardEvaluationContext());

            var n1 = new StringLiteral("\"number is \"", -1, -1, "\"number is \"");
            var n2 = new LongLiteral("123", -1, -1, 123);
            var o = new OpPlus(-1, -1, n1, n2);
            var value = o.GetValueInternal(expressionState);
            Assert.Equal(typeof(string), value.TypeDescriptor);
            Assert.Equal(typeof(string), value.TypeDescriptor);
            Assert.Equal("number is 123", value.Value);
        }

        [Fact]
        public void Test_BinaryPlusWithRightStringOperand()
        {
            var expressionState = new ExpressionState(new StandardEvaluationContext());

            var n1 = new LongLiteral("123", -1, -1, 123);
            var n2 = new StringLiteral("\" is a number\"", -1, -1, "\" is a number\"");
            var o = new OpPlus(-1, -1, n1, n2);
            var value = o.GetValueInternal(expressionState);
            Assert.Equal(typeof(string), value.TypeDescriptor);
            Assert.Equal(typeof(string), value.TypeDescriptor);
            Assert.Equal("123 is a number", value.Value);
        }

        [Fact]
        public void Test_BinaryPlusWithTime_ToString()
        {
            var expressionState = new ExpressionState(new StandardEvaluationContext());
            var time = default(DateTime);

            var var = new VariableReference("timeVar", -1, -1);
            var.SetValue(expressionState, time);

            var n2 = new StringLiteral("\" is now\"", -1, -1, "\" is now\"");
            var o = new OpPlus(-1, -1, var, n2);
            var value = o.GetValueInternal(expressionState);
            Assert.Equal(typeof(string), value.TypeDescriptor);
            Assert.Equal(typeof(string), value.TypeDescriptor);
            Assert.Equal(time + " is now", value.Value);
        }
    }
}
