// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Spring
{
    public class VariableAndFunctionTests : AbstractExpressionTests
    {
        [Fact]
        public void TestVariableAccess01()
        {
            Evaluate("#answer", "42", typeof(int), SHOULD_BE_WRITABLE);
            Evaluate("#answer / 2", 21, typeof(int), SHOULD_NOT_BE_WRITABLE);
        }

        [Fact]
        public void TestVariableAccess_WellKnownVariables()
        {
            Evaluate("#this.Name", "Nikola Tesla", typeof(string));
            Evaluate("#root.Name", "Nikola Tesla", typeof(string));
        }

        [Fact]
        public void TestFunctionAccess01()
        {
            Evaluate("#ReverseInt(1,2,3)", "System.Int32[3]{(0)=3,(1)=2,(2)=1,}", typeof(int[]));
            Evaluate("#ReverseInt('1',2,3)", "System.Int32[3]{(0)=3,(1)=2,(2)=1,}", typeof(int[])); // requires type conversion of '1' to 1
            EvaluateAndCheckError("#ReverseInt(1)", SpelMessage.INCORRECT_NUMBER_OF_ARGUMENTS_TO_FUNCTION, 0, 1, 3);
        }

        [Fact]
        public void TestFunctionAccess02()
        {
            Evaluate("#ReverseString('hello')", "olleh", typeof(string));
            Evaluate("#ReverseString(37)", "73", typeof(string)); // requires type conversion of 37 to '37'
        }

        [Fact]
        public void TestCallVarargsFunction()
        {
            Evaluate("#VarargsFunctionReverseStringsAndMerge('a','b','c')", "cba", typeof(string));
            Evaluate("#VarargsFunctionReverseStringsAndMerge('a')", "a", typeof(string));
            Evaluate("#VarargsFunctionReverseStringsAndMerge()", string.Empty, typeof(string));
            Evaluate("#VarargsFunctionReverseStringsAndMerge('b',25)", "25b", typeof(string));
            Evaluate("#VarargsFunctionReverseStringsAndMerge(25)", "25", typeof(string));
            Evaluate("#VarargsFunctionReverseStringsAndMerge2(1,'a','b','c')", "1cba", typeof(string));
            Evaluate("#VarargsFunctionReverseStringsAndMerge2(2,'a')", "2a", typeof(string));
            Evaluate("#VarargsFunctionReverseStringsAndMerge2(3)", "3", typeof(string));
            Evaluate("#VarargsFunctionReverseStringsAndMerge2(4,'b',25)", "425b", typeof(string));
            Evaluate("#VarargsFunctionReverseStringsAndMerge2(5,25)", "525", typeof(string));
        }

        [Fact]
        public void TestCallingIllegalFunctions()
        {
            var parser = new SpelExpressionParser();
            var ctx = new StandardEvaluationContext();
            ctx.SetVariable("notStatic", GetType().GetMethod("NonStatic"));
            var ex = Assert.Throws<SpelEvaluationException>(() => parser.ParseRaw("#notStatic()").GetValue(ctx));
            Assert.Equal(SpelMessage.FUNCTION_MUST_BE_STATIC, ex.MessageCode);
        }

        // this method is used by the Test above
#pragma warning disable xUnit1013 // Public method should be marked as test
        public void NonStatic()
#pragma warning restore xUnit1013 // Public method should be marked as test
        {
        }
    }
}
