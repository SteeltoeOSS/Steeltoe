// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Spring.Standard;
using Xunit;

namespace Steeltoe.Common.Expression.Spring
{
    public class ParsingTests
    {
        private SpelExpressionParser parser = new SpelExpressionParser();

        // literals
        [Fact]
        public void TestLiteralBoolean01()
        {
            ParseCheck("False");
            ParseCheck("false");
            ParseCheck("FALSE");
            ParseCheck("FaLsE");
        }

        [Fact]
        public void TestLiteralLong01()
        {
            ParseCheck("37L", "37");
        }

        [Fact]
        public void TestLiteralBoolean02()
        {
            ParseCheck("True");
            ParseCheck("true");
            ParseCheck("TRUE");
        }

        [Fact]
        public void TestLiteralBoolean03()
        {
            ParseCheck("!true");
        }

        [Fact]
        public void TestLiteralInteger01()
        {
            ParseCheck("1");
        }

        [Fact]
        public void TestLiteralInteger02()
        {
            ParseCheck("1415");
        }

        [Fact]
        public void TestLiteralString01()
        {
            ParseCheck("'hello'");
        }

        [Fact]
        public void TestLiteralString02()
        {
            ParseCheck("'joe bloggs'");
        }

        [Fact]
        public void TestLiteralString03()
        {
            ParseCheck("'Tony''s Pizza'", "'Tony's Pizza'");
        }

        [Fact]
        public void TestLiteralReal01()
        {
            ParseCheck("6.0221415E+23");
        }

        [Fact]
        public void TestLiteralHex01()
        {
            ParseCheck("0x7FFFFFFF", "2147483647");
        }

        [Fact]
        public void TestLiteralDate01()
        {
            ParseCheck("date('1974/08/24')");
        }

        [Fact]
        public void TestLiteralDate02()
        {
            ParseCheck("date('19740824T131030','yyyyMMddTHHmmss')");
        }

        [Fact]
        public void TestLiteralNull01()
        {
            ParseCheck("null");
        }

        // boolean operators
        [Fact]
        public void TestBooleanOperatorsOr01()
        {
            ParseCheck("false or false", "(false or false)");
        }

        [Fact]
        public void TestBooleanOperatorsOr02()
        {
            ParseCheck("false or true", "(false or true)");
        }

        [Fact]
        public void TestBooleanOperatorsOr03()
        {
            ParseCheck("true or false", "(true or false)");
        }

        [Fact]
        public void TestBooleanOperatorsOr04()
        {
            ParseCheck("true or false", "(true or false)");
        }

        [Fact]
        public void TestBooleanOperatorsMix01()
        {
            ParseCheck("false or true and false", "(false or (true and false))");
        }

        // relational operators
        [Fact]
        public void TestRelOperatorsGT01()
        {
            ParseCheck("3>6", "(3 > 6)");
        }

        [Fact]
        public void TestRelOperatorsLT01()
        {
            ParseCheck("3<6", "(3 < 6)");
        }

        [Fact]
        public void TestRelOperatorsLE01()
        {
            ParseCheck("3<=6", "(3 <= 6)");
        }

        [Fact]
        public void TestRelOperatorsGE01()
        {
            ParseCheck("3>=6", "(3 >= 6)");
        }

        [Fact]
        public void TestRelOperatorsGE02()
        {
            ParseCheck("3>=3", "(3 >= 3)");
        }

        [Fact]
        public void TestElvis()
        {
            ParseCheck("3?:1", "3 ?: 1");
        }

        [Fact]
        public void TestRelOperatorsBetween01()
        {
            ParseCheck("1 between {1, 5}", "(1 between {1,5})");
        }

        [Fact]
        public void TestRelOperatorsBetween02()
        {
            ParseCheck("'efg' between {'abc', 'xyz'}", "('efg' between {'abc','xyz'})");
        }// true

        [Fact]
        public void TestRelOperatorsIs01()
        {
            ParseCheck("'xyz' instanceof int", "('xyz' instanceof int)");
        }// false

        [Fact]
        public void TestRelOperatorsIs02()
        {
            ParseCheck("{1, 2, 3, 4, 5} instanceof List", "({1,2,3,4,5} instanceof List)");
        }// true

        [Fact]
        public void TestRelOperatorsMatches01()
        {
            ParseCheck("'5.0067' matches '^-?\\d+(\\.\\d{2})?$'", "('5.0067' matches '^-?\\d+(\\.\\d{2})?$')");
        }// false

        [Fact]
        public void TestRelOperatorsMatches02()
        {
            ParseCheck("'5.00' matches '^-?\\d+(\\.\\d{2})?$'", "('5.00' matches '^-?\\d+(\\.\\d{2})?$')");
        }// true

        // mathematical operators
        [Fact]
        public void TestMathOperatorsAdd01()
        {
            ParseCheck("2+4", "(2 + 4)");
        }

        [Fact]
        public void TestMathOperatorsAdd02()
        {
            ParseCheck("'a'+'b'", "('a' + 'b')");
        }

        [Fact]
        public void TestMathOperatorsAdd03()
        {
            ParseCheck("'hello'+' '+'world'", "(('hello' + ' ') + 'world')");
        }

        [Fact]
        public void TestMathOperatorsSubtract01()
        {
            ParseCheck("5-4", "(5 - 4)");
        }

        [Fact]
        public void TestMathOperatorsMultiply01()
        {
            ParseCheck("7*4", "(7 * 4)");
        }

        [Fact]
        public void TestMathOperatorsDivide01()
        {
            ParseCheck("8/4", "(8 / 4)");
        }

        [Fact]
        public void TestMathOperatorModulus01()
        {
            ParseCheck("7 % 4", "(7 % 4)");
        }

        // mixed operators
        [Fact]
        public void TestMixedOperators01()
        {
            ParseCheck("true and 5>3", "(true and (5 > 3))");
        }

        // collection processors
        // [Fact]
        // public void TestCollectionProcessorsCount01()
        // {
        //    ParseCheck("new String[] {'abc','def','xyz'}.count()");
        // }
        // [Fact]
        // public void TestCollectionProcessorsCount02() {
        // ParseCheck("new int[] {1,2,3}.count()");
        // }
        // [Fact]
        // public void TestCollectionProcessorsMax01() {
        // ParseCheck("new int[] {1,2,3}.max()");
        // }
        // [Fact]
        // public void TestCollectionProcessorsMin01() {
        // ParseCheck("new int[] {1,2,3}.min()");
        // }
        // [Fact]
        // public void TestCollectionProcessorsAverage01() {
        // ParseCheck("new int[] {1,2,3}.average()");
        // }
        // [Fact]
        // public void TestCollectionProcessorsSort01() {
        // ParseCheck("new int[] {3,2,1}.sort()");
        // }
        // [Fact]
        // public void TestCollectionProcessorsNonNull01() {
        // ParseCheck("{'a','b',null,'d',null}.nonNull()");
        // }
        // [Fact]
        // public void TestCollectionProcessorsDistinct01() {
        // ParseCheck("{'a','b','a','d','e'}.distinct()");
        // }

        // references
        [Fact]
        public void TestReferences01()
        {
            ParseCheck("@foo");
            ParseCheck("@'foo.bar'");
            ParseCheck("@\"foo.bar.goo\"", "@'foo.bar.goo'");
        }

        [Fact]
        public void TestReferences03()
        {
            ParseCheck("@$$foo");
        }

        // properties
        [Fact]
        public void TestProperties01()
        {
            ParseCheck("name");
        }

        [Fact]
        public void TestProperties02()
        {
            ParseCheck("placeofbirth.CitY");
        }

        [Fact]
        public void TestProperties03()
        {
            ParseCheck("a.b.c.d.e");
        }

        // inline list creation
        [Fact]
        public void TestInlineListCreation01()
        {
            ParseCheck("{1, 2, 3, 4, 5}", "{1,2,3,4,5}");
        }

        [Fact]
        public void TestInlineListCreation02()
        {
            ParseCheck("{'abc','xyz'}", "{'abc','xyz'}");
        }

        // inline map creation
        [Fact]
        public void TestInlineMapCreation01()
        {
            ParseCheck("{'key1':'Value 1','today':DateTime.Today}");
        }

        [Fact]
        public void TestInlineMapCreation02()
        {
            ParseCheck("{1:'January',2:'February',3:'March'}");
        }

        // methods
        [Fact]
        public void TestMethods01()
        {
            ParseCheck("echo(12)");
        }

        [Fact]
        public void TestMethods02()
        {
            ParseCheck("echo(name)");
        }

        [Fact]
        public void TestMethods03()
        {
            ParseCheck("age.doubleItAndAdd(12)");
        }

        // constructors
        [Fact]
        public void TestConstructors01()
        {
            ParseCheck("new String('hello')");
        }

        // [Fact]
        // public void TestConstructors02()
        // {
        //    ParseCheck("new String[3]");
        // }

        // array construction
        // [Fact]
        // public void TestArrayConstruction01()
        // {
        //    ParseCheck("new int[] {1, 2, 3, 4, 5}", "new int[] {1,2,3,4,5}");
        // }

        // [Fact]
        // public void TestArrayConstruction02()
        // {
        //    ParseCheck("new String[] {'abc','xyz'}", "new String[] {'abc','xyz'}");
        // }

        // variables and functions
        [Fact]
        public void TestVariables01()
        {
            ParseCheck("#foo");
        }

        [Fact]
        public void TestFunctions01()
        {
            ParseCheck("#fn(1,2,3)");
        }

        [Fact]
        public void TestFunctions02()
        {
            ParseCheck("#fn('hello')");
        }

        // projections and selections
        // [Fact]
        // public void TestProjections01()
        // {
        //    ParseCheck("{1,2,3,4,5,6,7,8,9,10}.!{#isEven()}");
        // }

        // [Fact]
        // public void TestSelections01() {
        // ParseCheck("{1,2,3,4,5,6,7,8,9,10}.?{#isEven(#this) == 'y'}",
        // "{1,2,3,4,5,6,7,8,9,10}.?{(#isEven(#this) == 'y')}");
        // }

        // [Fact]
        // public void TestSelectionsFirst01() {
        // ParseCheck("{1,2,3,4,5,6,7,8,9,10}.^{#isEven(#this) == 'y'}",
        // "{1,2,3,4,5,6,7,8,9,10}.^{(#isEven(#this) == 'y')}");
        // }

        // [Fact]
        // public void TestSelectionsLast01() {
        // ParseCheck("{1,2,3,4,5,6,7,8,9,10}.${#isEven(#this) == 'y'}",
        // "{1,2,3,4,5,6,7,8,9,10}.${(#isEven(#this) == 'y')}");
        // }

        // assignment
        [Fact]
        public void TestAssignmentToVariables01()
        {
            ParseCheck("#var1='value1'");
        }

        // ternary operator
        [Fact]
        public void TestTernaryOperator01()
        {
            ParseCheck("1>2?3:4", "(1 > 2) ? 3 : 4");
        }

        [Fact]
        public void TestTernaryOperator02()
        {
            ParseCheck("{1}.#isEven(#this) == 'y'?'it is even':'it is odd'", "({1}.#isEven(#this) == 'y') ? 'it is even' : 'it is odd'");
        }

        [Fact]
        public void TestTypeReferences01()
        {
            ParseCheck("T(System.String)");
        }

        [Fact]
        public void TestTypeReferences02()
        {
            ParseCheck("T(String)");
        }

        [Fact]
        public void TestInlineList1()
        {
            ParseCheck("{1,2,3,4}");
        }

        private void ParseCheck(string expression)
        {
            ParseCheck(expression, expression);
        }

        private void ParseCheck(string expression, string expectedStringFormOfAST)
        {
            var e = parser.ParseRaw(expression) as SpelExpression;
            Assert.NotNull(e);
            Assert.Equal(expectedStringFormOfAST, e.ToStringAST(), true);
        }
    }
}
