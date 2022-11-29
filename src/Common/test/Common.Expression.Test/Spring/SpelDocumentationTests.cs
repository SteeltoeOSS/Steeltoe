// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Text;
using Steeltoe.Common.Expression.Internal;
using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using Steeltoe.Common.Expression.Test.Spring.TestResources;
using Xunit;

namespace Steeltoe.Common.Expression.Test.Spring;

public class SpelDocumentationTests : AbstractExpressionTests
{
    private static readonly Inventor Tesla = new("Nikola Tesla", new DateTime(1856, 7, 9), "Serbian")
    {
        PlaceOfBirth = new PlaceOfBirth("SmilJan"),
        Inventions = new[]
        {
            "Telephone repeater",
            "Rotating magnetic field principle",
            "Polyphase alternating-current system",
            "Induction motor",
            "Alternating-current power transmission",
            "Tesla coil transformer",
            "Wireless communication",
            "Radio",
            "Fluorescent lights"
        }
    };

    private static readonly Inventor Pupin = new("Pupin", new DateTime(1856, 7, 9), "Idvor")
    {
        PlaceOfBirth = new PlaceOfBirth("Idvor")
    };

    [Fact]
    public void TestMethodInvocation()
    {
        Evaluate("'Hello World'.ToUpperInvariant()", "HELLO WORLD", typeof(string));
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
        IExpression exp = parser.ParseExpression("Name");

        var context = new StandardEvaluationContext();
        context.SetRootObject(tesla);

        string name = (string)exp.GetValue(context);
        Assert.Equal("Nikola Tesla", name);
    }

    [Fact]
    public void TestEqualityCheck()
    {
        var parser = new SpelExpressionParser();

        var context = new StandardEvaluationContext();
        context.SetRootObject(Tesla);

        IExpression exp = parser.ParseExpression("Name == 'Nikola Tesla'");
        bool isEqual = exp.GetValue<bool>(context);
        Assert.True(isEqual);
    }

    // Section 7.4.1
    [Fact]
    public void TestXmlBasedConfig()
    {
        Evaluate("(T(Random).Shared.Next() * 100.0)>0", true, typeof(bool));
    }

    // Section 7.5
    [Fact]
    public void TestLiterals()
    {
        var parser = new SpelExpressionParser();

        string helloWorld = (string)parser.ParseExpression("'Hello World'").GetValue(); // evaluates to "Hello World"
        Assert.Equal("Hello World", helloWorld);

        double avogadrosNumber = parser.ParseExpression("6.0221415E+23").GetValue<double>();
        Assert.InRange(avogadrosNumber, 6.0221415E+23, 6.0221415E+23);

        int maxValue = parser.ParseExpression("0x7FFFFFFF").GetValue<int>(); // evaluates to 2147483647
        Assert.Equal(int.MaxValue, maxValue);

        bool trueValue = parser.ParseExpression("true").GetValue<bool>();
        Assert.True(trueValue);

        object nullValue = parser.ParseExpression("null").GetValue();
        Assert.Null(nullValue);
    }

    [Fact]
    public void TestPropertyAccess()
    {
        StandardEvaluationContext context = TestScenarioCreator.GetTestEvaluationContext();
        int year = Parser.ParseExpression("BirthDate.Year + 1900").GetValue<int>(context); // 1856
        Assert.Equal(3756, year);

        string city = (string)Parser.ParseExpression("PlaceOfBirth.City").GetValue(context);
        Assert.Equal("SmilJan", city);
    }

    [Fact]
    public void TestPropertyNavigation()
    {
        var parser = new SpelExpressionParser();

        // Inventions Array
        StandardEvaluationContext teslaContext = TestScenarioCreator.GetTestEvaluationContext();

        string invention = parser.ParseExpression("Inventions[3]").GetValue<string>(teslaContext);
        Assert.Equal("Induction motor", invention);

        // Members List
        var societyContext = new StandardEvaluationContext();

        var ieee = new InstituteOfElectricalAndElectronicsEngineers
        {
            Members =
            {
                [0] = Tesla
            }
        };

        societyContext.SetRootObject(ieee);

        // Evaluates to "Nikola Tesla"
        string name = parser.ParseExpression("Members[0].Name").GetValue<string>(societyContext);
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
        var president = Parser.ParseExpression("Officers['president']").GetValue<Inventor>(societyContext);
        Assert.NotNull(president);

        // Evaluates to "Idvor"
        string city = Parser.ParseExpression("Officers['president'].PlaceOfBirth.City").GetValue<string>(societyContext);
        Assert.NotNull(city);

        // setting values
        var i = Parser.ParseExpression("Officers['advisors'][0]").GetValue<Inventor>(societyContext);
        Assert.Equal("Nikola Tesla", i.Name);

        Parser.ParseExpression("Officers['advisors'][0].PlaceOfBirth.Country").SetValue(societyContext, "Croatia");

        var i2 = Parser.ParseExpression("Reverse[0]['advisors'][0]").GetValue<Inventor>(societyContext);
        Assert.Equal("Nikola Tesla", i2.Name);
    }

    // 7.5.3
    [Fact]
    public void TestMethodInvocation2()
    {
        // string literal, Evaluates to "bc"
        string c = Parser.ParseExpression("'abc'.Substring(1, 2)").GetValue<string>();
        Assert.Equal("bc", c);

        var societyContext = new StandardEvaluationContext();
        societyContext.SetRootObject(new InstituteOfElectricalAndElectronicsEngineers());

        // Evaluates to true
        bool isMember = Parser.ParseExpression("IsMember('Mihajlo Pupin')").GetValue<bool>(societyContext);
        Assert.True(isMember);
    }

    // 7.5.4.1
    [Fact]
    public void TestRelationalOperators()
    {
        bool result = Parser.ParseExpression("2 == 2").GetValue<bool>();
        Assert.True(result);

        // Evaluates to false
        result = Parser.ParseExpression("2 < -5.0").GetValue<bool>();
        Assert.False(result);

        // Evaluates to true
        result = Parser.ParseExpression("'black' < 'block'").GetValue<bool>();
        Assert.True(result);
    }

    [Fact]
    public void TestOtherOperators()
    {
        // Evaluates to false
        bool falseValue = Parser.ParseExpression("'xyz' instanceof T(int)").GetValue<bool>();
        Assert.False(falseValue);

        // Evaluates to true
        bool trueValue = Parser.ParseExpression("'5.00' matches '^-?\\d+(\\.\\d{2})?$'").GetValue<bool>();
        Assert.True(trueValue);

        // Evaluates to false
        falseValue = Parser.ParseExpression("'5.0067' matches '^-?\\d+(\\.\\d{2})?$'").GetValue<bool>();
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
        bool falseValue = Parser.ParseExpression("true and false").GetValue<bool>();
        Assert.False(falseValue);

        // Evaluates to true
        string expression = "IsMember('Nikola Tesla') and IsMember('Mihajlo Pupin')";
        bool trueValue = Parser.ParseExpression(expression).GetValue<bool>(societyContext);
        Assert.True(trueValue);

        // -- OR --

        // Evaluates to true
        trueValue = Parser.ParseExpression("true or false").GetValue<bool>();
        Assert.True(trueValue);

        // Evaluates to true
        expression = "IsMember('Nikola Tesla') or IsMember('Albert Einstein')";
        trueValue = Parser.ParseExpression(expression).GetValue<bool>(societyContext);
        Assert.True(trueValue);

        // -- NOT --

        // Evaluates to false
        falseValue = Parser.ParseExpression("!true").GetValue<bool>();
        Assert.False(falseValue);

        // -- AND and NOT --
        expression = "IsMember('Nikola Tesla') and !IsMember('Mihajlo Pupin')";
        falseValue = Parser.ParseExpression(expression).GetValue<bool>(societyContext);
        Assert.False(falseValue);
    }

    // 7.5.4.3
    [Fact]
    public void TestNumericalOperators()
    {
        // Addition
        int two = Parser.ParseExpression("1 + 1").GetValue<int>(); // 2
        Assert.Equal(2, two);

        string testString = Parser.ParseExpression("'Test' + ' ' + 'string'").GetValue<string>(); // 'Test string'
        Assert.Equal("Test string", testString);

        // Subtraction
        int four = Parser.ParseExpression("1 - -3").GetValue<int>(); // 4
        Assert.Equal(4, four);

        double d = Parser.ParseExpression("1000.00 - 1e4").GetValue<double>(); // -9000
        Assert.InRange(d, -9000.0d, -9000.0d);

        // Multiplication
        int six = Parser.ParseExpression("-2 * -3").GetValue<int>(); // 6
        Assert.Equal(6, six);

        double twentyFour = Parser.ParseExpression("2.0 * 3e0 * 4").GetValue<double>(); // 24.0
        Assert.InRange(twentyFour, 24.0d, 24.0d);

        // Division
        int minusTwo = Parser.ParseExpression("6 / -3").GetValue<int>(); // -2
        Assert.Equal(-2, minusTwo);

        double one = Parser.ParseExpression("8.0 / 4e0 / 2").GetValue<double>(); // 1.0
        Assert.InRange(one, 1.0d, 1.0d);

        // Modulus
        int three = Parser.ParseExpression("7 % 4").GetValue<int>(); // 3
        Assert.Equal(3, three);

        int oneInt = Parser.ParseExpression("8 / 5 % 2").GetValue<int>(); // 1
        Assert.Equal(1, oneInt);

        // Operator precedence
        int minusTwentyOne = Parser.ParseExpression("1+2-3*8").GetValue<int>(); // -21
        Assert.Equal(-21, minusTwentyOne);
    }

    // 7.5.5
    [Fact]
    public void TestAssignment()
    {
        var inventor = new Inventor();
        var inventorContext = new StandardEvaluationContext();
        inventorContext.SetRootObject(inventor);

        Parser.ParseExpression("Foo").SetValue(inventorContext, "Alexander Seovic2");

        Assert.Equal("Alexander Seovic2", Parser.ParseExpression("Foo").GetValue<string>(inventorContext));

        // alternatively
        string alexander = Parser.ParseExpression("Foo = 'Alexandar Seovic'").GetValue<string>(inventorContext);
        Assert.Equal("Alexandar Seovic", Parser.ParseExpression("Foo").GetValue<string>(inventorContext));
        Assert.Equal("Alexandar Seovic", alexander);
    }

    // 7.5.6
    [Fact]
    public void TestTypes()
    {
        var dateClass = Parser.ParseExpression("T(DateTime)").GetValue<Type>();
        Assert.Equal(typeof(DateTime), dateClass);
        bool trueValue = Parser.ParseExpression("T(TypeCode).Double < T(TypeCode).Decimal").GetValue<bool>();
        Assert.True(trueValue);
    }

    // 7.5.7
    [Fact]
    public void TestConstructors()
    {
        var societyContext = new StandardEvaluationContext();
        societyContext.SetRootObject(new InstituteOfElectricalAndElectronicsEngineers());

        var einstein = Parser.ParseExpression($"new {typeof(Inventor).FullName}('Albert Einstein',new DateTime(1879, 3, 14), 'German')").GetValue<Inventor>();

        Assert.Equal("Albert Einstein", einstein.Name);

        // create new inventor instance within add method of List
        Parser.ParseExpression($"Members2.Add(new {typeof(Inventor).FullName}('Albert Einstein', 'German'))").GetValue(societyContext);
    }

    // 7.5.8
    [Fact]
    public void TestVariables()
    {
        var tesla = new Inventor("Nikola Tesla", "Serbian");
        var context = new StandardEvaluationContext();
        context.SetVariable("newName", "Mike Tesla");

        context.SetRootObject(tesla);

        Parser.ParseExpression("Foo = #newName").GetValue(context);

        Assert.Equal("Mike Tesla", tesla.Foo);
    }

    [Fact]
    public void TestSpecialVariables()
    {
        // create an array of integers
        var primes = new List<int>
        {
            2,
            3,
            5,
            7,
            11,
            13,
            17
        };

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

        string helloWorldReversed = parser.ParseExpression("#reversestring('hello world')").GetValue<string>(context);
        Assert.Equal("dlrow olleh", helloWorldReversed);
    }

    // 7.5.10
    [Fact]
    public void TestTernary()
    {
        string falseString = Parser.ParseExpression("false ? 'trueExp' : 'falseExp'").GetValue<string>();
        Assert.Equal("falseExp", falseString);

        var societyContext = new StandardEvaluationContext();
        societyContext.SetRootObject(new InstituteOfElectricalAndElectronicsEngineers());

        Parser.ParseExpression("Name").SetValue(societyContext, "IEEE");
        societyContext.SetVariable("queryName", "Nikola Tesla");

        const string expression = "IsMember(#queryName)? #queryName + ' is a member of the ' " +
            "+ Name + ' Society' : #queryName + ' is not a member of the ' + Name + ' Society'";

        string queryResultString = Parser.ParseExpression(expression).GetValue<string>(societyContext);
        Assert.Equal("Nikola Tesla is a member of the IEEE Society", queryResultString);

        // queryResultstring = "Nikola Tesla is a member of the IEEE Society"
    }

    // 7.5.11
    [Fact]
    public void TestSelection()
    {
        var societyContext = new StandardEvaluationContext();
        societyContext.SetRootObject(new InstituteOfElectricalAndElectronicsEngineers());
        var list = (List<object>)Parser.ParseExpression("Members2.?[Nationality == 'Serbian']").GetValue(societyContext);
        Assert.Single(list);
        Assert.Equal("Nikola Tesla", ((Inventor)list[0]).Name);
    }

    // 7.5.12
    [Fact]
    public void TestTemplating()
    {
        string randomPhrase = Parser.ParseExpression("random number is ${T(Random).Shared.Next()}", new TemplatedParserContext()).GetValue<string>();
        Assert.StartsWith("random number", randomPhrase, StringComparison.Ordinal);
    }

    public static class StringUtils
    {
        public static string ReverseString(string input)
        {
            var backwards = new StringBuilder();

            for (int i = 0; i < input.Length; i++)
            {
                backwards.Append(input[input.Length - 1 - i]);
            }

            return backwards.ToString();
        }
    }

    public class InstituteOfElectricalAndElectronicsEngineers
    {
        public Inventor[] Members { get; } = new Inventor[1];
        public List<object> Members2 { get; } = new();
        public Dictionary<string, object> Officers { get; } = new();
        public List<Dictionary<string, object>> Reverse { get; } = new();

        public string Name { get; set; }

        public InstituteOfElectricalAndElectronicsEngineers()
        {
            Officers.Add("president", Pupin);

            var list = new List<object>
            {
                Tesla
            };

            Officers.Add("advisors", list);
            Members2.Add(Tesla);
            Members2.Add(Pupin);

            Reverse.Add(Officers);
        }

        public bool IsMember(string name)
        {
            return true;
        }
    }

    public class TemplatedParserContext : IParserContext
    {
        public string ExpressionPrefix => "${";

        public string ExpressionSuffix => "}";

        public bool IsTemplate => true;
    }
}
