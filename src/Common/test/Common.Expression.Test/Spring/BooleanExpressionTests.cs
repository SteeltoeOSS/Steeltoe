// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Converter;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using System;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Spring
{
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
            EvaluateAndCheckError("1.0 or false", SpelMessage.TYPE_CONVERSION_ERROR, 0);
            EvaluateAndCheckError("false or 39.4", SpelMessage.TYPE_CONVERSION_ERROR, 9);
            EvaluateAndCheckError("true and 'hello'", SpelMessage.TYPE_CONVERSION_ERROR, 9);
            EvaluateAndCheckError(" 'hello' and 'goodbye'", SpelMessage.TYPE_CONVERSION_ERROR, 1);
            EvaluateAndCheckError("!35.2", SpelMessage.TYPE_CONVERSION_ERROR, 1);
            EvaluateAndCheckError("! 'foob'", SpelMessage.TYPE_CONVERSION_ERROR, 2);
        }

        [Fact]
        public void TestConvertAndHandleNull()
        {
            // SPR-9445
            // without null conversion
            EvaluateAndCheckError("null or true", SpelMessage.TYPE_CONVERSION_ERROR, 0, "null", "System.Boolean");
            EvaluateAndCheckError("null and true", SpelMessage.TYPE_CONVERSION_ERROR, 0, "null", "System.Boolean");
            EvaluateAndCheckError("!null", SpelMessage.TYPE_CONVERSION_ERROR, 1, "null", "System.Boolean");
            EvaluateAndCheckError("null ? 'foo' : 'bar'", SpelMessage.TYPE_CONVERSION_ERROR, 0, "null", "System.Boolean");

            context.TypeConverter = new StandardTypeConverter(new TestGenericConversionService());

            Evaluate("null or true", true, typeof(bool), false);
            Evaluate("null and true", false, typeof(bool), false);
            Evaluate("!null", true, typeof(bool), false);
            Evaluate("null ? 'foo' : 'bar'", "bar", typeof(string), false);
        }
    }

    public class TestGenericConversionService : IConversionService
    {
        public bool CanBypassConvert(Type sourceType, Type targetType)
        {
            return false;
        }

        public bool CanConvert(Type sourceType, Type targetType)
        {
            return true;
        }

        public T Convert<T>(object source)
        {
            return (T)Convert(source, source?.GetType(), typeof(T));
        }

        public object Convert(object source, Type sourceType, Type targetType)
        {
            if (source == null)
            {
                return targetType == typeof(bool) ? false : null;
            }

            return source;
        }
    }
}
