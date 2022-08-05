// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable xUnit1004 // Test methods should not be skipped
// ReSharper disable InconsistentNaming

namespace Steeltoe.Common.Expression.Internal.Spring;

public class SpelCompilationPerformanceTests : AbstractExpressionTests
{
    private const bool NoisyTests = true;

    private readonly int _count = 50000; // number of evaluations that are timed in one run

    private readonly int _iterations = 10; // number of times to repeat 'count' evaluations (for averaging)

    private readonly ITestOutputHelper _output;

    private IExpression _expression;

    public SpelCompilationPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(Skip = "Time sensitive test, sometimes fails on CI")]
    public void CompilingMathematicalExpressionsWithDifferentOperandTypes()
    {
        var nh = new NumberHolder();
        _expression = Parser.ParseExpression("(T(Convert).ToDouble(Payload))/18D");
        object o = _expression.GetValue(nh);
        Assert.Equal(2d, o);
        _output.WriteLine("Performance check for SpEL expression: 'Convert.ToDouble(Payload)/18D'");

        long startTime = DateTime.Now.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(nh);
        }

        _output.WriteLine("One million iterations: " + (DateTime.Now.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.Now.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(nh);
        }

        _output.WriteLine("One million iterations: " + (DateTime.Now.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.Now.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(nh);
        }

        _output.WriteLine("One million iterations: " + (DateTime.Now.Ticks - startTime) / 10000 + "ms");

        Compile(_expression);
        _output.WriteLine("Now compiled:");
        o = _expression.GetValue(nh);
        Assert.Equal(2d, o);

        startTime = DateTime.Now.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(nh);
        }

        _output.WriteLine("One million iterations: " + (DateTime.Now.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.Now.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(nh);
        }

        _output.WriteLine("One million iterations: " + (DateTime.Now.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.Now.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(nh);
        }

        _output.WriteLine("One million iterations: " + (DateTime.Now.Ticks - startTime) / 10000 + "ms");

        _expression = Parser.ParseExpression("Payload/18D");
        o = _expression.GetValue(nh);
        Assert.Equal(2d, o);
        _output.WriteLine("Performance check for SpEL expression: 'Payload / 18D");

        startTime = DateTime.Now.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(nh);
        }

        _output.WriteLine("One million iterations: " + (DateTime.Now.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.Now.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(nh);
        }

        _output.WriteLine("One million iterations: " + (DateTime.Now.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.Now.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(nh);
        }

        _output.WriteLine("One million iterations: " + (DateTime.Now.Ticks - startTime) / 10000 + "ms");

        Compile(_expression);
        _output.WriteLine("Now compiled:");
        o = _expression.GetValue(nh);
        Assert.Equal(2d, o);

        startTime = DateTime.Now.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(nh);
        }

        _output.WriteLine("One million iterations: " + (DateTime.Now.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.Now.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(nh);
        }

        _output.WriteLine("One million iterations: " + (DateTime.Now.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.Now.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(nh);
        }

        _output.WriteLine("One million iterations: " + (DateTime.Now.Ticks - startTime) / 10000 + "ms");
    }

    [Fact(Skip = "Time sensitive test, sometimes fails on CI")]
    public void InlineLists()
    {
        _expression = Parser.ParseExpression("{'abcde','ijklm'}[0].Substring({1,3,4}[0],{1,3,4}[1])");
        object o = _expression.GetValue();
        Assert.Equal("bcd", o);
        _output.WriteLine("Performance check for SpEL expression: '{'abcde','ijklm'}[0].substring({1,3,4}[0],{1,3,4}[1])'");

        long startTime = DateTime.Now.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue();
        }

        _output.WriteLine("One million iterations: " + (DateTime.Now.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.Now.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue();
        }

        _output.WriteLine("One million iterations: " + (DateTime.Now.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.Now.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue();
        }

        _output.WriteLine("One million iterations: " + (DateTime.Now.Ticks - startTime) / 10000 + "ms");

        Compile(_expression);
        _output.WriteLine("Now compiled:");
        o = _expression.GetValue();
        Assert.Equal("bcd", o);

        startTime = DateTime.Now.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue();
        }

        _output.WriteLine("One million iterations: " + (DateTime.Now.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.Now.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue();
        }

        _output.WriteLine("One million iterations: " + (DateTime.Now.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.Now.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue();
        }

        _output.WriteLine("One million iterations: " + (DateTime.Now.Ticks - startTime) / 10000 + "ms");
    }

    [Fact(Skip = "Time sensitive test, sometimes fails on CI")]
    public void InlineNestedLists()
    {
        _expression = Parser.ParseExpression("{'abcde',{'ijklm','nopqr'}}[1][0].Substring({1,3,4}[0],{1,3,4}[1])");
        object o = _expression.GetValue();
        Assert.Equal("jkl", o);
        _output.WriteLine("Performance check for SpEL expression: '{'abcde',{'ijklm','nopqr'}}[1][0].Substring({1,3,4}[0],{1,3,4}[1])'");

        long startTime = DateTime.Now.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue();
        }

        _output.WriteLine("One million iterations: " + (DateTime.Now.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.Now.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue();
        }

        _output.WriteLine("One million iterations: " + (DateTime.Now.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.Now.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue();
        }

        _output.WriteLine("One million iterations: " + (DateTime.Now.Ticks - startTime) / 10000 + "ms");

        Compile(_expression);
        _output.WriteLine("Now compiled:");
        o = _expression.GetValue();
        Assert.Equal("jkl", o);

        startTime = DateTime.Now.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue();
        }

        _output.WriteLine("One million iterations: " + (DateTime.Now.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.Now.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue();
        }

        _output.WriteLine("One million iterations: " + (DateTime.Now.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.Now.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue();
        }

        _output.WriteLine("One million iterations: " + (DateTime.Now.Ticks - startTime) / 10000 + "ms");
    }

    [Fact(Skip = "Time sensitive test, sometimes fails on CI")]
    public void StringConcatenation()
    {
        var g = new Greeter();
        _expression = Parser.ParseExpression("'hello' + World + ' spring'");
        object o = _expression.GetValue(g);
        Assert.Equal("helloworld spring", o);
        _output.WriteLine("Performance check for SpEL expression: 'hello' + World + ' spring'");

        long startTime = DateTime.Now.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(g);
        }

        _output.WriteLine("One million iterations: " + (DateTime.Now.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.Now.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(g);
        }

        _output.WriteLine("One million iterations: " + (DateTime.Now.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.Now.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(g);
        }

        _output.WriteLine("One million iterations: " + (DateTime.Now.Ticks - startTime) / 10000 + "ms");

        Compile(_expression);
        _output.WriteLine("Now compiled:");
        o = _expression.GetValue(g);
        Assert.Equal("helloworld spring", o);

        startTime = DateTime.Now.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(g);
        }

        _output.WriteLine("One million iterations: " + (DateTime.Now.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.Now.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(g);
        }

        _output.WriteLine("One million iterations: " + (DateTime.Now.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.Now.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(g);
        }

        _output.WriteLine("One million iterations: " + (DateTime.Now.Ticks - startTime) / 10000 + "ms");
    }

    [Fact(Skip = "Time sensitive test, sometimes fails on CI")]
    public void ComplexExpressionPerformance()
    {
        var payload = new Payload();
        IExpression expression = Parser.ParseExpression("DR[0].DRFixedSection.Duration lt 0.1");
        bool b = false;
        long iTotal = 0;
        const long cTotal = 0;

        // warmup
        for (int i = 0; i < _count; i++)
        {
            b = expression.GetValue<bool>(payload);
        }

        // Verify the result
        Assert.False(b);

        Log("timing interpreted: ");

        for (int i = 0; i < _iterations; i++)
        {
            long startTime = DateTime.Now.Ticks;

            for (int j = 0; j < _count; j++)
            {
                b = expression.GetValue<bool>(payload);
            }

            long endTime = DateTime.Now.Ticks;
            long interpretedSpeed = endTime - startTime;
            iTotal += interpretedSpeed;
            Log($"{interpretedSpeed}ticks ");
        }

        LogLn();

        Compile(expression);
        bool bc = false;
        expression.GetValue<bool>(payload);
        Log("timing compiled: ");

        for (int i = 0; i < _iterations; i++)
        {
            long startTime = DateTime.Now.Ticks;

            for (int j = 0; j < _count; j++)
            {
                bc = expression.GetValue<bool>(payload);
            }

            long endTime = DateTime.Now.Ticks;
            long interpretedSpeed = endTime - startTime;
            iTotal += interpretedSpeed;
            Log($"{interpretedSpeed}ticks ");
        }

        LogLn();
        ReportPerformance("complex expression", iTotal, cTotal);

        // Verify the result
        Assert.False(b);

        // Verify the same result for compiled vs interpreted
        Assert.Equal(b, bc);

        // Verify if the input changes, the result changes
        payload.DR[0].DRFixedSection.Duration = 0.04d;
        bc = expression.GetValue<bool>(payload);
        Assert.True(bc);
    }

    [Fact(Skip = "Time sensitive test, sometimes fails on CI")]
    public void CompilingMethodReference()
    {
        long interpretedTotal = 0;
        const long compiledTotal = 0;
        long startTime;
        long endTime;
        string interpretedResult = null;
        string compiledResult = null;
        var testData = new HW();
        IExpression expression = Parser.ParseExpression("Hello()");

        // warmup
        for (int i = 0; i < _count; i++)
        {
            interpretedResult = expression.GetValue<string>(testData);
        }

        Log("timing interpreted: ");

        for (int i = 0; i < _iterations; i++)
        {
            startTime = DateTime.Now.Ticks;

            for (int j = 0; j < _count; j++)
            {
                interpretedResult = expression.GetValue<string>(testData);
            }

            endTime = DateTime.Now.Ticks;
            long interpretedSpeed = endTime - startTime;
            interpretedTotal += interpretedSpeed;
            Log($"{interpretedSpeed}ticks ");
        }

        LogLn();

        Compile(expression);

        Log("timing compiled: ");
        expression.GetValue<string>(testData);

        for (int i = 0; i < _iterations; i++)
        {
            startTime = DateTime.Now.Ticks;

            for (int j = 0; j < _count; j++)
            {
                compiledResult = expression.GetValue<string>(testData);
            }

            endTime = DateTime.Now.Ticks;
            long interpretedSpeed = endTime - startTime;
            interpretedTotal += interpretedSpeed;
            Log($"{interpretedSpeed}ticks ");
        }

        LogLn();

        Assert.Equal(interpretedResult, compiledResult);
        ReportPerformance("method reference", interpretedTotal, compiledTotal);

        if (compiledTotal >= interpretedTotal)
        {
            throw new Exception("Compiled version is slower than interpreted!");
        }
    }

    [Fact(Skip = "Time sensitive test, sometimes fails on CI")]
    public void CompilingPropertyReferenceField()
    {
        long interpretedTotal = 0;
        const long compiledTotal = 0;
        long startTime;
        long endTime;
        string interpretedResult = null;
        string compiledResult = null;
        var testData = new TestClass2();
        IExpression expression = Parser.ParseExpression("Name");

        // warmup
        for (int i = 0; i < _count; i++)
        {
            interpretedResult = expression.GetValue<string>(testData);
        }

        Log("timing interpreted: ");

        for (int i = 0; i < _iterations; i++)
        {
            startTime = DateTime.Now.Ticks;

            for (int j = 0; j < _count; j++)
            {
                interpretedResult = expression.GetValue<string>(testData);
            }

            endTime = DateTime.Now.Ticks;
            long interpretedSpeed = endTime - startTime;
            interpretedTotal += interpretedSpeed;
            Log($"{interpretedSpeed}ticks ");
        }

        LogLn();

        Compile(expression);

        Log("timing compiled: ");
        expression.GetValue<string>(testData);

        for (int i = 0; i < _iterations; i++)
        {
            startTime = DateTime.Now.Ticks;

            for (int j = 0; j < _count; j++)
            {
                compiledResult = expression.GetValue<string>(testData);
            }

            endTime = DateTime.Now.Ticks;
            long interpretedSpeed = endTime - startTime;
            interpretedTotal += interpretedSpeed;
            Log($"{interpretedSpeed}ticks ");
        }

        LogLn();

        Assert.Equal(interpretedResult, compiledResult);
        ReportPerformance("property reference (field)", interpretedTotal, compiledTotal);

        if (compiledTotal >= interpretedTotal)
        {
            throw new Exception("Compiled version is slower than interpreted!");
        }
    }

    [Fact(Skip = "Time sensitive test, sometimes fails on CI")]
    public void CompilingPropertyReferenceNestedField()
    {
        long interpretedTotal = 0;
        const long compiledTotal = 0;
        long startTime;
        long endTime;
        string interpretedResult = null;
        string compiledResult = null;
        var testData = new TestClass2();
        IExpression expression = Parser.ParseExpression("Foo.Bar.Boo");

        // warmup
        for (int i = 0; i < _count; i++)
        {
            interpretedResult = expression.GetValue<string>(testData);
        }

        Log("timing interpreted: ");

        for (int i = 0; i < _iterations; i++)
        {
            startTime = DateTime.Now.Ticks;

            for (int j = 0; j < _count; j++)
            {
                interpretedResult = expression.GetValue<string>(testData);
            }

            endTime = DateTime.Now.Ticks;
            long interpretedSpeed = endTime - startTime;
            interpretedTotal += interpretedSpeed;
            Log($"{interpretedSpeed}ticks ");
        }

        LogLn();

        Compile(expression);

        Log("timing compiled: ");
        expression.GetValue<string>(testData);

        for (int i = 0; i < _iterations; i++)
        {
            startTime = DateTime.Now.Ticks;

            for (int j = 0; j < _count; j++)
            {
                compiledResult = expression.GetValue<string>(testData);
            }

            endTime = DateTime.Now.Ticks;
            long interpretedSpeed = endTime - startTime;
            interpretedTotal += interpretedSpeed;
            Log($"{interpretedSpeed}ticks ");
        }

        LogLn();

        Assert.Equal(interpretedResult, compiledResult);
        ReportPerformance("property reference (nested field)", interpretedTotal, compiledTotal);

        if (compiledTotal >= interpretedTotal)
        {
            throw new Exception("Compiled version is slower than interpreted!");
        }
    }

    [Fact(Skip = "Time sensitive test, sometimes fails on CI")]
    public void CompilingPropertyReferenceNestedMixedFieldGetter()
    {
        long interpretedTotal = 0;
        const long compiledTotal = 0;
        long startTime;
        long endTime;
        string interpretedResult = null;
        string compiledResult = null;
        var testData = new TestClass2();
        IExpression expression = Parser.ParseExpression("Foo.Baz.Boo");

        // warmup
        for (int i = 0; i < _count; i++)
        {
            interpretedResult = expression.GetValue<string>(testData);
        }

        Log("timing interpreted: ");

        for (int i = 0; i < _iterations; i++)
        {
            startTime = DateTime.Now.Ticks;

            for (int j = 0; j < _count; j++)
            {
                interpretedResult = expression.GetValue<string>(testData);
            }

            endTime = DateTime.Now.Ticks;
            long interpretedSpeed = endTime - startTime;
            interpretedTotal += interpretedSpeed;
            Log($"{interpretedSpeed}ticks ");
        }

        LogLn();

        Compile(expression);

        Log("timing compiled: ");
        expression.GetValue<string>(testData);

        for (int i = 0; i < _iterations; i++)
        {
            startTime = DateTime.Now.Ticks;

            for (int j = 0; j < _count; j++)
            {
                compiledResult = expression.GetValue<string>(testData);
            }

            endTime = DateTime.Now.Ticks;
            long interpretedSpeed = endTime - startTime;
            interpretedTotal += interpretedSpeed;
            Log($"{interpretedSpeed}ticks ");
        }

        LogLn();

        Assert.Equal(interpretedResult, compiledResult);
        ReportPerformance("nested property reference (mixed field/getter)", interpretedTotal, compiledTotal);

        if (compiledTotal >= interpretedTotal)
        {
            throw new Exception("Compiled version is slower than interpreted!");
        }
    }

    [Fact(Skip = "Time sensitive test, sometimes fails on CI")]
    public void CompilingNestedMixedFieldPropertyReferenceMethodReference()
    {
        long interpretedTotal = 0;
        const long compiledTotal = 0;
        long startTime;
        long endTime;
        string interpretedResult = null;
        string compiledResult = null;
        var testData = new TestClass2();
        IExpression expression = Parser.ParseExpression("Foo.Bay().Boo");

        // warmup
        for (int i = 0; i < _count; i++)
        {
            interpretedResult = expression.GetValue<string>(testData);
        }

        Log("timing interpreted: ");

        for (int i = 0; i < _iterations; i++)
        {
            startTime = DateTime.Now.Ticks;

            for (int j = 0; j < _count; j++)
            {
                interpretedResult = expression.GetValue<string>(testData);
            }

            endTime = DateTime.Now.Ticks;
            long interpretedSpeed = endTime - startTime;
            interpretedTotal += interpretedSpeed;
            Log($"{interpretedSpeed}ticks ");
        }

        LogLn();

        Compile(expression);

        Log("timing compiled: ");
        expression.GetValue<string>(testData);

        for (int i = 0; i < _iterations; i++)
        {
            startTime = DateTime.Now.Ticks;

            for (int j = 0; j < _count; j++)
            {
                compiledResult = expression.GetValue<string>(testData);
            }

            endTime = DateTime.Now.Ticks;
            long interpretedSpeed = endTime - startTime;
            interpretedTotal += interpretedSpeed;
            Log($"{interpretedSpeed}ticks ");
        }

        LogLn();

        Assert.Equal(interpretedResult, compiledResult);
        ReportPerformance("nested reference(mixed field / method)", interpretedTotal, compiledTotal);

        if (compiledTotal >= interpretedTotal)
        {
            throw new Exception("Compiled version is slower than interpreted!");
        }
    }

    [Fact(Skip = "Time sensitive test, sometimes fails on CI")]
    public void CompilingPropertyReferenceGetter()
    {
        long interpretedTotal = 0;
        const long compiledTotal = 0;
        long startTime;
        long endTime;
        string interpretedResult = null;
        string compiledResult = null;
        var testData = new TestClass2();
        IExpression expression = Parser.ParseExpression("Name2");

        // warmup
        for (int i = 0; i < _count; i++)
        {
            interpretedResult = expression.GetValue<string>(testData);
        }

        Log("timing interpreted: ");

        for (int i = 0; i < _iterations; i++)
        {
            startTime = DateTime.Now.Ticks;

            for (int j = 0; j < _count; j++)
            {
                interpretedResult = expression.GetValue<string>(testData);
            }

            endTime = DateTime.Now.Ticks;
            long interpretedSpeed = endTime - startTime;
            interpretedTotal += interpretedSpeed;
            Log($"{interpretedSpeed}ticks ");
        }

        LogLn();

        Compile(expression);

        Log("timing compiled: ");
        expression.GetValue<string>(testData);

        for (int i = 0; i < _iterations; i++)
        {
            startTime = DateTime.Now.Ticks;

            for (int j = 0; j < _count; j++)
            {
                compiledResult = expression.GetValue<string>(testData);
            }

            endTime = DateTime.Now.Ticks;
            long interpretedSpeed = endTime - startTime;
            interpretedTotal += interpretedSpeed;
            Log($"{interpretedSpeed}ticks ");
        }

        LogLn();

        Assert.Equal(interpretedResult, compiledResult);
        ReportPerformance("property reference (getter)", interpretedTotal, compiledTotal);

        if (compiledTotal >= interpretedTotal)
        {
            throw new Exception("Compiled version is slower than interpreted!");
        }
    }

#pragma warning restore xUnit1004 // Test methods should not be skipped

    private void ReportPerformance(string title, long interpretedTotalTicks, long compiledTotalTicks)
    {
        double interpretedTotal = (double)interpretedTotalTicks / 10000;
        double compiledTotal = (double)compiledTotalTicks / 10000;
        double averageInterpreted = interpretedTotal / _iterations;
        double averageCompiled = compiledTotal / _iterations;
        double ratio = averageCompiled / averageInterpreted * 100.0d;

        LogLn(
            $">>{title}: average for {_count}: compiled={averageCompiled}ms interpreted={averageInterpreted}ms: compiled takes {(int)ratio}% of the interpreted time");

        if (averageCompiled > averageInterpreted)
        {
            throw new Exception($"Compiled version took longer than interpreted! CompiledSpeed=~{averageCompiled}ms InterpretedSpeed={averageInterpreted}ms");
        }

        LogLn();
    }

    private void Log(string message)
    {
        if (NoisyTests)
        {
            _output.WriteLine(message);
        }
    }

    private void LogLn(params string[] messages)
    {
        if (NoisyTests)
        {
            _output.WriteLine(messages.Length > 0 ? messages[0] : string.Empty);
        }
    }

    private void Compile(IExpression expression)
    {
        Assert.True(SpelCompiler.Compile(expression));
    }

    public class HW
    {
        public string Hello()
        {
            return "foobar";
        }
    }

    public class Payload
    {
        public Two[] DR { get; } =
        {
            new()
        };
    }

    public class Two
    {
        public Three DRFixedSection { get; } = new();
    }

    public class Three
    {
        public double Duration { get; set; } = 0.4d;
    }

    public class NumberHolder
    {
        public int Payload = 36;
    }

    public class Greeter
    {
        public string World => "world";
    }

    public class TestClass2
    {
        public string Name = "Santa";

        public string Name2 => "foobar";

        public Foo Foo => new();
    }

    public class Foo
    {
        public Bar Bar = new();

        public Bar Baz { get; } = new();

        public Bar Bay()
        {
            return Baz;
        }
    }

    public class Bar
    {
        public string Boo = "oranges";
    }
}
