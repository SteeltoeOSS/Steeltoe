// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;
using Steeltoe.Common.Expression.Internal;
using Steeltoe.Common.Expression.Internal.Spring;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using Steeltoe.Common.Expression.Test.Spring.TestResources;
using Xunit;

namespace Steeltoe.Common.Expression.Test.Spring;

public class SetValueTests : AbstractExpressionTests
{
    private static readonly bool IsDebug = bool.Parse(bool.FalseString);

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
        SetValueExpectError($"new {typeof(Inventor).FullName}().Inventions[1]", SpelMessage.CannotIndexIntoNullValue);
    }

    [Fact]
    public void TestSetArrayElementValueAllPrimitiveTypes()
    {
        SetValue("ArrayContainer.Integers[1]", 3);
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
        StandardEvaluationContext lContext = TestScenarioCreator.GetTestEvaluationContext();

        // PROPERTYORFIELDREFERENCE
        // Non existent field (or property):
        IExpression e1 = Parser.ParseExpression("ArrayContainer.wibble");
        Assert.False(e1.IsWritable(lContext));

        IExpression e2 = Parser.ParseExpression("ArrayContainer.wibble.foo");
        Assert.Throws<SpelEvaluationException>(() => e2.IsWritable(lContext));

        // org.springframework.expression.spel.SpelEvaluationException: EL1008E:(pos 15): Property or field 'wibble' cannot be found on object of type 'org.springframework.expression.spel.Testresources.ArrayContainer' - maybe not public?
        // at org.springframework.expression.spel.ast.PropertyOrFieldReference.readProperty(PropertyOrFieldReference.java:225)
        // VARIABLE
        // the variable does not exist (but that is OK, we should be writable)
        IExpression e3 = Parser.ParseExpression("#madeup1");
        Assert.True(e3.IsWritable(lContext));

        IExpression e4 = Parser.ParseExpression("#madeup2.bar"); // compound expression
        Assert.False(e4.IsWritable(lContext));

        // INDEXER
        // non existent indexer (wibble made up)
        IExpression e5 = Parser.ParseExpression("ArrayContainer.wibble[99]");
        Assert.Throws<SpelEvaluationException>(() => e5.IsWritable(lContext));

        // non existent indexer (index via a string)
        IExpression e6 = Parser.ParseExpression("ArrayContainer.ints['abc']");
        Assert.Throws<SpelEvaluationException>(() => e6.IsWritable(lContext));
    }

    [Fact]
    public void TestSetArrayElementValueAllPrimitiveTypesErrors()
    {
        // none of these sets are possible due to (expected) conversion problems
        SetValueExpectError("ArrayContainer.Integers[1]", "wibble");
        SetValueExpectError("ArrayContainer.Floats[1]", "dribble");
        SetValueExpectError("ArrayContainer.Booleans[1]", "nein");
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
        SetValue("PlacesLivedList[0]", "Wien");
    }

    [Fact]
    public void TestSetGenericListElementValueTypeCoersionOk()
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
        StandardEvaluationContext eContext = TestScenarioCreator.GetTestEvaluationContext();
        IExpression e = Parse("PublicName='Andy'");
        Assert.False(e.IsWritable(eContext));
        Assert.Equal("Andy", e.GetValue(eContext));
    }

    /*
     * Testing the coercion of both the keys and the values to the correct type
     */
    [Fact]
    public void TestSetGenericMapElementRequiresCoercion()
    {
        StandardEvaluationContext eContext = TestScenarioCreator.GetTestEvaluationContext();
        IExpression e = Parse("MapOfStringToBoolean[42]");
        Assert.Null(e.GetValue(eContext));

        // Key should be coerced to string representation of 42
        e.SetValue(eContext, "true");

        // All keys should be strings
        var ks = Parse("MapOfStringToBoolean.Keys").GetValue<ICollection>(eContext);

        foreach (object key in ks)
        {
            Assert.IsType<string>(key);
        }

        // All values should be booleans
        var vs = Parse("MapOfStringToBoolean.Values").GetValue<ICollection>(eContext);

        foreach (object val in vs)
        {
            Assert.IsType<bool>(val);
        }

        // One final Test check coercion on the key for a map lookup
        bool o = e.GetValue<bool>(eContext);
        Assert.True(o);
    }

    protected void SetValueExpectError(string expression, object value)
    {
        IExpression e = Parser.ParseExpression(expression);
        Assert.NotNull(e);

        if (IsDebug)
        {
            SpelUtilities.PrintAbstractSyntaxTree(Console.Out, e);
        }

        StandardEvaluationContext lContext = TestScenarioCreator.GetTestEvaluationContext();
        Assert.Throws<SpelEvaluationException>(() => e.SetValue(lContext, value));
    }

    protected void SetValue(string expression, object value)
    {
        try
        {
            IExpression e = Parser.ParseExpression(expression);
            Assert.NotNull(e);

            if (IsDebug)
            {
                SpelUtilities.PrintAbstractSyntaxTree(Console.Out, e);
            }

            StandardEvaluationContext lContext = TestScenarioCreator.GetTestEvaluationContext();
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
            IExpression e = Parser.ParseExpression(expression);
            Assert.NotNull(e);

            if (IsDebug)
            {
                SpelUtilities.PrintAbstractSyntaxTree(Console.Out, e);
            }

            StandardEvaluationContext lContext = TestScenarioCreator.GetTestEvaluationContext();
            Assert.True(e.IsWritable(lContext));
            e.SetValue(lContext, value);
            object a = expectedValue;
            object b = e.GetValue(lContext);
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

    private IExpression Parse(string expressionString)
    {
        return Parser.ParseExpression(expressionString);
    }
}
