// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Text;
using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Spring;

public abstract class AbstractExpressionTests
{
    protected const bool ShouldBeWritable = true;
    private static readonly bool IsDebug = bool.Parse(bool.FalseString);
    protected static readonly bool ShouldNotBeWritable;
    protected readonly IExpressionParser Parser = new SpelExpressionParser();
    protected readonly StandardEvaluationContext Context = TestScenarioCreator.GetTestEvaluationContext();

    public virtual void EvaluateAndAskForReturnType(string expression, object expectedValue, Type expectedResultType)
    {
        IExpression expr = Parser.ParseExpression(expression);
        Assert.NotNull(expr);

        if (IsDebug)
        {
            SpelUtilities.PrintAbstractSyntaxTree(Console.Out, expr);
        }

        object value = expr.GetValue(Context, expectedResultType);

        if (value == null)
        {
            if (expectedValue == null)
            {
                return; // no point doing other checks
            }

            Assert.True(expectedValue == null, $"Expression returned null value, but expected '{expectedValue}'");
        }

        Type resultType = value.GetType();
        Assert.Equal(expectedResultType, resultType);
        Assert.Equal(expectedValue, value);
    }

    public virtual void Evaluate(string expression, object expectedValue, Type expectedResultType)
    {
        IExpression expr = Parser.ParseExpression(expression);
        Assert.NotNull(expr);

        if (IsDebug)
        {
            SpelUtilities.PrintAbstractSyntaxTree(Console.Out, expr);
        }

        object value = expr.GetValue(Context);

        // Check the return value
        if (value == null)
        {
            if (expectedValue == null)
            {
                return; // no point doing other checks
            }

            Assert.True(expectedValue == null, $"Expression returned null value, but expected '{expectedValue}'");
        }

        Type resultType = value.GetType();
        Assert.Equal(expectedResultType, resultType);
        Assert.Equal(expectedValue, expectedValue is string ? StringValueOf(value) : value);
    }

    public virtual void Evaluate(string expression, object expectedValue, Type expectedClassOfResult, bool shouldBeWritable)
    {
        IExpression expr = Parser.ParseExpression(expression);
        Assert.NotNull(expr);

        if (IsDebug)
        {
            SpelUtilities.PrintAbstractSyntaxTree(Console.Out, expr);
        }

        object value = expr.GetValue(Context);

        if (value == null)
        {
            if (expectedValue == null)
            {
                return; // no point doing other checks
            }

            Assert.True(expectedValue == null, $"Expression returned null value, but expected '{expectedValue}'");
        }

        Type resultType = value.GetType();
        Assert.Equal(expectedValue, expectedValue is string ? StringValueOf(value) : value);

        Assert.Equal(expectedClassOfResult, resultType);
        Assert.Equal(shouldBeWritable, expr.IsWritable(Context));
    }

    protected static void PrintDimension(StringBuilder sb, Array array, int[] indexes, int dimension)
    {
        bool isLeaf = dimension == array.Rank - 1;

        if (isLeaf)
        {
            int len = array.GetLength(dimension);

            for (int i = 0; i < len; i++)
            {
                indexes[dimension] = i;
                sb.Append('(').Append(string.Join(",", indexes)).Append(")=");
                object val = array.GetValue(indexes);
                sb.Append(val == null ? "null" : val.ToString());

                sb.Append(',');
            }
        }
        else
        {
            int dimLen = array.GetLength(dimension);

            for (int i = 0; i < dimLen; i++)
            {
                indexes[dimension] = i;
                PrintDimension(sb, array, indexes, dimension + 1);
            }
        }
    }

    protected static string PrintArray(Array array)
    {
        int[] indexes = new int[array.Rank];
        var sb = new StringBuilder();
        sb.Append(array.GetType().GetElementType().FullName);

        for (int i = 0; i < array.Rank; i++)
        {
            sb.Append('[').Append(array.GetLength(i)).Append(']');
        }

        sb.Append('{');
        PrintDimension(sb, array, indexes, 0);
        sb.Append('}');
        return sb.ToString();
    }

    protected static string StringValueOf(object value)
    {
        return StringValueOf(value, false);
    }

    protected static string StringValueOf(object value, bool isNested)
    {
        // do something nice for arrays
        if (value == null)
        {
            return "null";
        }

        Type valueType = value.GetType();

        if (valueType.IsArray)
        {
            string result = PrintArray(value as Array);
            return result;
        }

        if (value is IDictionary dictionary)
        {
            var sb = new StringBuilder("{");

            foreach (DictionaryEntry obj in dictionary)
            {
                sb.Append(StringValueOf(obj.Key));
                sb.Append('=');
                sb.Append(StringValueOf(obj.Value));
                sb.Append(',');
            }

            if (sb[sb.Length - 1] == ',')
            {
                return $"{sb.ToString(0, sb.Length - 1)}}}";
            }

            sb.Append('}');
            return sb.ToString();
        }

        if (value is IEnumerable enumerable && value is not string)
        {
            var sb = new StringBuilder("[");

            foreach (object obj in enumerable)
            {
                if (obj is IEnumerable && obj is not string)
                {
                    sb.Append(StringValueOf(obj));
                    sb.Append(',');
                }
                else
                {
                    sb.Append(obj);
                    sb.Append(',');
                }
            }

            if (sb[sb.Length - 1] == ',')
            {
                return $"{sb.ToString(0, sb.Length - 1)}]";
            }

            sb.Append(']');
            return sb.ToString();
        }

        return value.ToString();
    }

    protected virtual void EvaluateAndCheckError(string expression, SpelMessage expectedMessage, params object[] otherProperties)
    {
        EvaluateAndCheckError(expression, null, expectedMessage, otherProperties);
    }

    protected virtual void EvaluateAndCheckError(string expression, Type expectedReturnType, SpelMessage expectedMessage, params object[] otherProperties)
    {
        var ex = Assert.Throws<SpelEvaluationException>(() =>
        {
            IExpression expr = Parser.ParseExpression(expression);
            Assert.NotNull(expr);

            if (expectedReturnType != null)
            {
                expr.GetValue(Context, expectedReturnType);
            }
            else
            {
                expr.GetValue(Context);
            }
        });

        Assert.Equal(expectedMessage, ex.MessageCode);

        if (otherProperties != null && otherProperties.Length != 0)
        {
            // first one is expected position of the error within the string
            int pos = (int)otherProperties[0];
            Assert.Equal(pos, ex.Position);

            if (otherProperties.Length > 1)
            {
                // Check inserts match
                object[] inserts = ex.Inserts;
                Assert.True(inserts.Length >= otherProperties.Length - 1);
                object[] expectedInserts = new object[inserts.Length];
                Array.Copy(otherProperties, 1, expectedInserts, 0, expectedInserts.Length);
                Assert.Equal(expectedInserts.Length, inserts.Length);

                for (int i = 0; i < inserts.Length; i++)
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
            IExpression expr = Parser.ParseExpression(expression);

            if (IsDebug)
            {
                SpelUtilities.PrintAbstractSyntaxTree(Console.Out, expr);
            }
        });

        Assert.Equal(expectedMessage, ex.MessageCode);

        if (otherProperties != null && otherProperties.Length != 0)
        {
            // first one is expected position of the error within the string
            int pos = (int)otherProperties[0];
            Assert.Equal(pos, ex.Position);

            if (otherProperties.Length > 1)
            {
                // Check inserts match
                object[] inserts = ex.Inserts;
                Assert.True(inserts.Length >= otherProperties.Length - 1);
                object[] expectedInserts = new object[inserts.Length];
                Array.Copy(otherProperties, 1, expectedInserts, 0, expectedInserts.Length);
                Assert.Equal(expectedInserts.Length, inserts.Length);

                for (int i = 0; i < inserts.Length; i++)
                {
                    Assert.Equal(expectedInserts[i], inserts[i]);
                }
            }
        }
    }
}
