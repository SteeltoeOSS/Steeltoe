// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using Steeltoe.Common.Expression.Internal.Spring.TestResources;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Spring
{
    public class EvaluationTests : AbstractExpressionTests
    {
        private static readonly bool DEBUG = false;

        [Fact]
        public void TestCreateListsOnAttemptToIndexNull01()
        {
            var parser = new SpelExpressionParser(new SpelParserOptions(true, true));
            var e = parser.ParseExpression("List[0]");
            var testClass = new TestClass();

            var o = e.GetValue(new StandardEvaluationContext(testClass));
            Assert.Equal(string.Empty, o);
            o = parser.ParseExpression("List[3]").GetValue(new StandardEvaluationContext(testClass));
            Assert.Equal(string.Empty, o);
            Assert.Equal(4, testClass.List.Count);
            Assert.Throws<SpelEvaluationException>(() => parser.ParseExpression("List2[3]").GetValue(new StandardEvaluationContext(testClass)));

            o = parser.ParseExpression("Foo[3]").GetValue(new StandardEvaluationContext(testClass));
            Assert.Equal(string.Empty, o);
            Assert.Equal(4, testClass.Foo.Count);

            o = parser.ParseExpression("FooIList[3]").GetValue(new StandardEvaluationContext(testClass));
            Assert.Equal(string.Empty, o);
            Assert.Equal(4, testClass.Foo.Count);
        }

        [Fact]
        public void TestCreateMapsOnAttemptToIndexNull01()
        {
            var testClass = new TestClass();
            var ctx = new StandardEvaluationContext(testClass);
            var parser = new SpelExpressionParser(new SpelParserOptions(true, true));

            var o = parser.ParseExpression("Map['a']").GetValue(ctx);
            Assert.Null(o);
            o = parser.ParseExpression("Map").GetValue(ctx);
            Assert.NotNull(o);

            // map2 should be null, there is no setter
            Assert.Throws<SpelEvaluationException>(() => parser.ParseExpression("Map2['a']").GetValue(ctx));
        }

        // wibble2 should be null (cannot be initialized dynamically), there is no setter
        [Fact]
        public void TestCreateObjectsOnAttemptToReferenceNull()
        {
            var testClass = new TestClass();
            var ctx = new StandardEvaluationContext(testClass);
            var parser = new SpelExpressionParser(new SpelParserOptions(true, true));

            var o = parser.ParseExpression("Wibble.Bar").GetValue(ctx);
            Assert.Equal("hello", o);
            o = parser.ParseExpression("Wibble").GetValue(ctx);
            Assert.NotNull(o);

            Assert.Throws<SpelEvaluationException>(() => parser.ParseExpression("Wibble2.Bar").GetValue(ctx));
        }

        [Fact]
        public void TestElvis01()
        {
            Evaluate("'Andy'?:'Dave'", "Andy", typeof(string));
            Evaluate("null?:'Dave'", "Dave", typeof(string));
        }

        [Fact]
        public void TestSafeNavigation()
        {
            Evaluate("null?.null?.null", null, null);
        }

        [Fact]
        public void TestRelOperatorGT01()
        {
            Evaluate("3 > 6", "False", typeof(bool));
        }

        [Fact]
        public void TestRelOperatorLT01()
        {
            Evaluate("3 < 6", "True", typeof(bool));
        }

        [Fact]
        public void TestRelOperatorLE01()
        {
            Evaluate("3 <= 6", "True", typeof(bool));
        }

        [Fact]
        public void TestRelOperatorGE01()
        {
            Evaluate("3 >= 6", "False", typeof(bool));
        }

        [Fact]
        public void TestRelOperatorGE02()
        {
            Evaluate("3 >= 3", "True", typeof(bool));
        }

        [Fact]
        public void TestRelOperatorsInstanceof01()
        {
            Evaluate("'xyz' instanceof T(int)", "False", typeof(bool));
        }

        [Fact]
        public void TestRelOperatorsInstanceof04()
        {
            Evaluate("null instanceof T(String)", "False", typeof(bool));
        }

        [Fact]
        public void TestRelOperatorsInstanceof05()
        {
            Evaluate("null instanceof T(System.Int32)", "False", typeof(bool));
        }

        [Fact]
        public void TestRelOperatorsInstanceof06()
        {
            EvaluateAndCheckError("'A' instanceof null", SpelMessage.INSTANCEOF_OPERATOR_NEEDS_CLASS_OPERAND, 15, "null");
        }

        [Fact]
        public void TestRelOperatorsMatches01()
        {
            Evaluate("'5.0067' matches '^-?\\d+(\\.\\d{2})?$'", "False", typeof(bool));
        }

        [Fact]
        public void TestRelOperatorsMatches02()
        {
            Evaluate("'5.00' matches '^-?\\d+(\\.\\d{2})?$'", "True", typeof(bool));
        }

        [Fact]
        public void TestRelOperatorsMatches03()
        {
            EvaluateAndCheckError("null matches '^.*$'", SpelMessage.INVALID_FIRST_OPERAND_FOR_MATCHES_OPERATOR, 0, null);
        }

        [Fact]
        public void TestRelOperatorsMatches04()
        {
            EvaluateAndCheckError("'abc' matches null", SpelMessage.INVALID_SECOND_OPERAND_FOR_MATCHES_OPERATOR, 14, null);
        }

        [Fact]
        public void TestRelOperatorsMatches05()
        {
            Evaluate("27 matches '^.*2.*$'", true, typeof(bool));  // conversion int>string
        }

        // SPR-16731
        [Fact]
        public void TestMatchesWithPatternAccessThreshold()
        {
            var pattern = "^(?=[a-z0-9-]{1,47})([a-z0-9]+[-]{0,1}){1,47}[a-z0-9]{1}$";
            var expression = $"'abcde-fghijklmn-o42pasdfasdfasdf.qrstuvwxyz10x.xx.yyy.zasdfasfd' matches '{pattern}'";
            var expr = _parser.ParseExpression(expression);
            var ex = Assert.Throws<SpelEvaluationException>(() => expr.GetValue());
            Assert.IsType<RegexMatchTimeoutException>(ex.InnerException);
            Assert.Equal(SpelMessage.FLAWED_PATTERN, ex.MessageCode);
        }

        // property access
        [Fact]
        public void TestPropertyField01()
        {
            Evaluate("Name", "Nikola Tesla", typeof(string), false);

            // not writable because (1) name is private (2) there is no setter, only a getter
            EvaluateAndCheckError("madeup", SpelMessage.PROPERTY_OR_FIELD_NOT_READABLE, 0, "madeup", "Steeltoe.Common.Expression.Internal.Spring.TestResources.Inventor");
        }

        [Fact]
        public void TestPropertyField02_SPR7100()
        {
            Evaluate("_name", "Nikola Tesla", typeof(string));
            Evaluate("_name_", "Nikola Tesla", typeof(string));
        }

        [Fact]
        public void TestRogueTrailingDotCausesNPE_SPR6866()
        {
            var ex = Assert.Throws<SpelParseException>(() => new SpelExpressionParser().ParseExpression("PlaceOfBirth.foo."));
            Assert.Equal(SpelMessage.OOD, ex.MessageCode);
            Assert.Equal(16, ex.Position);
        }

        // nested properties
        [Fact]
        public void TestPropertiesNested01()
        {
            Evaluate("PlaceOfBirth.City", "SmilJan", typeof(string), true);
        }

        [Fact]
        public void TestPropertiesNested02()
        {
            Evaluate("PlaceOfBirth.DoubleIt(12)", "24", typeof(int));
        }

        [Fact]
        public void TestPropertiesNested03()
        {
            var ex = Assert.Throws<SpelParseException>(() => new SpelExpressionParser().ParseRaw("PlaceOfBirth.23"));
            Assert.Equal(SpelMessage.UNEXPECTED_DATA_AFTER_DOT, ex.MessageCode);
            Assert.Equal("23", ex.Inserts[0]);
        }

        // methods
        [Fact]
        public void TestMethods01()
        {
            Evaluate("Echo(12)", "12", typeof(string));
        }

        [Fact]
        public void TestMethods02()
        {
            Evaluate("Echo(Name)", "Nikola Tesla", typeof(string));
        }

        // constructors
        [Fact]
        public void TestConstructorInvocation01()
        {
            Evaluate("new String('hello')", "hello", typeof(string));
        }

        [Fact]
        public void TestConstructorInvocation05()
        {
            Evaluate("new System.String('foobar')", "foobar", typeof(string));
        }

        [Fact]
        public void TestConstructorInvocation06()
        {
            // repeated evaluation to drive use of cached executor
            var e = (SpelExpression)_parser.ParseExpression("new String('wibble')");
            var newstring = e.GetValue<string>();
            Assert.Equal("wibble", newstring);
            newstring = e.GetValue<string>();
            Assert.Equal("wibble", newstring);

            // not writable
            Assert.False(e.IsWritable(new StandardEvaluationContext()));

            // ast
            Assert.Equal("new String('wibble')", e.ToStringAST());
        }

        // unary expressions
        [Fact]
        public void TestUnaryMinus01()
        {
            Evaluate("-5", "-5", typeof(int));
        }

        [Fact]
        public void TestUnaryPlus01()
        {
            Evaluate("+5", "5", typeof(int));
        }

        [Fact]
        public void TestUnaryNot01()
        {
            Evaluate("!true", "False", typeof(bool));
        }

        [Fact]
        public void TestUnaryNot02()
        {
            Evaluate("!false", "True", typeof(bool));
        }

        [Fact]
        public void TestUnaryNotWithNullValue()
        {
            Assert.Throws<SpelEvaluationException>(() => _parser.ParseExpression("!null").GetValue());
        }

        [Fact]
        public void TestAndWithNullValueOnLeft()
        {
            Assert.Throws<SpelEvaluationException>(() => _parser.ParseExpression("null and true").GetValue());
        }

        [Fact]
        public void TestAndWithNullValueOnRight()
        {
            Assert.Throws<SpelEvaluationException>(() => _parser.ParseExpression("true and null").GetValue());
        }

        [Fact]
        public void TestOrWithNullValueOnLeft()
        {
            Assert.Throws<SpelEvaluationException>(() => _parser.ParseExpression("null or false").GetValue());
        }

        [Fact]
        public void TestOrWithNullValueOnRight()
        {
            Assert.Throws<SpelEvaluationException>(() => _parser.ParseExpression("false or null").GetValue());
        }

        // assignment
        [Fact]
        public void TestAssignmentToVariables01()
        {
            Evaluate("#var1='value1'", "value1", typeof(string));
        }

        [Fact]
        public void TestTernaryOperator01()
        {
            Evaluate("2>4?1:2", 2, typeof(int));
        }

        [Fact]
        public void TestTernaryOperator02()
        {
            Evaluate("'abc'=='abc'?1:2", 1, typeof(int));
        }

        [Fact]
        public void TestTernaryOperator03()
        {
            // cannot convert string to boolean
            EvaluateAndCheckError("'hello'?1:2", SpelMessage.TYPE_CONVERSION_ERROR);
        }

        [Fact]
        public void TestTernaryOperator04()
        {
            var e = _parser.ParseExpression("1>2?3:4");
            Assert.False(e.IsWritable(_context));
        }

        [Fact]
        public void TestTernaryOperator05()
        {
            Evaluate("1>2?#var=4:#var=5", 5, typeof(int));
            Evaluate("3?:#var=5", 3, typeof(int));
            Evaluate("null?:#var=5", 5, typeof(int));
            Evaluate("2>4?(3>2?true:false):(5<3?true:false)", false, typeof(bool));
        }

        [Fact]
        public void TestTernaryOperatorWithNullValue()
        {
            Assert.Throws<SpelEvaluationException>(() => _parser.ParseExpression("null ? 0 : 1").GetValue());
        }

        [Fact]
        public void MethodCallWithRootReferenceThroughParameter()
        {
            Evaluate("PlaceOfBirth.DoubleIt(Inventions.Length)", 18, typeof(int));
        }

        [Fact]
        public void CtorCallWithRootReferenceThroughParameter()
        {
            Evaluate("new Steeltoe.Common.Expression.Internal.Spring.TestResources.PlaceOfBirth(Inventions[0].ToString()).City", "Telephone repeater", typeof(string));
        }

        [Fact]
        public void FnCallWithRootReferenceThroughParameter()
        {
            Evaluate("#ReverseInt(Inventions.Length, Inventions.Length, Inventions.Length)", "System.Int32[3]{(0)=9,(1)=9,(2)=9,}", typeof(int[]));
        }

        [Fact]
        public void MethodCallWithRootReferenceThroughParameterThatIsAFunctionCall()
        {
            Evaluate("PlaceOfBirth.DoubleIt(#ReverseInt(Inventions.Length,2,3)[2])", 18, typeof(int));
        }

        [Fact]
        public void TestIndexer03()
        {
            Evaluate("'christian'[8]", "n", typeof(string));
        }

        [Fact]
        public void TestIndexerError()
        {
            EvaluateAndCheckError("new Steeltoe.Common.Expression.Internal.Spring.TestResources.Inventor().Inventions[1]", SpelMessage.CANNOT_INDEX_INTO_NULL_VALUE);
        }

        [Fact]
        public void TestStaticRef02()
        {
            Evaluate("T(Steeltoe.Common.Expression.Internal.Spring.TestResources.Color).Green.RGB!=0", "True", typeof(bool));
        }

        // variables and functions
        [Fact]
        public void TestVariableAccess01()
        {
            Evaluate("#answer", "42", typeof(int), true);
        }

        [Fact]
        public void TestFunctionAccess01()
        {
            Evaluate("#ReverseInt(1,2,3)", "System.Int32[3]{(0)=3,(1)=2,(2)=1,}", typeof(int[]));
        }

        [Fact]
        public void TestFunctionAccess02()
        {
            Evaluate("#ReverseString('hello')", "olleh", typeof(string));
        }

        // type references
        [Fact]
        public void TestTypeReferences01()
        {
            var t = typeof(string);
            Evaluate("T(System.String)", "System.String", t.GetType());
        }

        [Fact]
        public void TestTypeReferencesAndQualifiedIdentifierCaching()
        {
            var e = (SpelExpression)_parser.ParseExpression("T(System.String)");
            Assert.False(e.IsWritable(new StandardEvaluationContext()));
            Assert.Equal("T(System.String)", e.ToStringAST());
            Assert.Equal(typeof(string), e.GetValue(typeof(Type)));

            // use cached QualifiedIdentifier:
            Assert.Equal("T(System.String)", e.ToStringAST());
            Assert.Equal(typeof(string), e.GetValue(typeof(Type)));
        }

        [Fact]
        public void OperatorVariants()
        {
            var e = (SpelExpression)_parser.ParseExpression("#a < #b");
            var ctx = new StandardEvaluationContext();
            ctx.SetVariable("a", (short)3);
            ctx.SetVariable("b", (short)6);
            Assert.True(e.GetValue<bool>(ctx));
            ctx.SetVariable("b", (byte)6);
            Assert.True(e.GetValue<bool>(ctx));
            ctx.SetVariable("a", (byte)9);
            ctx.SetVariable("b", (byte)6);
            Assert.False(e.GetValue<bool>(ctx));
            ctx.SetVariable("a", 10L);
            ctx.SetVariable("b", (short)30);
            Assert.True(e.GetValue<bool>(ctx));
            ctx.SetVariable("a", (byte)3);
            ctx.SetVariable("b", (short)30);
            Assert.True(e.GetValue<bool>(ctx));
            ctx.SetVariable("a", (byte)3);
            ctx.SetVariable("b", 30L);
            Assert.True(e.GetValue<bool>(ctx));
            ctx.SetVariable("a", (byte)3);
            ctx.SetVariable("b", 30f);
            Assert.True(e.GetValue<bool>(ctx));
            ctx.SetVariable("a", 10M);
            ctx.SetVariable("b", 20M);
            Assert.True(e.GetValue<bool>(ctx, typeof(bool)));
        }

        [Fact]
        public void TestTypeReferencesPrimitive()
        {
            Evaluate("T(int)", "System.Int32", typeof(int).GetType());
            Evaluate("T(byte)", "System.Byte", typeof(byte).GetType());
            Evaluate("T(char)", "System.Char", typeof(char).GetType());
            Evaluate("T(boolean)", "System.Boolean", typeof(bool).GetType());
            Evaluate("T(long)", "System.Int64", typeof(long).GetType());
            Evaluate("T(short)", "System.Int16", typeof(short).GetType());
            Evaluate("T(double)", "System.Double", typeof(double).GetType());
            Evaluate("T(float)", "System.Single", typeof(float).GetType());
        }

        [Fact]
        public void TestTypeReferences02()
        {
            var t = typeof(string);
            Evaluate("T(String)", "System.String", t.GetType());
        }

        [Fact]
        public void TestStringType()
        {
            EvaluateAndAskForReturnType("PlaceOfBirth.City", "SmilJan", typeof(string));
        }

        [Fact]
        public void TestNumbers01()
        {
            EvaluateAndAskForReturnType("3*4+5", 17, typeof(int));
            EvaluateAndAskForReturnType("3*4+5", 17L, typeof(long));
            EvaluateAndAskForReturnType("65", 'A', typeof(char));
            EvaluateAndAskForReturnType("3*4+5", (short)17, typeof(short));
            EvaluateAndAskForReturnType("3*4+5", "17", typeof(string));
        }

        [Fact]
        public void TestAdvancedNumerics()
        {
            var twentyFour = _parser.ParseExpression("2.0 * 3e0 * 4").GetValue(typeof(int));
            Assert.Equal(24, twentyFour);
            var one = _parser.ParseExpression("8.0 / 5e0 % 2").GetValue<double>();
            Assert.InRange((float)one, 1.6f, 1.6f);
            var o = _parser.ParseExpression("8.0 / 5e0 % 2").GetValue<int>();
            Assert.Equal(2, o);
            var sixteen = _parser.ParseExpression("-2 ^ 4").GetValue<int>();
            Assert.Equal(16, sixteen);
            var minusFortyFive = _parser.ParseExpression("1+2-3*8^2/2/2").GetValue<int>();
            Assert.Equal(-45, minusFortyFive);
        }

        [Fact]
        public void TestComparison()
        {
            var context = TestScenarioCreator.GetTestEvaluationContext();
            var trueValue = _parser.ParseExpression("T(DateTime) == BirthDate.GetType()").GetValue<bool>(context);
            Assert.True(trueValue);
        }

        [Fact]
        public void TestResolvingList()
        {
            var context = TestScenarioCreator.GetTestEvaluationContext();
            Assert.Throws<SpelEvaluationException>(() => _parser.ParseExpression("T(List)!=null").GetValue<bool>(context));
            ((StandardTypeLocator)context.TypeLocator).RegisterImport("System.Collections");
            Assert.True(_parser.ParseExpression("T(ArrayList)!=null").GetValue<bool>(context));
        }

        [Fact]
        public void TestResolvingString()
        {
            var stringClass = _parser.ParseExpression("T(String)").GetValue<Type>();
            Assert.Equal(typeof(string), stringClass);
        }

        [Fact]
        public void InitializingCollectionElementsOnWrite()
        {
            var person = new TestPerson();
            var context = new StandardEvaluationContext(person);
            var config = new SpelParserOptions(true, true);
            var parser = new SpelExpressionParser(config);
            var e = parser.ParseExpression("Name");
            e.SetValue(context, "Oleg");
            Assert.Equal("Oleg", person.Name);

            e = parser.ParseExpression("Address.Street");
            e.SetValue(context, "123 High St");
            Assert.Equal("123 High St", person.Address.Street);

            e = parser.ParseExpression("Address.CrossStreets[0]");
            e.SetValue(context, "Blah");
            Assert.Equal("Blah", person.Address.CrossStreets[0]);

            e = parser.ParseExpression("Address.CrossStreets[3]");
            e.SetValue(context, "Wibble");
            Assert.Equal("Blah", person.Address.CrossStreets[0]);
            Assert.Equal("Wibble", person.Address.CrossStreets[3]);
        }

        [Fact]
        public void CaseInsensitiveNullLiterals()
        {
            var parser = new SpelExpressionParser();

            var e = parser.ParseExpression("null");
            Assert.Null(e.GetValue());

            e = parser.ParseExpression("NULL");
            Assert.Null(e.GetValue());

            e = parser.ParseExpression("NuLl");
            Assert.Null(e.GetValue());
        }

        [Fact]
        public void TestCustomMethodFilter()
        {
            var context = new StandardEvaluationContext();

            // Register a custom MethodResolver...
            var customResolvers = new List<IMethodResolver>
            {
                new CustomMethodResolver()
            };
            context.MethodResolvers = customResolvers;

            // or simply...
            // context.setMethodResolvers(new ArrayList<MethodResolver>());

            // Register a custom MethodFilter...
            var filter = new CustomMethodFilter();
            var ex = Assert.Throws<InvalidOperationException>(() =>
                context.RegisterMethodFilter(typeof(string), filter));

            Assert.Contains(ex.Message, "Method filter cannot be set as the reflective method resolver is not in use");
        }

        [Fact]
        public void CollectionGrowingViaIndexer()
        {
            var instance = new Spr9751();

            // Add a new element to the list
            var ctx = new StandardEvaluationContext(instance);
            var parser = new SpelExpressionParser(new SpelParserOptions(true, true));
            var e = parser.ParseExpression("ListOfStrings[++Index3]='def'");
            e.GetValue(ctx);
            Assert.Equal(2, instance.ListOfStrings.Count);
            Assert.Equal("def", instance.ListOfStrings[1]);

            // Check reference beyond end of collection
            ctx = new StandardEvaluationContext(instance);
            parser = new SpelExpressionParser(new SpelParserOptions(true, true));
            e = parser.ParseExpression("ListOfStrings[0]");
            var value = e.GetValue<string>(ctx);
            Assert.Equal("abc", value);
            e = parser.ParseExpression("ListOfStrings[1]");
            value = e.GetValue<string>(ctx);
            Assert.Equal("def", value);
            e = parser.ParseExpression("ListOfStrings[2]");
            value = e.GetValue<string>(ctx);
            Assert.Equal(string.Empty, value);

            // Now turn off growing and reference off the end
            var failCtx = new StandardEvaluationContext(instance);
            parser = new SpelExpressionParser(new SpelParserOptions(false, false));
            var failExp = parser.ParseExpression("ListOfStrings[3]");
            var ex = Assert.Throws<SpelEvaluationException>(() => failExp.GetValue<string>(failCtx));
            Assert.Equal(SpelMessage.COLLECTION_INDEX_OUT_OF_BOUNDS, ex.MessageCode);
        }

        [Fact]
        public void LimitCollectionGrowing()
        {
            var instance = new TestClass();
            var ctx = new StandardEvaluationContext(instance);
            var parser = new SpelExpressionParser(new SpelParserOptions(true, true, 3));
            var e = parser.ParseExpression("Foo[2]");
            e.SetValue(ctx, "2");
            Assert.Equal(3, instance.Foo.Count);
            e = parser.ParseExpression("Foo[3]");
            try
            {
                e.SetValue(ctx, "3");
            }
            catch (SpelEvaluationException see)
            {
                Assert.Equal(SpelMessage.UNABLE_TO_GROW_COLLECTION, see.MessageCode);
                Assert.Equal(3, instance.Foo.Count);
            }
        }

        // For now I am making #this not assignable
        [Fact]
        public void Increment01Root()
        {
            var i = 42;
            var ctx = new StandardEvaluationContext(i);
            var parser = new SpelExpressionParser(new SpelParserOptions(true, true));
            var e = parser.ParseExpression("#this++");
            Assert.Equal(42, i);
            var ex = Assert.Throws<SpelEvaluationException>(() => e.GetValue<int>(ctx));
            Assert.Equal(SpelMessage.NOT_ASSIGNABLE, ex.MessageCode);
        }

        [Fact]
        public void Increment02Postfix()
        {
            var helper = new Spr9751();
            var ctx = new StandardEvaluationContext(helper);
            var parser = new SpelExpressionParser(new SpelParserOptions(true, true));
            IExpression e;

            // decimal
            e = parser.ParseExpression("Bd++");
            Assert.Equal(2M, helper.Bd);
            var return_bd = e.GetValue<decimal>(ctx);
            Assert.Equal(2M, return_bd);
            Assert.Equal(3M, helper.Bd);

            // double
            e = parser.ParseExpression("Ddd++");
            Assert.InRange((float)helper.Ddd, 2.0f, 2.0f);
            var return_ddd = e.GetValue<double>(ctx);
            Assert.InRange((float)return_ddd, 2.0f, 2.0f);
            Assert.InRange((float)helper.Ddd, 3.0f, 3.0f);

            // float
            e = parser.ParseExpression("Fff++");
            Assert.InRange(helper.Fff, 3.0f, 3.0f);
            var return_fff = e.GetValue<float>(ctx);
            Assert.InRange(return_fff, 3.0f, 3.0f);
            Assert.InRange(helper.Fff, 4.0f, 4.0f);

            // long
            e = parser.ParseExpression("Lll++");
            Assert.Equal(66666L, helper.Lll);
            var return_lll = e.GetValue<long>(ctx);
            Assert.Equal(66666L, return_lll);
            Assert.Equal(66667L, helper.Lll);

            // int
            e = parser.ParseExpression("Iii++");
            Assert.Equal(42, helper.Iii);
            var return_iii = e.GetValue<int>(ctx);
            Assert.Equal(42, return_iii);
            Assert.Equal(43, helper.Iii);
            return_iii = e.GetValue<int>(ctx);
            Assert.Equal(43, return_iii);
            Assert.Equal(44, helper.Iii);

            // short
            e = parser.ParseExpression("Sss++");
            Assert.Equal((short)15, helper.Sss);
            var return_sss = e.GetValue<short>(ctx);
            Assert.Equal((short)15, return_sss);
            Assert.Equal((short)16, helper.Sss);
        }

        [Fact]
        public void Increment02Prefix()
        {
            var helper = new Spr9751();
            var ctx = new StandardEvaluationContext(helper);
            var parser = new SpelExpressionParser(new SpelParserOptions(true, true));
            IExpression e;

            // decimal
            e = parser.ParseExpression("++Bd");
            Assert.Equal(2M, helper.Bd);
            var return_bd = e.GetValue<decimal>(ctx);
            Assert.Equal(3M, return_bd);
            Assert.Equal(3M, helper.Bd);

            // double
            e = parser.ParseExpression("++Ddd");
            Assert.InRange(helper.Ddd, 2.0d, 2.0d);
            var return_ddd = e.GetValue<double>(ctx);
            Assert.InRange(return_ddd, 3.0d, 3.0d);
            Assert.InRange(helper.Ddd, 3.0d, 3.0d);

            // float
            e = parser.ParseExpression("++Fff");
            Assert.InRange(helper.Fff, 3.0f, 3.0f);
            var return_fff = e.GetValue<float>(ctx);
            Assert.InRange(return_fff, 4.0f, 4.0f);
            Assert.InRange(helper.Fff, 4.0f, 4.0f);

            // long
            e = parser.ParseExpression("++Lll");
            Assert.Equal(66666L, helper.Lll);
            var return_lll = e.GetValue<long>(ctx);
            Assert.Equal(66667L, return_lll);
            Assert.Equal(66667L, helper.Lll);

            // int
            e = parser.ParseExpression("++Iii");
            Assert.Equal(42, helper.Iii);
            var return_iii = e.GetValue<int>(ctx);
            Assert.Equal(43, return_iii);
            Assert.Equal(43, helper.Iii);
            return_iii = e.GetValue<int>(ctx);
            Assert.Equal(44, return_iii);
            Assert.Equal(44, helper.Iii);

            // short
            e = parser.ParseExpression("++Sss");
            Assert.Equal((short)15, helper.Sss);
            var return_sss = e.GetValue<int>(ctx);
            Assert.Equal((short)16, return_sss);
            Assert.Equal((short)16, helper.Sss);
        }

        [Fact]
        public void Increment03()
        {
            var helper = new Spr9751();
            var ctx = new StandardEvaluationContext(helper);
            var parser = new SpelExpressionParser(new SpelParserOptions(true, true));

            var e1 = parser.ParseExpression("M()++");
            var ex = Assert.Throws<SpelEvaluationException>(() => e1.GetValue<decimal>(ctx));
            Assert.Equal(SpelMessage.OPERAND_NOT_INCREMENTABLE, ex.MessageCode);

            var e2 = parser.ParseExpression("++M()");
            ex = Assert.Throws<SpelEvaluationException>(() => e2.GetValue<decimal>(ctx));
            Assert.Equal(SpelMessage.OPERAND_NOT_INCREMENTABLE, ex.MessageCode);
        }

        [Fact]
        public void Increment04()
        {
            var i = 42;
            var ctx = new StandardEvaluationContext(i);
            var parser = new SpelExpressionParser(new SpelParserOptions(true, true));
            var e1 = parser.ParseExpression("++1");
            var ex = Assert.Throws<SpelEvaluationException>(() => e1.GetValue<double>(ctx));
            Assert.Equal(SpelMessage.NOT_ASSIGNABLE, ex.MessageCode);
            var e2 = parser.ParseExpression("1++");
            ex = Assert.Throws<SpelEvaluationException>(() => e2.GetValue<double>(ctx));
            Assert.Equal(SpelMessage.NOT_ASSIGNABLE, ex.MessageCode);
        }

        [Fact]
        public void Decrement01Root()
        {
            var i = 42;
            var ctx = new StandardEvaluationContext(i);
            var parser = new SpelExpressionParser(new SpelParserOptions(true, true));
            var e = parser.ParseExpression("#this--");
            Assert.Equal(42, i);
            var ex = Assert.Throws<SpelEvaluationException>(() => e.GetValue<int>(ctx));
            Assert.Equal(SpelMessage.NOT_ASSIGNABLE, ex.MessageCode);
        }

        [Fact]
        public void Decrement02Postfix()
        {
            var helper = new Spr9751();
            var ctx = new StandardEvaluationContext(helper);
            var parser = new SpelExpressionParser(new SpelParserOptions(true, true));
            IExpression e;

            // BigDecimal
            e = parser.ParseExpression("Bd--");
            Assert.Equal(2M, helper.Bd);
            var return_bd = e.GetValue<decimal>(ctx);
            Assert.Equal(2M, return_bd);
            Assert.Equal(1M, helper.Bd);

            // double
            e = parser.ParseExpression("Ddd--");
            Assert.InRange((float)helper.Ddd, 2.0d, 2.0d);
            var return_ddd = e.GetValue<double>(ctx);
            Assert.InRange(return_ddd, 2.0d, 2.0d);
            Assert.InRange(helper.Ddd, 1.0d, 1.0d);

            // float
            e = parser.ParseExpression("Fff--");
            Assert.InRange(helper.Fff, 3.0f, 3.0f);
            var return_fff = e.GetValue<float>(ctx);
            Assert.InRange(return_fff, 3.0f, 3.0f);
            Assert.InRange(helper.Fff, 2.0f, 2.0f);

            // long
            e = parser.ParseExpression("Lll--");
            Assert.Equal(66666L, helper.Lll);
            var return_lll = e.GetValue<long>(ctx);
            Assert.Equal(66666L, return_lll);
            Assert.Equal(66665L, helper.Lll);

            // int
            e = parser.ParseExpression("Iii--");
            Assert.Equal(42, helper.Iii);
            var return_iii = e.GetValue<int>(ctx);
            Assert.Equal(42, return_iii);
            Assert.Equal(41, helper.Iii);
            return_iii = e.GetValue<int>(ctx);
            Assert.Equal(41, return_iii);
            Assert.Equal(40, helper.Iii);

            // short
            e = parser.ParseExpression("Sss--");
            Assert.Equal((short)15, helper.Sss);
            var return_sss = e.GetValue<short>(ctx);
            Assert.Equal((short)15, return_sss);
            Assert.Equal((short)14, helper.Sss);
        }

        [Fact]
        public void Decrement02Prefix()
        {
            var helper = new Spr9751();
            var ctx = new StandardEvaluationContext(helper);
            var parser = new SpelExpressionParser(new SpelParserOptions(true, true));
            IExpression e;

            // BigDecimal
            e = parser.ParseExpression("--Bd");
            Assert.Equal(2M, helper.Bd);
            var return_bd = e.GetValue<decimal>(ctx);
            Assert.Equal(1M, return_bd);
            Assert.Equal(1M, helper.Bd);

            // double
            e = parser.ParseExpression("--Ddd");
            Assert.InRange((float)helper.Ddd, 2.0d, 2.0d);
            var return_ddd = e.GetValue<double>(ctx);
            Assert.InRange(return_ddd, 1.0d, 1.0d);
            Assert.InRange(helper.Ddd, 1.0d, 1.0d);

            // float
            e = parser.ParseExpression("--Fff");
            Assert.InRange(helper.Fff, 3.0f, 3.0f);
            var return_fff = e.GetValue<float>(ctx);
            Assert.InRange(return_fff, 2.0f, 2.0f);
            Assert.InRange(helper.Fff, 2.0f, 2.0f);

            // long
            e = parser.ParseExpression("--Lll");
            Assert.Equal(66666L, helper.Lll);
            var return_lll = e.GetValue<long>(ctx);
            Assert.Equal(66665L, return_lll);
            Assert.Equal(66665L, helper.Lll);

            // int
            e = parser.ParseExpression("--Iii");
            Assert.Equal(42, helper.Iii);
            var return_iii = e.GetValue<int>(ctx);
            Assert.Equal(41, return_iii);
            Assert.Equal(41, helper.Iii);
            return_iii = e.GetValue<int>(ctx);
            Assert.Equal(40, return_iii);
            Assert.Equal(40, helper.Iii);

            // short
            e = parser.ParseExpression("--Sss");
            Assert.Equal((short)15, helper.Sss);
            var return_sss = e.GetValue<int>(ctx);
            Assert.Equal(14, return_sss);
            Assert.Equal(14, helper.Sss);
        }

        [Fact]
        public void Decrement03()
        {
            var helper = new Spr9751();
            var ctx = new StandardEvaluationContext(helper);
            var parser = new SpelExpressionParser(new SpelParserOptions(true, true));

            var e1 = parser.ParseExpression("M()--");
            var ex = Assert.Throws<SpelEvaluationException>(() => e1.GetValue<double>(ctx));
            Assert.Equal(SpelMessage.OPERAND_NOT_DECREMENTABLE, ex.MessageCode);

            var e2 = parser.ParseExpression("--M()");
            ex = Assert.Throws<SpelEvaluationException>(() => e2.GetValue<double>(ctx));
            Assert.Equal(SpelMessage.OPERAND_NOT_DECREMENTABLE, ex.MessageCode);
        }

        [Fact]
        public void Decrement04()
        {
            var i = 42;
            var ctx = new StandardEvaluationContext(i);
            var parser = new SpelExpressionParser(new SpelParserOptions(true, true));
            var e1 = parser.ParseExpression("--1");
            var ex = Assert.Throws<SpelEvaluationException>(() => e1.GetValue<int>(ctx));
            Assert.Equal(SpelMessage.NOT_ASSIGNABLE, ex.MessageCode);

            var e2 = parser.ParseExpression("1--");
            ex = Assert.Throws<SpelEvaluationException>(() => e1.GetValue<int>(ctx));
            Assert.Equal(SpelMessage.NOT_ASSIGNABLE, ex.MessageCode);
        }

        [Fact]
        public void IncDecTogether()
        {
            var helper = new Spr9751();
            var ctx = new StandardEvaluationContext(helper);
            var parser = new SpelExpressionParser(new SpelParserOptions(true, true));
            IExpression e;

            // index1 is 2 at the start - the 'intArray[#root.index1++]' should not be evaluated twice!
            // intArray[2] is 3
            e = parser.ParseExpression("IntArray[#root.Index1++]++");
            e.GetValue<int>(ctx);
            Assert.Equal(3, helper.Index1);
            Assert.Equal(4, helper.IntArray[2]);

            // index1 is 3 intArray[3] is 4
            e = parser.ParseExpression("IntArray[#root.Index1++]--");
            Assert.Equal(4, e.GetValue<int>(ctx));
            Assert.Equal(4, helper.Index1);
            Assert.Equal(3, helper.IntArray[3]);

            // index1 is 4, intArray[3] is 3
            e = parser.ParseExpression("IntArray[--#root.Index1]++");
            Assert.Equal(3, e.GetValue<int>(ctx));
            Assert.Equal(3, helper.Index1);
            Assert.Equal(4, helper.IntArray[3]);
        }

        [Fact]
        public void IncrementAllNodeTypes()
        {
            var helper = new Spr9751();
            var ctx = new StandardEvaluationContext(helper);
            var parser = new SpelExpressionParser(new SpelParserOptions(true, true));
            IExpression e;

            // BooleanLiteral
            ExpectFailNotAssignable(parser, ctx, "true++");
            ExpectFailNotAssignable(parser, ctx, "--false");
            ExpectFailSetValueNotSupported(parser, ctx, "true=false");

            // IntLiteral
            ExpectFailNotAssignable(parser, ctx, "12++");
            ExpectFailNotAssignable(parser, ctx, "--1222");
            ExpectFailSetValueNotSupported(parser, ctx, "12=16");

            // LongLiteral
            ExpectFailNotAssignable(parser, ctx, "1.0d++");
            ExpectFailNotAssignable(parser, ctx, "--3.4d");
            ExpectFailSetValueNotSupported(parser, ctx, "1.0d=3.2d");

            // NullLiteral
            ExpectFailNotAssignable(parser, ctx, "null++");
            ExpectFailNotAssignable(parser, ctx, "--null");
            ExpectFailSetValueNotSupported(parser, ctx, "null=null");
            ExpectFailSetValueNotSupported(parser, ctx, "null=123");

            // OpAnd
            ExpectFailNotAssignable(parser, ctx, "(true && false)++");
            ExpectFailNotAssignable(parser, ctx, "--(false AND true)");
            ExpectFailSetValueNotSupported(parser, ctx, "(true && false)=(false && true)");

            // OpDivide
            ExpectFailNotAssignable(parser, ctx, "(3/4)++");
            ExpectFailNotAssignable(parser, ctx, "--(2/5)");
            ExpectFailSetValueNotSupported(parser, ctx, "(1/2)=(3/4)");

            // OpEq
            ExpectFailNotAssignable(parser, ctx, "(3==4)++");
            ExpectFailNotAssignable(parser, ctx, "--(2==5)");
            ExpectFailSetValueNotSupported(parser, ctx, "(1==2)=(3==4)");

            // OpGE
            ExpectFailNotAssignable(parser, ctx, "(3>=4)++");
            ExpectFailNotAssignable(parser, ctx, "--(2>=5)");
            ExpectFailSetValueNotSupported(parser, ctx, "(1>=2)=(3>=4)");

            // OpGT
            ExpectFailNotAssignable(parser, ctx, "(3>4)++");
            ExpectFailNotAssignable(parser, ctx, "--(2>5)");
            ExpectFailSetValueNotSupported(parser, ctx, "(1>2)=(3>4)");

            // OpLE
            ExpectFailNotAssignable(parser, ctx, "(3<=4)++");
            ExpectFailNotAssignable(parser, ctx, "--(2<=5)");
            ExpectFailSetValueNotSupported(parser, ctx, "(1<=2)=(3<=4)");

            // OpLT
            ExpectFailNotAssignable(parser, ctx, "(3<4)++");
            ExpectFailNotAssignable(parser, ctx, "--(2<5)");
            ExpectFailSetValueNotSupported(parser, ctx, "(1<2)=(3<4)");

            // OpMinus
            ExpectFailNotAssignable(parser, ctx, "(3-4)++");
            ExpectFailNotAssignable(parser, ctx, "--(2-5)");
            ExpectFailSetValueNotSupported(parser, ctx, "(1-2)=(3-4)");

            // OpModulus
            ExpectFailNotAssignable(parser, ctx, "(3%4)++");
            ExpectFailNotAssignable(parser, ctx, "--(2%5)");
            ExpectFailSetValueNotSupported(parser, ctx, "(1%2)=(3%4)");

            // OpMultiply
            ExpectFailNotAssignable(parser, ctx, "(3*4)++");
            ExpectFailNotAssignable(parser, ctx, "--(2*5)");
            ExpectFailSetValueNotSupported(parser, ctx, "(1*2)=(3*4)");

            // OpNE
            ExpectFailNotAssignable(parser, ctx, "(3!=4)++");
            ExpectFailNotAssignable(parser, ctx, "--(2!=5)");
            ExpectFailSetValueNotSupported(parser, ctx, "(1!=2)=(3!=4)");

            // OpOr
            ExpectFailNotAssignable(parser, ctx, "(true || false)++");
            ExpectFailNotAssignable(parser, ctx, "--(false OR true)");
            ExpectFailSetValueNotSupported(parser, ctx, "(true || false)=(false OR true)");

            // OpPlus
            ExpectFailNotAssignable(parser, ctx, "(3+4)++");
            ExpectFailNotAssignable(parser, ctx, "--(2+5)");
            ExpectFailSetValueNotSupported(parser, ctx, "(1+2)=(3+4)");

            // RealLiteral
            ExpectFailNotAssignable(parser, ctx, "1.0d++");
            ExpectFailNotAssignable(parser, ctx, "--2.0d");
            ExpectFailSetValueNotSupported(parser, ctx, "(1.0d)=(3.0d)");
            ExpectFailNotAssignable(parser, ctx, "1.0f++");
            ExpectFailNotAssignable(parser, ctx, "--2.0f");
            ExpectFailSetValueNotSupported(parser, ctx, "(1.0f)=(3.0f)");

            // stringLiteral
            ExpectFailNotAssignable(parser, ctx, "'abc'++");
            ExpectFailNotAssignable(parser, ctx, "--'def'");
            ExpectFailSetValueNotSupported(parser, ctx, "'abc'='def'");

            // Ternary
            ExpectFailNotAssignable(parser, ctx, "(true?true:false)++");
            ExpectFailNotAssignable(parser, ctx, "--(true?true:false)");
            ExpectFailSetValueNotSupported(parser, ctx, "(true?true:false)=(true?true:false)");

            // TypeReference
            ExpectFailNotAssignable(parser, ctx, "T(String)++");
            ExpectFailNotAssignable(parser, ctx, "--T(Int32)");
            ExpectFailSetValueNotSupported(parser, ctx, "T(String)=T(Int32)");

            // OperatorBetween
            ExpectFailNotAssignable(parser, ctx, "(3 between {1,5})++");
            ExpectFailNotAssignable(parser, ctx, "--(3 between {1,5})");
            ExpectFailSetValueNotSupported(parser, ctx, "(3 between {1,5})=(3 between {1,5})");

            // OperatorInstanceOf
            ExpectFailNotAssignable(parser, ctx, "(Type instanceof T(String))++");
            ExpectFailNotAssignable(parser, ctx, "--(Type instanceof T(String))");
            ExpectFailSetValueNotSupported(parser, ctx, "(Type instanceof T(String))=(Type instanceof T(String))");

            // Elvis
            ExpectFailNotAssignable(parser, ctx, "(true?:false)++");
            ExpectFailNotAssignable(parser, ctx, "--(true?:false)");
            ExpectFailSetValueNotSupported(parser, ctx, "(true?:false)=(true?:false)");

            // OpInc
            ExpectFailNotAssignable(parser, ctx, "(Iii++)++");
            ExpectFailNotAssignable(parser, ctx, "--(++Iii)");
            ExpectFailSetValueNotSupported(parser, ctx, "(Iii++)=(++Iii)");

            // OpDec
            ExpectFailNotAssignable(parser, ctx, "(Iii--)++");
            ExpectFailNotAssignable(parser, ctx, "--(--Iii)");
            ExpectFailSetValueNotSupported(parser, ctx, "(Iii--)=(--Iii)");

            // OperatorNot
            ExpectFailNotAssignable(parser, ctx, "(!true)++");
            ExpectFailNotAssignable(parser, ctx, "--(!false)");
            ExpectFailSetValueNotSupported(parser, ctx, "(!true)=(!false)");

            // OperatorPower
            ExpectFailNotAssignable(parser, ctx, "(Iii^2)++");
            ExpectFailNotAssignable(parser, ctx, "--(Iii^2)");
            ExpectFailSetValueNotSupported(parser, ctx, "(Iii^2)=(Iii^3)");

            // Assign
            // iii=42
            e = parser.ParseExpression("Iii=Iii++");
            Assert.Equal(42, helper.Iii);
            var return_iii = e.GetValue<int>(ctx);
            Assert.Equal(42, helper.Iii);
            Assert.Equal(42, return_iii);

            // Identifier
            e = parser.ParseExpression("Iii++");
            Assert.Equal(42, helper.Iii);
            return_iii = e.GetValue<int>(ctx);
            Assert.Equal(42, return_iii);
            Assert.Equal(43, helper.Iii);

            e = parser.ParseExpression("--Iii");
            Assert.Equal(43, helper.Iii);
            return_iii = e.GetValue<int>(ctx);
            Assert.Equal(42, return_iii);
            Assert.Equal(42, helper.Iii);

            e = parser.ParseExpression("Iii=99");
            Assert.Equal(42, helper.Iii);
            return_iii = e.GetValue<int>(ctx);
            Assert.Equal(99, return_iii);
            Assert.Equal(99, helper.Iii);

            // CompoundExpression
            // foo.iii == 99
            e = parser.ParseExpression("Foo.Iii++");
            Assert.Equal(99, helper.Foo.Iii);
            var return_foo_iii = e.GetValue<int>(ctx);
            Assert.Equal(99, return_foo_iii);
            Assert.Equal(100, helper.Foo.Iii);

            e = parser.ParseExpression("--Foo.Iii");
            Assert.Equal(100, helper.Foo.Iii);
            return_foo_iii = e.GetValue<int>(ctx);
            Assert.Equal(99, return_foo_iii);
            Assert.Equal(99, helper.Foo.Iii);

            e = parser.ParseExpression("Foo.Iii=999");
            Assert.Equal(99, helper.Foo.Iii);
            return_foo_iii = e.GetValue<int>(ctx);
            Assert.Equal(999, return_foo_iii);
            Assert.Equal(999, helper.Foo.Iii);

            // ConstructorReference
            ExpectFailNotAssignable(parser, ctx, "(new String('abc'))++");
            ExpectFailNotAssignable(parser, ctx, "--(new String('abc'))");
            ExpectFailSetValueNotSupported(parser, ctx, "(new String('abc'))=(new String('abc'))");

            // MethodReference
            ExpectFailNotIncrementable(parser, ctx, "M()++");
            ExpectFailNotDecrementable(parser, ctx, "--M()");
            ExpectFailSetValueNotSupported(parser, ctx, "M()=M()");

            // OperatorMatches
            ExpectFailNotAssignable(parser, ctx, "('abc' matches '^a..')++");
            ExpectFailNotAssignable(parser, ctx, "--('abc' matches '^a..')");
            ExpectFailSetValueNotSupported(parser, ctx, "('abc' matches '^a..')=('abc' matches '^a..')");

            // Selection
            ctx.RegisterFunction("IsEven", typeof(Spr9751).GetMethod("IsEven", new Type[] { typeof(int) }));

            ExpectFailNotIncrementable(parser, ctx, "({1,2,3}.?[#IsEven(#this)])++");
            ExpectFailNotDecrementable(parser, ctx, "--({1,2,3}.?[#IsEven(#this)])");
            ExpectFailNotAssignable(parser, ctx, "({1,2,3}.?[#IsEven(#this)])=({1,2,3}.?[#IsEven(#this)])");

            // slightly diff here because return value isn't a list, it is a single entity
            ExpectFailNotAssignable(parser, ctx, "({1,2,3}.^[#IsEven(#this)])++");
            ExpectFailNotAssignable(parser, ctx, "--({1,2,3}.^[#IsEven(#this)])");
            ExpectFailNotAssignable(parser, ctx, "({1,2,3}.^[#IsEven(#this)])=({1,2,3}.^[#IsEven(#this)])");

            ExpectFailNotAssignable(parser, ctx, "({1,2,3}.$[#IsEven(#this)])++");
            ExpectFailNotAssignable(parser, ctx, "--({1,2,3}.$[#IsEven(#this)])");
            ExpectFailNotAssignable(parser, ctx, "({1,2,3}.$[#IsEven(#this)])=({1,2,3}.$[#IsEven(#this)])");

            // FunctionReference
            ExpectFailNotAssignable(parser, ctx, "#IsEven(3)++");
            ExpectFailNotAssignable(parser, ctx, "--#IsEven(4)");
            ExpectFailSetValueNotSupported(parser, ctx, "#IsEven(3)=#IsEven(5)");

            // VariableReference
            ctx.SetVariable("wibble", "hello world");
            ExpectFailNotIncrementable(parser, ctx, "#wibble++");
            ExpectFailNotDecrementable(parser, ctx, "--#wibble");
            e = parser.ParseExpression("#wibble=#wibble+#wibble");
            var s = e.GetValue<string>(ctx);
            Assert.Equal("hello worldhello world", s);
            Assert.Equal("hello worldhello world", ctx.LookupVariable("wibble"));

            ctx.SetVariable("wobble", 3);
            e = parser.ParseExpression("#wobble++");
            Assert.Equal(3, ctx.LookupVariable<int>("wobble"));
            var r = e.GetValue<int>(ctx);
            Assert.Equal(3, r);
            Assert.Equal(4, ctx.LookupVariable<int>("wobble"));

            e = parser.ParseExpression("--#wobble");
            Assert.Equal(4, ctx.LookupVariable<int>("wobble"));
            r = e.GetValue<int>(ctx);
            Assert.Equal(3, r);
            Assert.Equal(3, ctx.LookupVariable<int>("wobble"));

            e = parser.ParseExpression("#wobble=34");
            Assert.Equal(3, ctx.LookupVariable<int>("wobble"));
            r = e.GetValue<int>(ctx);
            Assert.Equal(34, r);
            Assert.Equal(34, ctx.LookupVariable<int>("wobble"));

            // Projection
            ExpectFailNotIncrementable(parser, ctx, "({1,2,3}.![#IsEven(#this)])++");  // projection would be {false,true,false}
            ExpectFailNotDecrementable(parser, ctx, "--({1,2,3}.![#IsEven(#this)])");  // projection would be {false,true,false}
            ExpectFailNotAssignable(parser, ctx, "({1,2,3}.![#IsEven(#this)])=({1,2,3}.![#IsEven(#this)])");

            // InlineList
            ExpectFailNotAssignable(parser, ctx, "({1,2,3})++");
            ExpectFailNotAssignable(parser, ctx, "--({1,2,3})");
            ExpectFailSetValueNotSupported(parser, ctx, "({1,2,3})=({1,2,3})");

            // InlineMap
            ExpectFailNotAssignable(parser, ctx, "({'a':1,'b':2,'c':3})++");
            ExpectFailNotAssignable(parser, ctx, "--({'a':1,'b':2,'c':3})");
            ExpectFailSetValueNotSupported(parser, ctx, "({'a':1,'b':2,'c':3})=({'a':1,'b':2,'c':3})");

            // ServiceReference
            ctx.ServiceResolver = new MyServiceResolver();
            ExpectFailNotAssignable(parser, ctx, "@foo++");
            ExpectFailNotAssignable(parser, ctx, "--@foo");
            ExpectFailSetValueNotSupported(parser, ctx, "@foo=@bar");

            // PropertyOrFieldReference
            helper.Iii = 42;
            e = parser.ParseExpression("Iii++");
            Assert.Equal(42, helper.Iii);
            r = e.GetValue<int>(ctx);
            Assert.Equal(42, r);
            Assert.Equal(43, helper.Iii);

            e = parser.ParseExpression("--Iii");
            Assert.Equal(43, helper.Iii);
            r = e.GetValue<int>(ctx);
            Assert.Equal(42, r);
            Assert.Equal(42, helper.Iii);

            e = parser.ParseExpression("Iii=100");
            Assert.Equal(42, helper.Iii);
            r = e.GetValue<int>(ctx);
            Assert.Equal(100, r);
            Assert.Equal(100, helper.Iii);
        }

        private void ExpectFailNotAssignable(IExpressionParser parser, IEvaluationContext eContext, string expressionstring)
        {
            ExpectFail(parser, eContext, expressionstring, SpelMessage.NOT_ASSIGNABLE);
        }

        private void ExpectFailSetValueNotSupported(IExpressionParser parser, IEvaluationContext eContext, string expressionstring)
        {
            ExpectFail(parser, eContext, expressionstring, SpelMessage.SETVALUE_NOT_SUPPORTED);
        }

        private void ExpectFailNotIncrementable(IExpressionParser parser, IEvaluationContext eContext, string expressionstring)
        {
            ExpectFail(parser, eContext, expressionstring, SpelMessage.OPERAND_NOT_INCREMENTABLE);
        }

        private void ExpectFailNotDecrementable(IExpressionParser parser, IEvaluationContext eContext, string expressionstring)
        {
            ExpectFail(parser, eContext, expressionstring, SpelMessage.OPERAND_NOT_DECREMENTABLE);
        }

        private void ExpectFail(IExpressionParser parser, IEvaluationContext eContext, string expressionstring, SpelMessage messageCode)
        {
            var ex = Assert.Throws<SpelEvaluationException>(() =>
            {
                var e = parser.ParseExpression(expressionstring);
                if (DEBUG)
                {
                    SpelUtilities.PrintAbstractSyntaxTree(Console.Out, e);
                }

                e.GetValue(eContext);
            });
            Assert.Equal(messageCode, ex.MessageCode);
        }

        #region Test Classes
        #pragma warning disable IDE1006 // Naming Styles
        #pragma warning disable IDE0044 // Add readonly modifier

        public class MyServiceResolver : IServiceResolver
        {
            public object Resolve(IEvaluationContext context, string serviceName)
            {
                if (serviceName.Equals("foo") || serviceName.Equals("bar"))
                {
                    return new Spr9751_2();
                }

                throw new AccessException($"not heard of {serviceName}");
            }
        }

        public class Spr9751
        {
            public string Type = "hello";
            public decimal Bd = 2M;
            public double Ddd = 2.0d;
            public float Fff = 3.0f;
            public long Lll = 66666L;
            public int Iii = 42;
            public short Sss = (short)15;
            public Spr9751_2 Foo = new ();

            public int[] IntArray = new int[] { 1, 2, 3, 4, 5 };
            public int Index1 = 2;

            public int[] IntegerArray;
            public int Index2 = 2;

            public List<string> ListOfStrings;
            public int Index3 = 0;

            public Spr9751()
            {
                IntegerArray = new int[5];
                IntegerArray[0] = 1;
                IntegerArray[1] = 2;
                IntegerArray[2] = 3;
                IntegerArray[3] = 4;
                IntegerArray[4] = 5;
                ListOfStrings = new List<string>
                {
                    "abc"
                };
            }

            public static bool IsEven(int i)
            {
                return (i % 2) == 0;
            }

            public void M()
            {
            }
        }

        public class Spr9751_2
        {
            public int Iii = 99;
        }

        public class CustomMethodResolver : IMethodResolver
        {
            public IMethodExecutor Resolve(IEvaluationContext context, object targetObject, string name, List<Type> argumentTypes)
            {
                return null;
            }
        }

        public class CustomMethodFilter : IMethodFilter
        {
            public List<MethodInfo> Filter(List<MethodInfo> methods)
            {
                return null;
            }
        }

        public class Foo
        {
            public string Bar = "hello";
        }

        public class TestClass
        {
            public Foo Wibble;

            public IDictionary Map;
            public Dictionary<string, int> MapStringToInteger;
            public List<string> List;
            public IList List2;

            public List<string> Foo { get; set; }

            public IList<string> FooIList { get; set; }

            public IDictionary Map2 { get; } = null;

            public Foo Wibble2 { get; } = null;
        }

        #pragma warning restore IDE0044 // Add readonly modifier
        #pragma warning restore IDE1006 // Naming Styles
        #endregion Test Classes
    }
}
