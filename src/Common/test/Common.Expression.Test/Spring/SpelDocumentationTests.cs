// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using Steeltoe.Common.Expression.Internal.Spring.TestResources;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Spring;

public class SpelDocumentationTests : AbstractExpressionTests
{
    private static readonly Inventor Tesla = new ("Nikola Tesla", new DateTime(1856, 7, 9), "Serbian")
    {
        PlaceOfBirth = new PlaceOfBirth("SmilJan"),
        Inventions = new[]
        {
            "Telephone repeater", "Rotating magnetic field principle",
            "Polyphase alternating-current system", "Induction motor", "Alternating-current power transmission",
            "Tesla coil transformer", "Wireless communication", "Radio", "Fluorescent lights"
        }
    };

    private static readonly Inventor Pupin = new ("Pupin", new DateTime(1856, 7, 9), "Idvor")
    {
        PlaceOfBirth = new PlaceOfBirth("Idvor")
    };

    [Fact]
    public void TestMethodInvocation()
    {
        Evaluate("'Hello World'.ToUpper()", "HELLO WORLD", typeof(string));
    }

    [Fact]
    public void TestBeanPropertyAccess()
    {
        Evaluate("new String('Hello World'[0])", "H", typeof(string));
    }

    [Fact]
    public void TestArrayLengthAccess()
    {
        Evaluate("'Hello World'.ToCharArray().Length", 11, typeof(int));
    }

    [Fact]
    public void TestRootObject()
    {
        // The constructor arguments are name, birthday, and nationality.
        var tesla = new Inventor("Nikola Tesla", new DateTime(1856, 7, 9), "Serbian");

        var parser = new SpelExpressionParser();
        var exp = parser.ParseExpression("Name");

        var context = new StandardEvaluationContext();
        context.SetRootObject(tesla);

        var name = (string)exp.GetValue(context);
        Assert.Equal("Nikola Tesla", name);
    }

    [Fact]
    public void TestEqualityCheck()
    {
        var parser = new SpelExpressionParser();

        var context = new StandardEvaluationContext();
        context.SetRootObject(Tesla);

        var exp = parser.ParseExpression("Name == 'Nikola Tesla'");
        var isEqual = exp.GetValue<bool>(context);
        Assert.True(isEqual);
    }

    // Section 7.4.1
    [Fact]
    public void TestXMLBasedConfig()
    {
        Evaluate("(new Random().Next() * 100.0)>0", true, typeof(bool));
    }

    // Section 7.5
    [Fact]
    public void TestLiterals()
    {
        var parser = new SpelExpressionParser();

        var helloWorld = (string)parser.ParseExpression("'Hello World'").GetValue(); // evaluates to "Hello World"
        Assert.Equal("Hello World", helloWorld);

        var avogadrosNumber = parser.ParseExpression("6.0221415E+23").GetValue<double>();
        Assert.InRange(avogadrosNumber, 6.0221415E+23, 6.0221415E+23);

        var maxValue = parser.ParseExpression("0x7FFFFFFF").GetValue<int>();  // evaluates to 2147483647
        Assert.Equal(int.MaxValue, maxValue);

        var trueValue = parser.ParseExpression("true").GetValue<bool>();
        Assert.True(trueValue);

        var nullValue = parser.ParseExpression("null").GetValue();
        Assert.Null(nullValue);
    }

    [Fact]
    public void TestPropertyAccess()
    {
        var context = TestScenarioCreator.GetTestEvaluationContext();
        var year = _parser.ParseExpression("BirthDate.Year + 1900").GetValue<int>(context); // 1856
        Assert.Equal(3756, year);

        var city = (string)_parser.ParseExpression("PlaceOfBirth.City").GetValue(context);
        Assert.Equal("SmilJan", city);
    }

    [Fact]
    public void TestPropertyNavigation()
    {
        var parser = new SpelExpressionParser();

        // Inventions Array
        var teslaContext = TestScenarioCreator.GetTestEvaluationContext();

        // teslaContext.SetRootObject(tesla);
        // Evaluates to "Induction motor"
        var invention = parser.ParseExpression("Inventions[3]").GetValue<string>(teslaContext);
        Assert.Equal("Induction motor", invention);

        // Members List
        var societyContext = new StandardEvaluationContext();
        var ieee = new InstituteOfElectricalAndElectronicsEngineers { Members = { [0] = Tesla } };
        societyContext.SetRootObject(ieee);

        // Evaluates to "Nikola Tesla"
        var name = parser.ParseExpression("Members[0].Name").GetValue<string>(societyContext);
        Assert.Equal("Nikola Tesla", name);

        // List and Array navigation
        // Evaluates to "Wireless communication"
        invention = parser.ParseExpression("Members[0].Inventions[6]").GetValue<string>(societyContext);
        Assert.Equal("Wireless communication", invention);
    }

    [Fact]
    public void TestDictionaryAccess()
    {
        var societyContext = new StandardEvaluationContext();
        societyContext.SetRootObject(new InstituteOfElectricalAndElectronicsEngineers());

        // Officer's Dictionary
        var pupin = _parser.ParseExpression("Officers['president']").GetValue<Inventor>(societyContext);
        Assert.NotNull(pupin);

        // Evaluates to "Idvor"
        var city = _parser.ParseExpression("Officers['president'].PlaceOfBirth.City").GetValue<string>(societyContext);
        Assert.NotNull(city);

        // setting values
        var i = _parser.ParseExpression("Officers['advisors'][0]").GetValue<Inventor>(societyContext);
        Assert.Equal("Nikola Tesla", i.Name);

        _parser.ParseExpression("Officers['advisors'][0].PlaceOfBirth.Country").SetValue(societyContext, "Croatia");

        var i2 = _parser.ParseExpression("Reverse[0]['advisors'][0]").GetValue<Inventor>(societyContext);
        Assert.Equal("Nikola Tesla", i2.Name);
    }

    // 7.5.3
    [Fact]
    public void TestMethodInvocation2()
    {
        // string literal, Evaluates to "bc"
        var c = _parser.ParseExpression("'abc'.Substring(1, 2)").GetValue<string>();
        Assert.Equal("bc", c);

        var societyContext = new StandardEvaluationContext();
        societyContext.SetRootObject(new InstituteOfElectricalAndElectronicsEngineers());

        // Evaluates to true
        var isMember = _parser.ParseExpression("IsMember('Mihajlo Pupin')").GetValue<bool>(societyContext);
        Assert.True(isMember);
    }

    // 7.5.4.1
    [Fact]
    public void TestRelationalOperators()
    {
        var result = _parser.ParseExpression("2 == 2").GetValue<bool>();
        Assert.True(result);

        // Evaluates to false
        result = _parser.ParseExpression("2 < -5.0").GetValue<bool>();
        Assert.False(result);

        // Evaluates to true
        result = _parser.ParseExpression("'black' < 'block'").GetValue<bool>();
        Assert.True(result);
    }

    [Fact]
    public void TestOtherOperators()
    {
        // Evaluates to false
        var falseValue = _parser.ParseExpression("'xyz' instanceof T(int)").GetValue<bool>();
        Assert.False(falseValue);

        // Evaluates to true
        var trueValue = _parser.ParseExpression("'5.00' matches '^-?\\d+(\\.\\d{2})?$'").GetValue<bool>();
        Assert.True(trueValue);

        // Evaluates to false
        falseValue = _parser.ParseExpression("'5.0067' matches '^-?\\d+(\\.\\d{2})?$'").GetValue<bool>();
        Assert.False(falseValue);
    }

    // 7.5.4.2
    [Fact]
    public void TestLogicalOperators()
    {
        var societyContext = new StandardEvaluationContext();
        societyContext.SetRootObject(new InstituteOfElectricalAndElectronicsEngineers());

        // -- AND --

        // Evaluates to false
        var falseValue = _parser.ParseExpression("true and false").GetValue<bool>();
        Assert.False(falseValue);

        // Evaluates to true
        var expression = "IsMember('Nikola Tesla') and IsMember('Mihajlo Pupin')";
        var trueValue = _parser.ParseExpression(expression).GetValue<bool>(societyContext);
        Assert.True(trueValue);

        // -- OR --

        // Evaluates to true
        trueValue = _parser.ParseExpression("true or false").GetValue<bool>();
        Assert.True(trueValue);

        // Evaluates to true
        expression = "IsMember('Nikola Tesla') or IsMember('Albert Einstien')";
        trueValue = _parser.ParseExpression(expression).GetValue<bool>(societyContext);
        Assert.True(trueValue);

        // -- NOT --

        // Evaluates to false
        falseValue = _parser.ParseExpression("!true").GetValue<bool>();
        Assert.False(falseValue);

        // -- AND and NOT --
        expression = "IsMember('Nikola Tesla') and !IsMember('Mihajlo Pupin')";
        falseValue = _parser.ParseExpression(expression).GetValue<bool>(societyContext);
        Assert.False(falseValue);
    }

    // 7.5.4.3
    [Fact]
    public void TestNumericalOperators()
    {
        // Addition
        var two = _parser.ParseExpression("1 + 1").GetValue<int>(); // 2
        Assert.Equal(2, two);

        var testString = _parser.ParseExpression("'Test' + ' ' + 'string'").GetValue<string>(); // 'Test string'
        Assert.Equal("Test string", testString);

        // Subtraction
        var four = _parser.ParseExpression("1 - -3").GetValue<int>(); // 4
        Assert.Equal(4, four);

        var d = _parser.ParseExpression("1000.00 - 1e4").GetValue<double>(); // -9000
        Assert.InRange(d, -9000.0d, -9000.0d);

        // Multiplication
        var six = _parser.ParseExpression("-2 * -3").GetValue<int>(); // 6
        Assert.Equal(6, six);

        var twentyFour = _parser.ParseExpression("2.0 * 3e0 * 4").GetValue<double>(); // 24.0
        Assert.InRange(twentyFour, 24.0d, 24.0d);

        // Division
        var minusTwo = _parser.ParseExpression("6 / -3").GetValue<int>(); // -2
        Assert.Equal(-2, minusTwo);

        var one = _parser.ParseExpression("8.0 / 4e0 / 2").GetValue<double>(); // 1.0
        Assert.InRange(one, 1.0d, 1.0d);

        // Modulus
        var three = _parser.ParseExpression("7 % 4").GetValue<int>(); // 3
        Assert.Equal(3, three);

        var oneInt = _parser.ParseExpression("8 / 5 % 2").GetValue<int>(); // 1
        Assert.Equal(1, oneInt);

        // Operator precedence
        var minusTwentyOne = _parser.ParseExpression("1+2-3*8").GetValue<int>(); // -21
        Assert.Equal(-21, minusTwentyOne);
    }

    // 7.5.5
    [Fact]
    public void TestAssignment()
    {
        var inventor = new Inventor();
        var inventorContext = new StandardEvaluationContext();
        inventorContext.SetRootObject(inventor);

        _parser.ParseExpression("Foo").SetValue(inventorContext, "Alexander Seovic2");

        Assert.Equal("Alexander Seovic2", _parser.ParseExpression("Foo").GetValue<string>(inventorContext));

        // alternatively
        var aleks = _parser.ParseExpression("Foo = 'Alexandar Seovic'").GetValue<string>(inventorContext);
        Assert.Equal("Alexandar Seovic", _parser.ParseExpression("Foo").GetValue<string>(inventorContext));
        Assert.Equal("Alexandar Seovic", aleks);
    }

    // 7.5.6
    [Fact]
    public void TestTypes()
    {
        var dateClass = _parser.ParseExpression("T(DateTime)").GetValue<Type>();
        Assert.Equal(typeof(DateTime), dateClass);
        var trueValue = _parser.ParseExpression("T(TypeCode).Double < T(TypeCode).Decimal").GetValue<bool>();
        Assert.True(trueValue);
    }

    // 7.5.7
    [Fact]
    public void TestConstructors()
    {
        var societyContext = new StandardEvaluationContext();
        societyContext.SetRootObject(new InstituteOfElectricalAndElectronicsEngineers());
        var einstein = _parser.ParseExpression("new Steeltoe.Common.Expression.Internal.Spring.TestResources.Inventor('Albert Einstein',new DateTime(1879, 3, 14), 'German')").GetValue<Inventor>();
        Assert.Equal("Albert Einstein", einstein.Name);

        // create new inventor instance within add method of List
        _parser.ParseExpression("Members2.Add(new Steeltoe.Common.Expression.Internal.Spring.TestResources.Inventor('Albert Einstein', 'German'))").GetValue(societyContext);
    }

    // 7.5.8
    [Fact]
    public void TestVariables()
    {
        var tesla = new Inventor("Nikola Tesla", "Serbian");
        var context = new StandardEvaluationContext();
        context.SetVariable("newName", "Mike Tesla");

        context.SetRootObject(tesla);

        _parser.ParseExpression("Foo = #newName").GetValue(context);

        Assert.Equal("Mike Tesla", tesla.Foo);
    }

    [Fact]
    public void TestSpecialVariables()
    {
        // create an array of integers
        var primes = new List<int> { 2, 3, 5, 7, 11, 13, 17 };

        // create parser and set variable 'primes' as the array of integers
        var parser = new SpelExpressionParser();
        var context = new StandardEvaluationContext();
        context.SetVariable("primes", primes);

        // all prime numbers > 10 from the list (using selection ?{...})
        var primesGreaterThanTen = parser.ParseExpression("#primes.?[#this>10]").GetValue<List<object>>(context);
        Assert.Equal(3, primesGreaterThanTen.Count);
        Assert.Equal(11, primesGreaterThanTen[0]);
        Assert.Equal(13, primesGreaterThanTen[1]);
        Assert.Equal(17, primesGreaterThanTen[2]);
    }

    // 7.5.9
    [Fact]
    public void TestFunctions()
    {
        var parser = new SpelExpressionParser();
        var context = new StandardEvaluationContext();
        context.RegisterFunction("reversestring", typeof(StringUtils).GetMethod(nameof(StringUtils.ReverseString), BindingFlags.Public | BindingFlags.Static));

        var helloWorldReversed = parser.ParseExpression("#reversestring('hello world')").GetValue<string>(context);
        Assert.Equal("dlrow olleh", helloWorldReversed);
    }

    // 7.5.10
    [Fact]
    public void TestTernary()
    {
        var falsestring = _parser.ParseExpression("false ? 'trueExp' : 'falseExp'").GetValue<string>();
        Assert.Equal("falseExp", falsestring);

        var societyContext = new StandardEvaluationContext();
        societyContext.SetRootObject(new InstituteOfElectricalAndElectronicsEngineers());

        _parser.ParseExpression("Name").SetValue(societyContext, "IEEE");
        societyContext.SetVariable("queryName", "Nikola Tesla");

        var expression = "IsMember(#queryName)? #queryName + ' is a member of the ' "
                         + "+ Name + ' Society' : #queryName + ' is not a member of the ' + Name + ' Society'";

        var queryResultstring = _parser.ParseExpression(expression).GetValue<string>(societyContext);
        Assert.Equal("Nikola Tesla is a member of the IEEE Society", queryResultstring);

        // queryResultstring = "Nikola Tesla is a member of the IEEE Society"
    }

    // 7.5.11
    [Fact]
    public void TestSelection()
    {
        var societyContext = new StandardEvaluationContext();
        societyContext.SetRootObject(new InstituteOfElectricalAndElectronicsEngineers());
        var list = (List<object>)_parser.ParseExpression("Members2.?[Nationality == 'Serbian']").GetValue(societyContext);
        Assert.Single(list);
        Assert.Equal("Nikola Tesla", ((Inventor)list[0]).Name);
    }

    // 7.5.12
    [Fact]
    public void TestTemplating()
    {
        var randomPhrase = _parser.ParseExpression("random number is ${new Random().Next()}", new TemplatedParserContext()).GetValue<string>();
        Assert.StartsWith("random number", randomPhrase);
    }

    public static class StringUtils
    {
        public static string ReverseString(string input)
        {
            var backwards = new StringBuilder();
            for (var i = 0; i < input.Length; i++)
            {
                backwards.Append(input[input.Length - 1 - i]);
            }

            return backwards.ToString();
        }
    }

    public class InstituteOfElectricalAndElectronicsEngineers
    {
        public Inventor[] Members = new Inventor[1];
        public List<object> Members2 = new ();
        public Dictionary<string, object> Officers = new ();
        public List<Dictionary<string, object>> Reverse = new ();

        public InstituteOfElectricalAndElectronicsEngineers()
        {
            Officers.Add("president", Pupin);
            var linv = new List<object>
            {
                Tesla
            };
            Officers.Add("advisors", linv);
            Members2.Add(Tesla);
            Members2.Add(Pupin);

            Reverse.Add(Officers);
        }

        public bool IsMember(string name)
        {
            return true;
        }

        public string Name { get; set; }
    }

    public class TemplatedParserContext : IParserContext
    {
        public string ExpressionPrefix => "${";

        public string ExpressionSuffix => "}";

        public bool IsTemplate => true;
    }
}
