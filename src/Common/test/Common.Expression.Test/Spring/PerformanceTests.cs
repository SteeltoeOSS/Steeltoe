// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Standard;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Common.Expression.Internal.Spring;
#pragma warning disable xUnit1004 // Test methods should not be skipped
public class PerformanceTests
{
    private static readonly int _iterations = 10000;
    private static readonly bool _debug = bool.Parse(bool.FalseString);
    private static readonly IExpressionParser _parser = new SpelExpressionParser();
    private static readonly IEvaluationContext _eContext = TestScenarioCreator.GetTestEvaluationContext();
    private readonly ITestOutputHelper _output;

    public PerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(Skip = "Time sensitive test, sometimes fails on CI")]
    public void TestPerformanceOfPropertyAccess()
    {
        long starttime = 0;
        long endtime = 0;
        IExpression expr;

        // warmup
        for (var i = 0; i < _iterations; i++)
        {
            expr = _parser.ParseExpression("PlaceOfBirth.City");
            Assert.NotNull(expr);
            expr.GetValue(_eContext);
        }

        starttime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        for (var i = 0; i < _iterations; i++)
        {
            expr = _parser.ParseExpression("PlaceOfBirth.City");
            Assert.NotNull(expr);
            expr.GetValue(_eContext);
        }

        endtime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var freshParseTime = endtime - starttime;
        if (_debug)
        {
            _output.WriteLine("PropertyAccess: Time for parsing and evaluation x 10000: " + freshParseTime + "ms");
        }

        expr = _parser.ParseExpression("PlaceOfBirth.City");
        Assert.NotNull(expr);
        starttime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        for (var i = 0; i < _iterations; i++)
        {
            expr.GetValue(_eContext);
        }

        endtime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var reuseTime = endtime - starttime;
        if (_debug)
        {
            _output.WriteLine("PropertyAccess: Time for just evaluation x 10000: " + reuseTime + "ms");
        }

        if (reuseTime > freshParseTime)
        {
            _output.WriteLine("Fresh parse every time, ITERATIONS iterations = " + freshParseTime + "ms");
            _output.WriteLine("Reuse SpelExpression, ITERATIONS iterations = " + reuseTime + "ms");
            throw new Exception("Should have been quicker to reuse!");
        }
    }

    [Fact(Skip = "Time sensitive test, sometimes fails on CI")]
    public void TestPerformanceOfMethodAccess()
    {
        long starttime = 0;
        long endtime = 0;
        IExpression expr;

        // warmup
        for (var i = 0; i < _iterations; i++)
        {
            expr = _parser.ParseExpression("get_PlaceOfBirth().get_City()");
            Assert.NotNull(expr);
            expr.GetValue(_eContext);
        }

        starttime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        for (var i = 0; i < _iterations; i++)
        {
            expr = _parser.ParseExpression("get_PlaceOfBirth().get_City()");
            Assert.NotNull(expr);
            expr.GetValue(_eContext);
        }

        endtime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var freshParseTime = endtime - starttime;
        if (_debug)
        {
            _output.WriteLine("MethodExpression: Time for parsing and evaluation x 10000: " + freshParseTime + "ms");
        }

        expr = _parser.ParseExpression("get_PlaceOfBirth().get_City()");
        Assert.NotNull(expr);
        starttime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        for (var i = 0; i < _iterations; i++)
        {
            expr.GetValue(_eContext);
        }

        endtime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var reuseTime = endtime - starttime;
        if (_debug)
        {
            _output.WriteLine("MethodExpression: Time for just evaluation x 10000: " + reuseTime + "ms");
        }

        if (reuseTime > freshParseTime)
        {
            _output.WriteLine("Fresh parse every time, ITERATIONS iterations = " + freshParseTime + "ms");
            _output.WriteLine("Reuse SpelExpression, ITERATIONS iterations = " + reuseTime + "ms");
            throw new Exception("Should have been quicker to reuse!");
        }
    }
}
#pragma warning restore xUnit1004 // Test methods should not be skipped
