// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Spring.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Steeltoe.Common.Expression.Spring.Standard
{
    public class PropertiesConversionSpelTests
    {
        private static readonly SpelExpressionParser _parser = new SpelExpressionParser();

        [Fact]
        public void Props()
        {
            var props = new Dictionary<string, string>();
            props.Add("x", "1");
            props.Add("y", "2");
            props.Add("z", "3");
            var expression = _parser.ParseExpression("Foo(#props)");
            var context = new StandardEvaluationContext();
            context.SetVariable("props", props);
            var result = expression.GetValue<string>(context, new TestBean());
            Assert.Equal("123", result);
        }

        [Fact]
        public void MapWithAllStringValues()
        {
            var map = new Dictionary<string, object>();
            map.Add("x", "1");
            map.Add("y", "2");
            map.Add("z", "3");
            var expression = _parser.ParseExpression("Foo(#props)");
            var context = new StandardEvaluationContext();
            context.SetVariable("props", map);
            var result = expression.GetValue<string>(context, new TestBean());
            Assert.Equal("123", result);
        }

        [Fact]
        public void MapWithNonStringValue()
        {
            var map = new Dictionary<string, object>();
            map.Add("x", "1");
            map.Add("y", 2);
            map.Add("z", "3");
            map.Add("a", Guid.NewGuid());
            var expression = _parser.ParseExpression("Foo(#props)");
            var context = new StandardEvaluationContext();
            context.SetVariable("props", map);
            var result = expression.GetValue<string>(context, new TestBean());
            Assert.Equal("123", result);
        }

        [Fact]
        public void CustomMapWithNonStringValue()
        {
            CustomMap map = new CustomMap();
            map.Add("x", "1");
            map.Add("y", 2);
            map.Add("z", "3");
            var expression = _parser.ParseExpression("Foo(#props)");
            var context = new StandardEvaluationContext();
            context.SetVariable("props", map);
            var result = expression.GetValue<string>(context, new TestBean());
            Assert.Equal("123", result);
        }

        public class TestBean
        {
            public string Foo(IDictionary props)
            {
                return props["x"]?.ToString() + props["y"]?.ToString() + props["z"]?.ToString();
            }
        }

        public class CustomMap : Dictionary<string, object>
        {
        }
    }
}
