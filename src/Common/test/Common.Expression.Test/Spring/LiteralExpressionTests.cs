// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Common;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Spring
{
    public class LiteralExpressionTests
    {
        [Fact]
        public void TestGetValue()
        {
            var lEx = new LiteralExpression("somevalue");
            Assert.Equal("somevalue", lEx.GetValue());
            Assert.Equal("somevalue", lEx.GetValue(typeof(string)));
            var ctx = new StandardEvaluationContext();
            Assert.Equal("somevalue", lEx.GetValue(ctx));
            Assert.Equal("somevalue", lEx.GetValue(ctx, typeof(string)));
            Assert.Equal("somevalue", lEx.GetValue(new Rooty()));
            Assert.Equal("somevalue", lEx.GetValue(new Rooty(), typeof(string)));
            Assert.Equal("somevalue", lEx.GetValue(ctx, new Rooty()));
            Assert.Equal("somevalue", lEx.GetValue(ctx, new Rooty(), typeof(string)));
            Assert.Equal("somevalue", lEx.ExpressionString);
            Assert.False(lEx.IsWritable(new StandardEvaluationContext()));
            Assert.False(lEx.IsWritable(new Rooty()));
            Assert.False(lEx.IsWritable(new StandardEvaluationContext(), new Rooty()));
        }

        [Fact]
        public void TestSetValue()
        {
            var ex = Assert.Throws<EvaluationException>(() => new LiteralExpression("somevalue").SetValue(new StandardEvaluationContext(), "flibble"));
            Assert.Equal("somevalue", ex.ExpressionString);
            ex = Assert.Throws<EvaluationException>(() => new LiteralExpression("somevalue").SetValue(new Rooty(), "flibble"));
            Assert.Equal("somevalue", ex.ExpressionString);
            ex = Assert.Throws<EvaluationException>(() => new LiteralExpression("somevalue").SetValue(new StandardEvaluationContext(), new Rooty(), "flibble"));
            Assert.Equal("somevalue", ex.ExpressionString);
        }

        [Fact]
        public void TestGetValueType()
        {
            var lEx = new LiteralExpression("somevalue");
            Assert.Equal(typeof(string), lEx.GetValueType());
            Assert.Equal(typeof(string), lEx.GetValueType(new StandardEvaluationContext()));
            Assert.Equal(typeof(string), lEx.GetValueType(new Rooty()));
            Assert.Equal(typeof(string), lEx.GetValueType(new StandardEvaluationContext(), new Rooty()));
        }

        public class Rooty
        {
        }
    }
}
