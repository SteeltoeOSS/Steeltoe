// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal;
using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Xunit;
using Xunit.Abstractions;

// ReSharper disable InconsistentNaming

namespace Steeltoe.Common.Expression.Test.Spring;

public sealed class SpelCompilationPerformanceTests : AbstractExpressionTests
{
    private const bool NoisyTests = true;

    private const int Count = 50000; // number of evaluations that are timed in one run

    private const int Iterations = 10; // number of times to repeat 'count' evaluations (for averaging)

    private readonly ITestOutputHelper _output;

    private IExpression _expression;

    public SpelCompilationPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void CompilingMathematicalExpressionsWithDifferentOperandTypes()
    {
        var nh = new NumberHolder();
        _expression = Parser.ParseExpression("(T(Convert).ToDouble(Payload))/18D");
        object o = _expression.GetValue(nh);
        Assert.Equal(2d, o);
        _output.WriteLine("Performance check for SpEL expression: 'Convert.ToDouble(Payload)/18D'");

        long startTime = DateTime.UtcNow.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(nh);
        }

        _output.WriteLine("One million iterations: " + (DateTime.UtcNow.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.UtcNow.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(nh);
        }

        _output.WriteLine("One million iterations: " + (DateTime.UtcNow.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.UtcNow.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(nh);
        }

        _output.WriteLine("One million iterations: " + (DateTime.UtcNow.Ticks - startTime) / 10000 + "ms");

        Compile(_expression);
        _output.WriteLine("Now compiled:");
        o = _expression.GetValue(nh);
        Assert.Equal(2d, o);

        startTime = DateTime.UtcNow.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(nh);
        }

        _output.WriteLine("One million iterations: " + (DateTime.UtcNow.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.UtcNow.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(nh);
        }

        _output.WriteLine("One million iterations: " + (DateTime.UtcNow.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.UtcNow.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(nh);
        }

        _output.WriteLine("One million iterations: " + (DateTime.UtcNow.Ticks - startTime) / 10000 + "ms");

        _expression = Parser.ParseExpression("Payload/18D");
        o = _expression.GetValue(nh);
        Assert.Equal(2d, o);
        _output.WriteLine("Performance check for SpEL expression: 'Payload / 18D");

        startTime = DateTime.UtcNow.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(nh);
        }

        _output.WriteLine("One million iterations: " + (DateTime.UtcNow.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.UtcNow.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(nh);
        }

        _output.WriteLine("One million iterations: " + (DateTime.UtcNow.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.UtcNow.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(nh);
        }

        _output.WriteLine("One million iterations: " + (DateTime.UtcNow.Ticks - startTime) / 10000 + "ms");

        Compile(_expression);
        _output.WriteLine("Now compiled:");
        o = _expression.GetValue(nh);
        Assert.Equal(2d, o);

        startTime = DateTime.UtcNow.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(nh);
        }

        _output.WriteLine("One million iterations: " + (DateTime.UtcNow.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.UtcNow.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(nh);
        }

        _output.WriteLine("One million iterations: " + (DateTime.UtcNow.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.UtcNow.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(nh);
        }

        _output.WriteLine("One million iterations: " + (DateTime.UtcNow.Ticks - startTime) / 10000 + "ms");
    }

    [Fact]
    public void InlineLists()
    {
        _expression = Parser.ParseExpression("{'abcde','ijklm'}[0].Substring({1,3,4}[0],{1,3,4}[1])");
        object o = _expression.GetValue();
        Assert.Equal("bcd", o);
        _output.WriteLine("Performance check for SpEL expression: '{'abcde','ijklm'}[0].substring({1,3,4}[0],{1,3,4}[1])'");

        long startTime = DateTime.UtcNow.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue();
        }

        _output.WriteLine("One million iterations: " + (DateTime.UtcNow.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.UtcNow.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue();
        }

        _output.WriteLine("One million iterations: " + (DateTime.UtcNow.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.UtcNow.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue();
        }

        _output.WriteLine("One million iterations: " + (DateTime.UtcNow.Ticks - startTime) / 10000 + "ms");

        Compile(_expression);
        _output.WriteLine("Now compiled:");
        o = _expression.GetValue();
        Assert.Equal("bcd", o);

        startTime = DateTime.UtcNow.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue();
        }

        _output.WriteLine("One million iterations: " + (DateTime.UtcNow.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.UtcNow.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue();
        }

        _output.WriteLine("One million iterations: " + (DateTime.UtcNow.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.UtcNow.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue();
        }

        _output.WriteLine("One million iterations: " + (DateTime.UtcNow.Ticks - startTime) / 10000 + "ms");
    }

    [Fact]
    public void InlineNestedLists()
    {
        _expression = Parser.ParseExpression("{'abcde',{'ijklm','nopqr'}}[1][0].Substring({1,3,4}[0],{1,3,4}[1])");
        object o = _expression.GetValue();
        Assert.Equal("jkl", o);
        _output.WriteLine("Performance check for SpEL expression: '{'abcde',{'ijklm','nopqr'}}[1][0].Substring({1,3,4}[0],{1,3,4}[1])'");

        long startTime = DateTime.UtcNow.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue();
        }

        _output.WriteLine("One million iterations: " + (DateTime.UtcNow.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.UtcNow.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue();
        }

        _output.WriteLine("One million iterations: " + (DateTime.UtcNow.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.UtcNow.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue();
        }

        _output.WriteLine("One million iterations: " + (DateTime.UtcNow.Ticks - startTime) / 10000 + "ms");

        Compile(_expression);
        _output.WriteLine("Now compiled:");
        o = _expression.GetValue();
        Assert.Equal("jkl", o);

        startTime = DateTime.UtcNow.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue();
        }

        _output.WriteLine("One million iterations: " + (DateTime.UtcNow.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.UtcNow.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue();
        }

        _output.WriteLine("One million iterations: " + (DateTime.UtcNow.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.UtcNow.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue();
        }

        _output.WriteLine("One million iterations: " + (DateTime.UtcNow.Ticks - startTime) / 10000 + "ms");
    }

    [Fact]
    public void StringConcatenation()
    {
        var g = new Greeter();
        _expression = Parser.ParseExpression("'hello' + World + ' spring'");
        object o = _expression.GetValue(g);
        Assert.Equal("helloworld spring", o);
        _output.WriteLine("Performance check for SpEL expression: 'hello' + World + ' spring'");

        long startTime = DateTime.UtcNow.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(g);
        }

        _output.WriteLine("One million iterations: " + (DateTime.UtcNow.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.UtcNow.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(g);
        }

        _output.WriteLine("One million iterations: " + (DateTime.UtcNow.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.UtcNow.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(g);
        }

        _output.WriteLine("One million iterations: " + (DateTime.UtcNow.Ticks - startTime) / 10000 + "ms");

        Compile(_expression);
        _output.WriteLine("Now compiled:");
        o = _expression.GetValue(g);
        Assert.Equal("helloworld spring", o);

        startTime = DateTime.UtcNow.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(g);
        }

        _output.WriteLine("One million iterations: " + (DateTime.UtcNow.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.UtcNow.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(g);
        }

        _output.WriteLine("One million iterations: " + (DateTime.UtcNow.Ticks - startTime) / 10000 + "ms");

        startTime = DateTime.UtcNow.Ticks;

        for (int i = 0; i < 1_000_000; i++)
        {
            _expression.GetValue(g);
        }

        _output.WriteLine("One million iterations: " + (DateTime.UtcNow.Ticks - startTime) / 10000 + "ms");
    }

    [Fact]
    public void ComplexExpressionPerformance()
    {
        var payload = new Payload();
        IExpression expression = Parser.ParseExpression("DR[0].DRFixedSection.Duration lt 0.1");
        bool b = false;
        long iTotal = 0;
        const long cTotal = 0;

        // warmup
        for (int i = 0; i < Count; i++)
        {
            b = expression.GetValue<bool>(payload);
        }

        // Verify the result
        Assert.False(b);

        Log("timing interpreted: ");

        for (int i = 0; i < Iterations; i++)
        {
            long startTime = DateTime.UtcNow.Ticks;

            for (int j = 0; j < Count; j++)
            {
                b = expression.GetValue<bool>(payload);
            }

            long endTime = DateTime.UtcNow.Ticks;
            long interpretedSpeed = endTime - startTime;
            iTotal += interpretedSpeed;
            Log($"{interpretedSpeed}ticks ");
        }

        LogLn();

        Compile(expression);
        bool bc = false;
        expression.GetValue<bool>(payload);
        Log("timing compiled: ");

        for (int i = 0; i < Iterations; i++)
        {
            long startTime = DateTime.UtcNow.Ticks;

            for (int j = 0; j < Count; j++)
            {
                bc = expression.GetValue<bool>(payload);
            }

            long endTime = DateTime.UtcNow.Ticks;
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

    [Fact]
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
        for (int i = 0; i < Count; i++)
        {
            interpretedResult = expression.GetValue<string>(testData);
        }

        Log("timing interpreted: ");

        for (int i = 0; i < Iterations; i++)
        {
            startTime = DateTime.UtcNow.Ticks;

            for (int j = 0; j < Count; j++)
            {
                interpretedResult = expression.GetValue<string>(testData);
            }

            endTime = DateTime.UtcNow.Ticks;
            long interpretedSpeed = endTime - startTime;
            interpretedTotal += interpretedSpeed;
            Log($"{interpretedSpeed}ticks ");
        }

        LogLn();

        Compile(expression);

        Log("timing compiled: ");
        expression.GetValue<string>(testData);

        for (int i = 0; i < Iterations; i++)
        {
            startTime = DateTime.UtcNow.Ticks;

            for (int j = 0; j < Count; j++)
            {
                compiledResult = expression.GetValue<string>(testData);
            }

            endTime = DateTime.UtcNow.Ticks;
            long interpretedSpeed = endTime - startTime;
            interpretedTotal += interpretedSpeed;
            Log($"{interpretedSpeed}ticks ");
        }

        LogLn();

        Assert.Equal(interpretedResult, compiledResult);
        ReportPerformance("method reference", interpretedTotal, compiledTotal);

        if (interpretedTotal <= compiledTotal)
        {
            throw new Exception("Compiled version is slower than interpreted!");
        }
    }

    [Fact]
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
        for (int i = 0; i < Count; i++)
        {
            interpretedResult = expression.GetValue<string>(testData);
        }

        Log("timing interpreted: ");

        for (int i = 0; i < Iterations; i++)
        {
            startTime = DateTime.UtcNow.Ticks;

            for (int j = 0; j < Count; j++)
            {
                interpretedResult = expression.GetValue<string>(testData);
            }

            endTime = DateTime.UtcNow.Ticks;
            long interpretedSpeed = endTime - startTime;
            interpretedTotal += interpretedSpeed;
            Log($"{interpretedSpeed}ticks ");
        }

        LogLn();

        Compile(expression);

        Log("timing compiled: ");
        expression.GetValue<string>(testData);

        for (int i = 0; i < Iterations; i++)
        {
            startTime = DateTime.UtcNow.Ticks;

            for (int j = 0; j < Count; j++)
            {
                compiledResult = expression.GetValue<string>(testData);
            }

            endTime = DateTime.UtcNow.Ticks;
            long interpretedSpeed = endTime - startTime;
            interpretedTotal += interpretedSpeed;
            Log($"{interpretedSpeed}ticks ");
        }

        LogLn();

        Assert.Equal(interpretedResult, compiledResult);
        ReportPerformance("property reference (field)", interpretedTotal, compiledTotal);

        if (interpretedTotal <= compiledTotal)
        {
            throw new Exception("Compiled version is slower than interpreted!");
        }
    }

    [Fact]
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
        for (int i = 0; i < Count; i++)
        {
            interpretedResult = expression.GetValue<string>(testData);
        }

        Log("timing interpreted: ");

        for (int i = 0; i < Iterations; i++)
        {
            startTime = DateTime.UtcNow.Ticks;

            for (int j = 0; j < Count; j++)
            {
                interpretedResult = expression.GetValue<string>(testData);
            }

            endTime = DateTime.UtcNow.Ticks;
            long interpretedSpeed = endTime - startTime;
            interpretedTotal += interpretedSpeed;
            Log($"{interpretedSpeed}ticks ");
        }

        LogLn();

        Compile(expression);

        Log("timing compiled: ");
        expression.GetValue<string>(testData);

        for (int i = 0; i < Iterations; i++)
        {
            startTime = DateTime.UtcNow.Ticks;

            for (int j = 0; j < Count; j++)
            {
                compiledResult = expression.GetValue<string>(testData);
            }

            endTime = DateTime.UtcNow.Ticks;
            long interpretedSpeed = endTime - startTime;
            interpretedTotal += interpretedSpeed;
            Log($"{interpretedSpeed}ticks ");
        }

        LogLn();

        Assert.Equal(interpretedResult, compiledResult);
        ReportPerformance("property reference (nested field)", interpretedTotal, compiledTotal);

        if (interpretedTotal <= compiledTotal)
        {
            throw new Exception("Compiled version is slower than interpreted!");
        }
    }

    [Fact]
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
        for (int i = 0; i < Count; i++)
        {
            interpretedResult = expression.GetValue<string>(testData);
        }

        Log("timing interpreted: ");

        for (int i = 0; i < Iterations; i++)
        {
            startTime = DateTime.UtcNow.Ticks;

            for (int j = 0; j < Count; j++)
            {
                interpretedResult = expression.GetValue<string>(testData);
            }

            endTime = DateTime.UtcNow.Ticks;
            long interpretedSpeed = endTime - startTime;
            interpretedTotal += interpretedSpeed;
            Log($"{interpretedSpeed}ticks ");
        }

        LogLn();

        Compile(expression);

        Log("timing compiled: ");
        expression.GetValue<string>(testData);

        for (int i = 0; i < Iterations; i++)
        {
            startTime = DateTime.UtcNow.Ticks;

            for (int j = 0; j < Count; j++)
            {
                compiledResult = expression.GetValue<string>(testData);
            }

            endTime = DateTime.UtcNow.Ticks;
            long interpretedSpeed = endTime - startTime;
            interpretedTotal += interpretedSpeed;
            Log($"{interpretedSpeed}ticks ");
        }

        LogLn();

        Assert.Equal(interpretedResult, compiledResult);
        ReportPerformance("nested property reference (mixed field/getter)", interpretedTotal, compiledTotal);

        if (interpretedTotal <= compiledTotal)
        {
            throw new Exception("Compiled version is slower than interpreted!");
        }
    }

    [Fact]
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
        for (int i = 0; i < Count; i++)
        {
            interpretedResult = expression.GetValue<string>(testData);
        }

        Log("timing interpreted: ");

        for (int i = 0; i < Iterations; i++)
        {
            startTime = DateTime.UtcNow.Ticks;

            for (int j = 0; j < Count; j++)
            {
                interpretedResult = expression.GetValue<string>(testData);
            }

            endTime = DateTime.UtcNow.Ticks;
            long interpretedSpeed = endTime - startTime;
            interpretedTotal += interpretedSpeed;
            Log($"{interpretedSpeed}ticks ");
        }

        LogLn();

        Compile(expression);

        Log("timing compiled: ");
        expression.GetValue<string>(testData);

        for (int i = 0; i < Iterations; i++)
        {
            startTime = DateTime.UtcNow.Ticks;

            for (int j = 0; j < Count; j++)
            {
                compiledResult = expression.GetValue<string>(testData);
            }

            endTime = DateTime.UtcNow.Ticks;
            long interpretedSpeed = endTime - startTime;
            interpretedTotal += interpretedSpeed;
            Log($"{interpretedSpeed}ticks ");
        }

        LogLn();

        Assert.Equal(interpretedResult, compiledResult);
        ReportPerformance("nested reference(mixed field / method)", interpretedTotal, compiledTotal);

        if (interpretedTotal <= compiledTotal)
        {
            throw new Exception("Compiled version is slower than interpreted!");
        }
    }

    [Fact]
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
        for (int i = 0; i < Count; i++)
        {
            interpretedResult = expression.GetValue<string>(testData);
        }

        Log("timing interpreted: ");

        for (int i = 0; i < Iterations; i++)
        {
            startTime = DateTime.UtcNow.Ticks;

            for (int j = 0; j < Count; j++)
            {
                interpretedResult = expression.GetValue<string>(testData);
            }

            endTime = DateTime.UtcNow.Ticks;
            long interpretedSpeed = endTime - startTime;
            interpretedTotal += interpretedSpeed;
            Log($"{interpretedSpeed}ticks ");
        }

        LogLn();

        Compile(expression);

        Log("timing compiled: ");
        expression.GetValue<string>(testData);

        for (int i = 0; i < Iterations; i++)
        {
            startTime = DateTime.UtcNow.Ticks;

            for (int j = 0; j < Count; j++)
            {
                compiledResult = expression.GetValue<string>(testData);
            }

            endTime = DateTime.UtcNow.Ticks;
            long interpretedSpeed = endTime - startTime;
            interpretedTotal += interpretedSpeed;
            Log($"{interpretedSpeed}ticks ");
        }

        LogLn();

        Assert.Equal(interpretedResult, compiledResult);
        ReportPerformance("property reference (getter)", interpretedTotal, compiledTotal);

        if (interpretedTotal <= compiledTotal)
        {
            throw new Exception("Compiled version is slower than interpreted!");
        }
    }

    private void ReportPerformance(string title, long interpretedTotalTicks, long compiledTotalTicks)
    {
        double interpretedTotal = (double)interpretedTotalTicks / 10000;
        double compiledTotal = (double)compiledTotalTicks / 10000;
        double averageInterpreted = interpretedTotal / Iterations;
        double averageCompiled = compiledTotal / Iterations;
        double ratio = averageCompiled / averageInterpreted * 100.0d;

        LogLn(
            $">>{title}: average for {Count}: compiled={averageCompiled}ms interpreted={averageInterpreted}ms: compiled takes {(int)ratio}% of the interpreted time");

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

    public sealed class HW
    {
        public string Hello()
        {
            return "foobar";
        }
    }

    public sealed class Payload
    {
        public Two[] DR { get; } =
        {
            new()
        };
    }

    public sealed class Two
    {
        public Three DRFixedSection { get; } = new();
    }

    public sealed class Three
    {
        public double Duration { get; set; } = 0.4d;
    }

    public sealed class NumberHolder
    {
        public int Payload { get; } = 36;
    }

    public sealed class Greeter
    {
        public string World => "world";
    }

    public sealed class TestClass2
    {
        public string Name { get; } = "Santa";

        public string Name2 => "foobar";

        public Foo Foo => new();
    }

    public sealed class Foo
    {
        public Bar Bar { get; } = new();

        public Bar Baz { get; } = new();

        public Bar Bay()
        {
            return Baz;
        }
    }

    public sealed class Bar
    {
        public string Boo { get; } = "oranges";
    }
}
