// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Spring
{
    public class SpelExceptionTests
    {
        [Fact]
        public void SpelExpressionMapNullVariables()
        {
            var parser = new SpelExpressionParser();
            var spelExpression = parser.ParseExpression("#aMap.containsKey('one')");
            Assert.Throws<SpelEvaluationException>(() => spelExpression.GetValue());
        }

        [Fact]
        public void SpelExpressionMapIndexAccessNullVariables()
        {
            var parser = new SpelExpressionParser();
            var spelExpression = parser.ParseExpression("#aMap['one'] eq 1");
            Assert.Throws<SpelEvaluationException>(() => spelExpression.GetValue());
        }

        [Fact]

        public void SpelExpressionMapWithVariables()
        {
            var parser = new SpelExpressionParser();
            var spelExpression = parser.ParseExpression("#aMap['one'] eq 1");
            var ctx = new StandardEvaluationContext();
            var hmap = new Dictionary<string, object>()
            {
                { "aMap",  new Dictionary<string, int>() { { "one", 1 }, { "two", 2 }, { "three", 3 } } }
            };
            ctx.SetVariables(hmap);

            var result = spelExpression.GetValue<bool>(ctx);
            Assert.True(result);
        }

        [Fact]
        public void SpelExpressionListNullVariables()
        {
            var parser = new SpelExpressionParser();
            var spelExpression = parser.ParseExpression("#aList.contains('one')");
            Assert.Throws<SpelEvaluationException>(() => spelExpression.GetValue());
        }

        [Fact]
        public void SpelExpressionListIndexAccessNullVariables()
        {
            var parser = new SpelExpressionParser();
            var spelExpression = parser.ParseExpression("#aList[0] eq 'one'");
            Assert.Throws<SpelEvaluationException>(() => spelExpression.GetValue());
        }

        [Fact]

        public void SpelExpressionListWithVariables()
        {
            var parser = new SpelExpressionParser();
            var spelExpression = parser.ParseExpression("#aList.Contains('one')");
            var ctx = new StandardEvaluationContext();
            var hmap = new Dictionary<string, object>()
            {
                { "aList",  new List<string>() { "one", "two", "three" } }
            };
            ctx.SetVariables(hmap);
            var result = spelExpression.GetValue<bool>(ctx);
            Assert.True(result);
        }

        [Fact]

        public void SpelExpressionListIndexAccessWithVariables()
        {
            var parser = new SpelExpressionParser();
            var spelExpression = parser.ParseExpression("#aList[0] eq 'one'");
            var ctx = new StandardEvaluationContext();
            var hmap = new Dictionary<string, object>()
            {
                { "aList",  new List<string>() { "one", "two", "three" } }
            };
            ctx.SetVariables(hmap);
            var result = spelExpression.GetValue<bool>(ctx);
            Assert.True(result);
        }

        [Fact]
        public void SpelExpressionArrayIndexAccessNullVariables()
        {
            var parser = new SpelExpressionParser();
            var spelExpression = parser.ParseExpression("#anArray[0] eq 1");
            Assert.Throws<SpelEvaluationException>(() => spelExpression.GetValue());
        }

        [Fact]

        public void SpelExpressionArrayWithVariables()
        {
            var parser = new SpelExpressionParser();
            var spelExpression = parser.ParseExpression("#anArray[0] eq 1");
            var ctx = new StandardEvaluationContext();
            var hmap = new Dictionary<string, object>()
            {
                { "anArray",  new int[] { 1, 2, 3 } }
            };
            ctx.SetVariables(hmap);

            var result = spelExpression.GetValue<bool>(ctx);
            Assert.True(result);
        }
    }
}
