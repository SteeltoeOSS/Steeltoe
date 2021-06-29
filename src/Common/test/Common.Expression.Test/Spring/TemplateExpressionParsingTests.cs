// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Common;
using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Spring
{
    public class TemplateExpressionParsingTests : AbstractExpressionTests
    {
        public static readonly IParserContext DEFAULT_TEMPLATE_PARSER_CONTEXT = new DefaultTemplateParserContext();

        public static readonly IParserContext HASH_DELIMITED_PARSER_CONTEXT = new HashDelimitedParserContext();

        [Fact]
        public void TestParsingSimpleTemplateExpression01()
        {
            var parser = new SpelExpressionParser();
            var expr = parser.ParseExpression("hello ${'world'}", DEFAULT_TEMPLATE_PARSER_CONTEXT);
            var o = expr.GetValue();
            Assert.Equal("hello world", o.ToString());
        }

        [Fact]
        public void TestParsingSimpleTemplateExpression02()
        {
            var parser = new SpelExpressionParser();
            var expr = parser.ParseExpression("hello ${'to'} you", DEFAULT_TEMPLATE_PARSER_CONTEXT);
            var o = expr.GetValue();
            Assert.Equal("hello to you", o.ToString());
        }

        [Fact]
        public void TestParsingSimpleTemplateExpression03()
        {
            var parser = new SpelExpressionParser();
            var expr = parser.ParseExpression("The quick ${'brown'} fox jumped over the ${'lazy'} dog", DEFAULT_TEMPLATE_PARSER_CONTEXT);
            var o = expr.GetValue();
            Assert.Equal("The quick brown fox jumped over the lazy dog", o.ToString());
        }

        [Fact]
        public void TestParsingSimpleTemplateExpression04()
        {
            var parser = new SpelExpressionParser();
            var expr = parser.ParseExpression("${'hello'} world", DEFAULT_TEMPLATE_PARSER_CONTEXT);
            var o = expr.GetValue();
            Assert.Equal("hello world", o.ToString());

            expr = parser.ParseExpression(string.Empty, DEFAULT_TEMPLATE_PARSER_CONTEXT);
            o = expr.GetValue();
            Assert.Equal(string.Empty, o.ToString());

            expr = parser.ParseExpression("abc", DEFAULT_TEMPLATE_PARSER_CONTEXT);
            o = expr.GetValue();
            Assert.Equal("abc", o.ToString());

            expr = parser.ParseExpression("abc", DEFAULT_TEMPLATE_PARSER_CONTEXT);
            o = expr.GetValue((object)null);
            Assert.Equal("abc", o.ToString());
        }

        [Fact]
        public void TestCompositeStringExpression()
        {
            var parser = new SpelExpressionParser();
            var ex = parser.ParseExpression("hello ${'world'}", DEFAULT_TEMPLATE_PARSER_CONTEXT);
            Assert.Equal("hello world", ex.GetValue());
            Assert.Equal("hello world", ex.GetValue(typeof(string)));
            Assert.Equal("hello world", ex.GetValue((object)null, typeof(string)));
            Assert.Equal("hello world", ex.GetValue(new Rooty()));
            Assert.Equal("hello world", ex.GetValue(new Rooty(), typeof(string)));

            var ctx = new StandardEvaluationContext();
            Assert.Equal("hello world", ex.GetValue(ctx));
            Assert.Equal("hello world", ex.GetValue(ctx, typeof(string)));
            Assert.Equal("hello world", ex.GetValue(ctx, null, typeof(string)));
            Assert.Equal("hello world", ex.GetValue(ctx, new Rooty()));
            Assert.Equal("hello world", ex.GetValue(ctx, new Rooty(), typeof(string)));
            Assert.Equal("hello world", ex.GetValue(ctx, new Rooty(), typeof(string)));
            Assert.Equal("hello ${'world'}", ex.ExpressionString);
            Assert.False(ex.IsWritable(new StandardEvaluationContext()));
            Assert.False(ex.IsWritable(new Rooty()));
            Assert.False(ex.IsWritable(new StandardEvaluationContext(), new Rooty()));

            Assert.Equal(typeof(string), ex.GetValueType());
            Assert.Equal(typeof(string), ex.GetValueType(ctx));
            Assert.Equal(typeof(string), ex.GetValueType(new Rooty()));
            Assert.Equal(typeof(string), ex.GetValueType(ctx, new Rooty()));
            Assert.Throws<EvaluationException>(() => ex.SetValue(ctx, null));
            Assert.Throws<EvaluationException>(() => ex.SetValue((object)null, null));
            Assert.Throws<EvaluationException>(() => ex.SetValue(ctx, null, null));
        }

        [Fact]
        public void TestNestedExpressions()
        {
            var parser = new SpelExpressionParser();

            // treat the nested ${..} as a part of the expression
            var ex = parser.ParseExpression("hello ${ListOfNumbersUpToTen.$[#this<5]} world", DEFAULT_TEMPLATE_PARSER_CONTEXT);
            var s = ex.GetValue(TestScenarioCreator.GetTestEvaluationContext(), typeof(string));
            Assert.Equal("hello 4 world", s);

            // not a useful expression but Tests nested expression syntax that clashes with template prefix/suffix
            ex = parser.ParseExpression("hello ${ListOfNumbersUpToTen.$[#root.ListOfNumbersUpToTen.$[#this%2==1]==3]} world", DEFAULT_TEMPLATE_PARSER_CONTEXT);
            Assert.IsType<CompositeStringExpression>(ex);
            var cse = (CompositeStringExpression)ex;
            var exprs = cse.Expressions;
            Assert.Equal(3, exprs.Count);
            Assert.Equal("ListOfNumbersUpToTen.$[#root.ListOfNumbersUpToTen.$[#this%2==1]==3]", exprs[1].ExpressionString);
            s = ex.GetValue(TestScenarioCreator.GetTestEvaluationContext(), typeof(string));
            Assert.Equal("hello  world", s);

            ex = parser.ParseExpression("hello ${ListOfNumbersUpToTen.$[#this<5]} ${ListOfNumbersUpToTen.$[#this>5]} world", DEFAULT_TEMPLATE_PARSER_CONTEXT);
            s = ex.GetValue(TestScenarioCreator.GetTestEvaluationContext(), typeof(string));
            Assert.Equal("hello 4 10 world", s);

            var pex = Assert.Throws<ParseException>(() => parser.ParseExpression("hello ${ListOfNumbersUpToTen.$[#this<5]} ${ListOfNumbersUpToTen.$[#this>5] world", DEFAULT_TEMPLATE_PARSER_CONTEXT));
            Assert.Equal("No ending suffix '}' for expression starting at character 41: ${ListOfNumbersUpToTen.$[#this>5] world", pex.SimpleMessage);

            pex = Assert.Throws<ParseException>(() => parser.ParseExpression("hello ${ListOfNumbersUpToTen.$[#root.ListOfNumbersUpToTen.$[#this%2==1==3]} world", DEFAULT_TEMPLATE_PARSER_CONTEXT));
            Assert.Equal("Found closing '}' at position 74 but most recent opening is '[' at position 30", pex.SimpleMessage);
        }

        [Fact]

        public void TestClashingWithSuffixes()
        {
            // Just wanting to use the prefix or suffix within the template:
            var ex = _parser.ParseExpression("hello ${3+4} world", DEFAULT_TEMPLATE_PARSER_CONTEXT);
            var s = ex.GetValue(TestScenarioCreator.GetTestEvaluationContext(), typeof(string));
            Assert.Equal("hello 7 world", s);

            ex = _parser.ParseExpression("hello ${3+4} wo${'${'}rld", DEFAULT_TEMPLATE_PARSER_CONTEXT);
            s = ex.GetValue(TestScenarioCreator.GetTestEvaluationContext(), typeof(string));
            Assert.Equal("hello 7 wo${rld", s);

            ex = _parser.ParseExpression("hello ${3+4} wo}rld", DEFAULT_TEMPLATE_PARSER_CONTEXT);
            s = ex.GetValue(TestScenarioCreator.GetTestEvaluationContext(), typeof(string));
            Assert.Equal("hello 7 wo}rld", s);
        }

        [Fact]
        public void TestParsingNormalExpressionThroughTemplateParser()
        {
            var expr = _parser.ParseExpression("1+2+3");
            Assert.Equal(6, expr.GetValue());
        }

        [Fact]
        public void TestErrorCases()
        {
            var pex = Assert.Throws<ParseException>(() => _parser.ParseExpression("hello ${'world'", DEFAULT_TEMPLATE_PARSER_CONTEXT));

            Assert.Equal("No ending suffix '}' for expression starting at character 6: ${'world'", pex.SimpleMessage);
            Assert.Equal("hello ${'world'", pex.ExpressionString);

            pex = Assert.Throws<ParseException>(() => _parser.ParseExpression("hello ${'wibble'${'world'}", DEFAULT_TEMPLATE_PARSER_CONTEXT));
            Assert.Equal("No ending suffix '}' for expression starting at character 6: ${'wibble'${'world'}", pex.SimpleMessage);
            pex = Assert.Throws<ParseException>(() => _parser.ParseExpression("hello ${} world", DEFAULT_TEMPLATE_PARSER_CONTEXT));
            Assert.Equal("No expression defined within delimiter '${}' at character 6", pex.SimpleMessage);
        }

        [Fact]
        public void TestTemplateParserContext()
        {
            var tpc = new TemplateParserContext("abc", "def");
            Assert.Equal("abc", tpc.ExpressionPrefix);
            Assert.Equal("def", tpc.ExpressionSuffix);
            Assert.True(tpc.IsTemplate);

            tpc = new TemplateParserContext();
            Assert.Equal("#{", tpc.ExpressionPrefix);
            Assert.Equal("}", tpc.ExpressionSuffix);
            Assert.True(tpc.IsTemplate);
        }

        public class DefaultTemplateParserContext : IParserContext
        {
            public bool IsTemplate => true;

            public string ExpressionPrefix => "${";

            public string ExpressionSuffix => "}";
        }

        public class HashDelimitedParserContext : IParserContext
        {
            public bool IsTemplate => true;

            public string ExpressionPrefix => "#{";

            public string ExpressionSuffix => "}";
        }

        public class Rooty
        {
        }
    }
}
