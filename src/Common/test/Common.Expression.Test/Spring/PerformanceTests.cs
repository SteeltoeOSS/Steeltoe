// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Spring.Standard;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Common.Expression.Spring
{
    public class PerformanceTests
    {
        public static readonly int ITERATIONS = 10000;
        public static readonly bool Report = true;
        private static readonly bool DEBUG = false;
        private static IExpressionParser parser = new SpelExpressionParser();
        private static IEvaluationContext eContext = TestScenarioCreator.GetTestEvaluationContext();
        private ITestOutputHelper _output;

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
            for (var i = 0; i < ITERATIONS; i++)
            {
                expr = parser.ParseExpression("PlaceOfBirth.City");
                Assert.NotNull(expr);
                expr.GetValue(eContext);
            }

            starttime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            for (var i = 0; i < ITERATIONS; i++)
            {
                expr = parser.ParseExpression("PlaceOfBirth.City");
                Assert.NotNull(expr);
                expr.GetValue(eContext);
            }

            endtime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var freshParseTime = endtime - starttime;
            if (DEBUG)
            {
                _output.WriteLine("PropertyAccess: Time for parsing and evaluation x 10000: " + freshParseTime + "ms");
            }

            expr = parser.ParseExpression("PlaceOfBirth.City");
            Assert.NotNull(expr);
            starttime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            for (var i = 0; i < ITERATIONS; i++)
            {
                expr.GetValue(eContext);
            }

            endtime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var reuseTime = endtime - starttime;
            if (DEBUG)
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
            for (var i = 0; i < ITERATIONS; i++)
            {
                expr = parser.ParseExpression("get_PlaceOfBirth().get_City()");
                Assert.NotNull(expr);
                expr.GetValue(eContext);
            }

            starttime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            for (var i = 0; i < ITERATIONS; i++)
            {
                expr = parser.ParseExpression("get_PlaceOfBirth().get_City()");
                Assert.NotNull(expr);
                expr.GetValue(eContext);
            }

            endtime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var freshParseTime = endtime - starttime;
            if (DEBUG)
            {
                _output.WriteLine("MethodExpression: Time for parsing and evaluation x 10000: " + freshParseTime + "ms");
            }

            expr = parser.ParseExpression("get_PlaceOfBirth().get_City()");
            Assert.NotNull(expr);
            starttime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            for (var i = 0; i < ITERATIONS; i++)
            {
                expr.GetValue(eContext);
            }

            endtime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var reuseTime = endtime - starttime;
            if (DEBUG)
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
}
