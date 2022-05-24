// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Common.Expression.Internal.Spring
{
    public class ParserErrorMessagesTests : AbstractExpressionTests
    {
        [Fact]
        public void TestBrokenExpression01()
        {
            // will not fit into an int, needs L suffix
            ParseAndCheckError("0xCAFEBABE", SpelMessage.NOT_AN_INTEGER);
            Evaluate("0xCAFEBABEL", 0xCAFEBABEL, typeof(long));
            ParseAndCheckError("0xCAFEBABECAFEBABEL", SpelMessage.NOT_A_LONG);
        }

        [Fact]
        public void TestBrokenExpression02()
        {
            // rogue 'G' on the end
            ParseAndCheckError("0xB0BG", SpelMessage.MORE_INPUT, 5, "G");
        }

        [Fact]
        public void TestBrokenExpression04()
        {
            // missing right operand
            ParseAndCheckError("true or ", SpelMessage.RIGHT_OPERAND_PROBLEM, 5);
        }

        [Fact]
        public void TestBrokenExpression05()
        {
            // missing right operand
            ParseAndCheckError("1 + ", SpelMessage.RIGHT_OPERAND_PROBLEM, 2);
        }

        [Fact]
        public void TestBrokenExpression07()
        {
            // T() can only take an identifier (possibly qualified), not a literal
            // message ought to say identifier rather than ID
            ParseAndCheckError("null instanceof T('a')", SpelMessage.NOT_EXPECTED_TOKEN, 18, "qualified ID", "literal_string");
        }
    }
}
