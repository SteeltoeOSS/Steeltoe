// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Spring.Standard;
using Steeltoe.Common.Expression.Spring.Support;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using Xunit;

namespace Steeltoe.Common.Expression.Spring
{
    public abstract class AbstractExpressionTests
    {
        protected static readonly bool SHOULD_BE_WRITABLE = true;
        protected static readonly bool SHOULD_NOT_BE_WRITABLE = false;
        protected readonly IExpressionParser parser = new SpelExpressionParser();
        protected readonly StandardEvaluationContext context = TestScenarioCreator.GetTestEvaluationContext();
        private static readonly bool DEBUG = false;

        public virtual void Evaluate(string expression, object expectedValue, Type expectedResultType)
        {
            var expr = parser.ParseExpression(expression);
            Assert.NotNull(expr);
            if (DEBUG)
            {
                SpelUtilities.PrintAbstractSyntaxTree(Console.Out, expr);
            }

            var value = expr.GetValue(context);

            // Check the return value
            if (value == null)
            {
                if (expectedValue == null)
                {
                    return;  // no point doing other checks
                }

                Assert.True(expectedValue == null, "Expression returned null value, but expected '" + expectedValue + "'");
            }

            var resultType = value.GetType();
            Assert.Equal(expectedResultType, resultType);

            if (expectedValue is string)
            {
                Assert.Equal(expectedValue, AbstractExpressionTests.StringValueOf(value));
            }
            else
            {
                Assert.Equal(expectedValue, value);
            }
        }

        public virtual void EvaluateAndAskForReturnType(string expression, object expectedValue, Type expectedResultType)
        {
            var expr = parser.ParseExpression(expression);
            Assert.NotNull(expr);
            if (DEBUG)
            {
                SpelUtilities.PrintAbstractSyntaxTree(Console.Out, expr);
            }

            var value = expr.GetValue(context, expectedResultType);
            if (value == null)
            {
                if (expectedValue == null)
                {
                    return;  // no point doing other checks
                }

                Assert.True(expectedValue == null, "Expression returned null value, but expected '" + expectedValue + "'");
            }

            var resultType = value.GetType();
            Assert.Equal(expectedResultType, resultType);
            Assert.Equal(expectedValue, value);
        }

        public virtual void Evaluate(string expression, object expectedValue, Type expectedClassOfResult, bool shouldBeWritable)
        {
            var expr = parser.ParseExpression(expression);
            Assert.NotNull(expr);
            if (DEBUG)
            {
                SpelUtilities.PrintAbstractSyntaxTree(Console.Out, expr);
            }

            var value = expr.GetValue(context);
            if (value == null)
            {
                if (expectedValue == null)
                {
                    return;  // no point doing other checks
                }

                Assert.True(expectedValue == null, "Expression returned null value, but expected '" + expectedValue + "'");
            }

            var resultType = value.GetType();
            if (expectedValue is string)
            {
                Assert.Equal(expectedValue, AbstractExpressionTests.StringValueOf(value));
            }
            else
            {
                Assert.Equal(expectedValue, value);
            }

            Assert.Equal(expectedClassOfResult, resultType);
            Assert.Equal(shouldBeWritable, expr.IsWritable(context));
        }

        protected static string StringValueOf(object value)
        {
            return StringValueOf(value, false);
        }

        protected static void PrintDimension(StringBuilder sb, Array array, int[] indexes, int dimension)
        {
            var isLeaf = dimension == array.Rank - 1;
            if (isLeaf)
            {
                var len = array.GetLength(dimension);
                for (var i = 0; i < len; i++)
                {
                    indexes[dimension] = i;
                    sb.Append("(").Append(string.Join(",", indexes)).Append(")=");
                    var val = array.GetValue(indexes);
                    if (val == null)
                    {
                        sb.Append("null");
                    }
                    else
                    {
                        sb.Append(val.ToString());
                    }

                    sb.Append(",");
                }
            }
            else
            {
                var dimLen = array.GetLength(dimension);
                for (var i = 0; i < dimLen; i++)
                {
                    indexes[dimension] = i;
                    PrintDimension(sb, array, indexes, dimension + 1);
                }
            }
        }

        protected static string PrintArray(Array array)
        {
            var indexes = new int[array.Rank];
            var sb = new StringBuilder();
            sb.Append(array.GetType().GetElementType().FullName);
            for (var i = 0; i < array.Rank; i++)
            {
                sb.Append("[").Append(array.GetLength(i)).Append("]");
            }

            sb.Append("{");
            PrintDimension(sb, array, indexes, 0);
            sb.Append("}");
            return sb.ToString();
        }

        protected static string StringValueOf(object value, bool isNested)
        {
            // do something nice for arrays
            if (value == null)
            {
                return "null";
            }

            var valueType = value.GetType();
            if (valueType.IsArray)
            {
                var result = PrintArray(value as Array);
                return result;
            }
            else if (value is IDictionary)
            {
                var asDict = value as IDictionary;
                var sb = new StringBuilder("{");
                foreach (DictionaryEntry obj in asDict)
                {
                    sb.Append(StringValueOf(obj.Key));
                    sb.Append("=");
                    sb.Append(StringValueOf(obj.Value));
                    sb.Append(",");
                }

                if (sb[sb.Length - 1] == ',')
                {
                    return sb.ToString(0, sb.Length - 1) + "}";
                }

                sb.Append("}");
                return sb.ToString();
            }
            else if (value is IEnumerable && !(value is string))
            {
                var enumerable = value as IEnumerable;
                var sb = new StringBuilder("[");
                foreach (var obj in enumerable)
                {
                    if (obj is IEnumerable && !(obj is string))
                    {
                        sb.Append(StringValueOf(obj));
                        sb.Append(",");
                    }
                    else
                    {
                        sb.Append(obj.ToString());
                        sb.Append(",");
                    }
                }

                if (sb[sb.Length - 1] == ',')
                {
                    return sb.ToString(0, sb.Length - 1) + "]";
                }

                sb.Append("]");
                return sb.ToString();
            }
            else
            {
                return value.ToString();
            }
        }

        protected virtual void EvaluateAndCheckError(string expression, SpelMessage expectedMessage, params object[] otherProperties)
        {
            EvaluateAndCheckError(expression, null, expectedMessage, otherProperties);
        }

        protected virtual void EvaluateAndCheckError(string expression, Type expectedReturnType, SpelMessage expectedMessage, params object[] otherProperties)
        {
            var ex = Assert.Throws<SpelEvaluationException>(() =>
            {
                var expr = parser.ParseExpression(expression);
                Assert.NotNull(expr);
                if (expectedReturnType != null)
                {
                    expr.GetValue(context, expectedReturnType);
                }
                else
                {
                    expr.GetValue(context);
                }
            });
            Assert.Equal(expectedMessage, ex.MessageCode);
            if (otherProperties != null && otherProperties.Length != 0)
            {
                // first one is expected position of the error within the string
                var pos = (int)otherProperties[0];
                Assert.Equal(pos, ex.Position);
                if (otherProperties.Length > 1)
                {
                    // Check inserts match
                    var inserts = ex.Inserts;
                    Assert.True(inserts.Length >= otherProperties.Length - 1);
                    var expectedInserts = new object[inserts.Length];
                    Array.Copy(otherProperties, 1, expectedInserts, 0, expectedInserts.Length);
                    Assert.Equal(expectedInserts.Length, inserts.Length);
                    for (var i = 0; i < inserts.Length; i++)
                    {
                        Assert.Equal(expectedInserts[i], inserts[i]);
                    }
                }
            }
        }

        protected virtual void ParseAndCheckError(string expression, SpelMessage expectedMessage, params object[] otherProperties)
        {
            var ex = Assert.Throws<SpelParseException>(() =>
            {
                var expr = parser.ParseExpression(expression);
                if (DEBUG)
                {
                    SpelUtilities.PrintAbstractSyntaxTree(Console.Out, expr);
                }
            });
            Assert.Equal(expectedMessage, ex.MessageCode);
            if (otherProperties != null && otherProperties.Length != 0)
            {
                // first one is expected position of the error within the string
                var pos = (int)otherProperties[0];
                Assert.Equal(pos, ex.Position);
                if (otherProperties.Length > 1)
                {
                    // Check inserts match
                    var inserts = ex.Inserts;
                    Assert.True(inserts.Length >= otherProperties.Length - 1);
                    var expectedInserts = new object[inserts.Length];
                    Array.Copy(otherProperties, 1, expectedInserts, 0, expectedInserts.Length);
                    Assert.Equal(expectedInserts.Length, inserts.Length);
                    for (var i = 0; i < inserts.Length; i++)
                    {
                        Assert.Equal(expectedInserts[i], inserts[i]);
                    }
                }
            }
        }
    }
}
