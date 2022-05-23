// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.TestResources;
using System;
using System.Collections;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Spring
{
    public class SetValueTests : AbstractExpressionTests
    {
        private static readonly bool DEBUG = false;

        [Fact]
        public void TestSetProperty()
        {
            SetValue("WonNobelPrize", true);
        }

        [Fact]
        public void TestSetNestedProperty()
        {
            SetValue("PlaceOfBirth.City", "Wien");
        }

        [Fact]
        public void TestSetArrayElementValue()
        {
            SetValue("Inventions[0]", "Just the telephone");
        }

        [Fact]
        public void TestErrorCase()
        {
            SetValueExpectError("3=4", null);
        }

        [Fact]
        public void TestSetElementOfNull()
        {
            SetValueExpectError("new Steeltoe.Common.Expression.Spring.TestResources.Inventor().Inventions[1]", SpelMessage.CANNOT_INDEX_INTO_NULL_VALUE);
        }

        [Fact]
        public void TestSetArrayElementValueAllPrimitiveTypes()
        {
            SetValue("ArrayContainer.Ints[1]", 3);
            SetValue("ArrayContainer.Floats[1]", 3.0f);
            SetValue("ArrayContainer.Booleans[1]", false);
            SetValue("ArrayContainer.Doubles[1]", 3.4d);
            SetValue("ArrayContainer.Shorts[1]", (short)3);
            SetValue("ArrayContainer.Longs[1]", 3L);
            SetValue("ArrayContainer.Bytes[1]", (byte)3);
            SetValue("ArrayContainer.Chars[1]", (char)3);
        }

        [Fact]
        public void TestIsWritableForInvalidExpressions_SPR10610()
        {
            var lContext = TestScenarioCreator.GetTestEvaluationContext();

            // PROPERTYORFIELDREFERENCE
            // Non existent field (or property):
            var e1 = _parser.ParseExpression("ArrayContainer.wibble");
            Assert.False(e1.IsWritable(lContext));

            var e2 = _parser.ParseExpression("ArrayContainer.wibble.foo");
            Assert.Throws<SpelEvaluationException>(() => e2.IsWritable(lContext));

            // org.springframework.expression.spel.SpelEvaluationException: EL1008E:(pos 15): Property or field 'wibble' cannot be found on object of type 'org.springframework.expression.spel.Testresources.ArrayContainer' - maybe not public?
            // at org.springframework.expression.spel.ast.PropertyOrFieldReference.readProperty(PropertyOrFieldReference.java:225)
            // VARIABLE
            // the variable does not exist (but that is OK, we should be writable)
            var e3 = _parser.ParseExpression("#madeup1");
            Assert.True(e3.IsWritable(lContext));

            var e4 = _parser.ParseExpression("#madeup2.bar"); // compound expression
            Assert.False(e4.IsWritable(lContext));

            // INDEXER
            // non existent indexer (wibble made up)
            var e5 = _parser.ParseExpression("ArrayContainer.wibble[99]");
            Assert.Throws<SpelEvaluationException>(() => e5.IsWritable(lContext));

            // non existent indexer (index via a string)
            var e6 = _parser.ParseExpression("ArrayContainer.ints['abc']");
            Assert.Throws<SpelEvaluationException>(() => e6.IsWritable(lContext));
        }

        [Fact]
        public void TestSetArrayElementValueAllPrimitiveTypesErrors()
        {
            // none of these sets are possible due to (expected) conversion problems
            SetValueExpectError("ArrayContainer.Ints[1]", "wibble");
            SetValueExpectError("ArrayContainer.Floats[1]", "dribble");
            SetValueExpectError("ArrayContainer.Booleans[1]", "nein");

            // TODO -- this fails with NPE due to ArrayToobject converter - discuss with Andy
            // SetValueExpectError("arrayContainer.doubles[1]", new ArrayList<string>());
            // SetValueExpectError("arrayContainer.shorts[1]", new ArrayList<string>());
            // SetValueExpectError("arrayContainer.longs[1]", new ArrayList<string>());
            SetValueExpectError("ArrayContainer.Bytes[1]", "NaB");
            SetValueExpectError("ArrayContainer.Chars[1]", "NaC");
        }

        [Fact]
        public void TestSetArrayElementNestedValue()
        {
            SetValue("PlacesLived[0].City", "Wien");
        }

        [Fact]
        public void TestSetListElementValue()
        {
            SetValue("PlacesLivedList[0]", new PlaceOfBirth("Wien"));
        }

        [Fact]
        public void TestSetGenericListElementValueTypeCoersion()
        {
            // TODO currently failing since SetValue does a GetValue and "Wien" string != PlaceOfBirth - check with andy
            SetValue("PlacesLivedList[0]", "Wien");
        }

        [Fact]
        public void TestSetGenericListElementValueTypeCoersionOK()
        {
            SetValue("BoolList[0]", "true", true);
        }

        [Fact]
        public void TestSetListElementNestedValue()
        {
            SetValue("PlacesLived[0].City", "Wien");
        }

        [Fact]
        public void TestSetArrayElementInvalidIndex()
        {
            SetValueExpectError("PlacesLived[23]", "Wien");
            SetValueExpectError("PlacesLivedList[23]", "Wien");
        }

        [Fact]
        public void TestSetMapElements()
        {
            SetValue("TestDictionary['montag']", "lundi");
        }

        [Fact]
        public void TestIndexingIntoUnsupportedType()
        {
            SetValueExpectError("'hello'[3]", 'p');
        }

        [Fact]
        public void TestSetPropertyTypeCoersion()
        {
            SetValue("PublicBoolean", "true", true);
        }

        [Fact]
        public void TestSetPropertyTypeCoersionThroughSetter()
        {
            SetValue("SomeProperty", "true", true);
        }

        [Fact]
        public void TestAssign()
        {
            var eContext = TestScenarioCreator.GetTestEvaluationContext();
            var e = Parse("PublicName='Andy'");
            Assert.False(e.IsWritable(eContext));
            Assert.Equal("Andy", e.GetValue(eContext));
        }

        /*
         * Testing the coercion of both the keys and the values to the correct type
         */
        [Fact]
        public void TestSetGenericMapElementRequiresCoercion()
        {
            var eContext = TestScenarioCreator.GetTestEvaluationContext();
            var e = Parse("MapOfstringToBoolean[42]");
            Assert.Null(e.GetValue(eContext));

            // Key should be coerced to string representation of 42
            e.SetValue(eContext, "true");

            // All keys should be strings
            var ks = Parse("MapOfstringToBoolean.Keys").GetValue<ICollection>(eContext);
            foreach (var key in ks)
            {
                Assert.IsType<string>(key);
            }

            // All values should be booleans
            var vs = Parse("MapOfstringToBoolean.Values").GetValue<ICollection>(eContext);
            foreach (var val in vs)
            {
                Assert.IsType<bool>(val);
            }

            // One final Test check coercion on the key for a map lookup
            var o = e.GetValue<bool>(eContext);
            Assert.True(o);
        }

        protected void SetValueExpectError(string expression, object value)
        {
            var e = _parser.ParseExpression(expression);
            Assert.NotNull(e);
            if (DEBUG)
            {
                SpelUtilities.PrintAbstractSyntaxTree(Console.Out, e);
            }

            var lContext = TestScenarioCreator.GetTestEvaluationContext();
            Assert.Throws<SpelEvaluationException>(() => e.SetValue(lContext, value));
        }

        protected void SetValue(string expression, object value)
        {
            try
            {
                var e = _parser.ParseExpression(expression);
                Assert.NotNull(e);
                if (DEBUG)
                {
                    SpelUtilities.PrintAbstractSyntaxTree(Console.Out, e);
                }

                var lContext = TestScenarioCreator.GetTestEvaluationContext();
                Assert.True(e.IsWritable(lContext));
                e.SetValue(lContext, value);
                Assert.Equal(value, e.GetValue(lContext, value.GetType()));
            }
            catch (EvaluationException ex)
            {
                throw new Exception($"Unexpected Exception: {ex.Message}", ex);
            }
            catch (ParseException ex)
            {
                throw new Exception($"Unexpected Exception: {ex.Message}", ex);
            }
        }

        protected void SetValue(string expression, object value, object expectedValue)
        {
            try
            {
                var e = _parser.ParseExpression(expression);
                Assert.NotNull(e);
                if (DEBUG)
                {
                    SpelUtilities.PrintAbstractSyntaxTree(Console.Out, e);
                }

                var lContext = TestScenarioCreator.GetTestEvaluationContext();
                Assert.True(e.IsWritable(lContext));
                e.SetValue(lContext, value);
                var a = expectedValue;
                var b = e.GetValue(lContext);
                Assert.Equal(b, a);
            }
            catch (EvaluationException ex)
            {
                throw new Exception($"Unexpected Exception: {ex.Message}", ex);
            }
            catch (ParseException ex)
            {
                throw new Exception($"Unexpected Exception: {ex.Message}", ex);
            }
        }

        private IExpression Parse(string expressionstring)
        {
            return _parser.ParseExpression(expressionstring);
        }
    }
}
