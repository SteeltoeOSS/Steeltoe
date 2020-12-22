// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Spring.Standard;
using Steeltoe.Common.Expression.Spring.Support;
using Steeltoe.Common.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using Xunit;

namespace Steeltoe.Common.Expression.Spring
{
    public class SpelReproTests : AbstractExpressionTests
    {
        [Fact]
        public void NPE_SPR5661()
        {
            Evaluate("JoinThreeStrings('a',null,'c')", "ac", typeof(string));
        }

        [Fact]
        public void SWF1086()
        {
            Evaluate("PrintDouble(T(Decimal).Parse('14.35'))", "14.35", typeof(string));
        }

        [Fact]
        public void DoubleCoercion()
        {
            Evaluate("PrintDouble(14.35)", "14.35", typeof(string));
        }

        [Fact]
        public void DoubleArrayCoercion()
        {
            Evaluate("PrintDoubles(GetDoublesAsStringList())", "{14.35, 15.45}", typeof(string));
        }

        [Fact]
        public void SPR5899()
        {
            var context = new StandardEvaluationContext(new Spr5899Class());
            var expr = new SpelExpressionParser().ParseRaw("TryToInvokeWithNull(12)");
            Assert.Equal(12, expr.GetValue(context));
            expr = new SpelExpressionParser().ParseRaw("TryToInvokeWithNull(null)");
            Assert.Equal(0, expr.GetValue(context));
            expr = new SpelExpressionParser().ParseRaw("TryToInvokeWithNull2(null)");
            Assert.Throws<SpelEvaluationException>(() => expr.GetValue());
            context.TypeLocator = new MyTypeLocator();

            // varargs
            expr = new SpelExpressionParser().ParseRaw("TryToInvokeWithNull3(null,'a','b')");
            Assert.Equal("ab", expr.GetValue(context));

            // varargs 2 - null is packed into the varargs
            expr = new SpelExpressionParser().ParseRaw("TryToInvokeWithNull3(12,'a',null,'c')");
            Assert.Equal("anullc", expr.GetValue(context));

            // check we can find the ctor ok
            expr = new SpelExpressionParser().ParseRaw("new Spr5899Class().ToString()");
            Assert.Equal("instance", expr.GetValue(context));

            expr = new SpelExpressionParser().ParseRaw("new Spr5899Class(null).ToString()");
            Assert.Equal("instance", expr.GetValue(context));

            // ctor varargs
            expr = new SpelExpressionParser().ParseRaw("new Spr5899Class(null,'a','b').ToString()");
            Assert.Equal("instance", expr.GetValue(context));

            // ctor varargs 2
            expr = new SpelExpressionParser().ParseRaw("new Spr5899Class(null,'a', null, 'b').ToString()");
            Assert.Equal("instance", expr.GetValue(context));
        }

        [Fact]
        public void SPR5905_InnerTypeReferences()
        {
            var context = new StandardEvaluationContext(new Spr5899Class());

            // Expression expr = new SpelExpressionParser().ParseRaw("T(java.util.Map$Entry)");
            // Assert.Equal(typeof(), expr.GetValue(context)).isEqualTo(typeof(Map.Entry));
            var expr = new SpelExpressionParser().ParseRaw("T(Steeltoe.Common.Expression.Spring.SpelReproTests$Outer$Inner).Run()");
            Assert.Equal(12, expr.GetValue(context));

            expr = new SpelExpressionParser().ParseRaw("new Steeltoe.Common.Expression.Spring.SpelReproTests$Outer$Inner().Run2()");
            Assert.Equal(13, expr.GetValue(context));
        }

        [Fact]
        public void SPR5804()
        {
            var m = new Dictionary<string, string>();
            m.Add("foo", "bar");
            var context = new StandardEvaluationContext(m);  // root is a map instance
            context.AddPropertyAccessor(new MapAccessor());
            var expr = new SpelExpressionParser().ParseRaw("['foo']");
            Assert.Equal("bar", expr.GetValue(context));
        }

        [Fact]
        public void SPR5847()
        {
            var context = new StandardEvaluationContext(new TestProperties());
            string name = null;
            IExpression expr = null;

            expr = new SpelExpressionParser().ParseRaw("JdbcProperties['username']");
            name = expr.GetValue<string>(context);
            Assert.Equal("Dave", name);

            expr = new SpelExpressionParser().ParseRaw("JdbcProperties[username]");
            name = expr.GetValue<string>(context);
            Assert.Equal("Dave", name);

            // MapAccessor required for this to work
            expr = new SpelExpressionParser().ParseRaw("JdbcProperties.username");
            context.AddPropertyAccessor(new MapAccessor());
            name = expr.GetValue<string>(context);
            Assert.Equal("Dave", name);

            // --- dotted property names

            // lookup foo on the root, then bar on that, then use that as the key into
            // jdbcProperties
            expr = new SpelExpressionParser().ParseRaw("JdbcProperties[Foo.bar]");
            context.AddPropertyAccessor(new MapAccessor());
            name = expr.GetValue<string>(context);
            Assert.Equal("Dave2", name);

            // key is foo.bar
            expr = new SpelExpressionParser().ParseRaw("JdbcProperties['foo.bar']");
            context.AddPropertyAccessor(new MapAccessor());
            name = expr.GetValue<string>(context);
            Assert.Equal("Elephant", name);
        }

        [Fact]
        public void NPE_SPR5673()
        {
            var hashes = TemplateExpressionParsingTests.HASH_DELIMITED_PARSER_CONTEXT;
            var dollars = TemplateExpressionParsingTests.DEFAULT_TEMPLATE_PARSER_CONTEXT;

            CheckTemplateParsing("abc${'def'} ghi", "abcdef ghi");

            CheckTemplateParsingError("abc${ {}( 'abc'", "Missing closing ')' for '(' at position 8");
            CheckTemplateParsingError("abc${ {}[ 'abc'", "Missing closing ']' for '[' at position 8");
            CheckTemplateParsingError("abc${ {}{ 'abc'", "Missing closing '}' for '{' at position 8");
            CheckTemplateParsingError("abc${ ( 'abc' }", "Found closing '}' at position 14 but most recent opening is '(' at position 6");
            CheckTemplateParsingError("abc${ '... }", "Found non terminating string literal starting at position 6");
            CheckTemplateParsingError("abc${ \"... }", "Found non terminating string literal starting at position 6");
            CheckTemplateParsingError("abc${ ) }", "Found closing ')' at position 6 without an opening '('");
            CheckTemplateParsingError("abc${ ] }", "Found closing ']' at position 6 without an opening '['");
            CheckTemplateParsingError("abc${ } }", "No expression defined within delimiter '${}' at character 3");
            CheckTemplateParsingError("abc$[ } ]", new DollarSquareTemplateParserContext(), "Found closing '}' at position 6 without an opening '{'");

            CheckTemplateParsing("abc ${\"def''g}hi\"} jkl", "abc def'g}hi jkl");
            CheckTemplateParsing("abc ${'def''g}hi'} jkl", "abc def'g}hi jkl");
            CheckTemplateParsing("}", "}");
            CheckTemplateParsing("${'hello'} world", "hello world");
            CheckTemplateParsing("Hello ${'}'}]", "Hello }]");
            CheckTemplateParsing("Hello ${'}'}", "Hello }");
            CheckTemplateParsingError("Hello ${ ( ", "No ending suffix '}' for expression starting at character 6: ${ ( ");
            CheckTemplateParsingError("Hello ${ ( }", "Found closing '}' at position 11 but most recent opening is '(' at position 9");
            CheckTemplateParsing("#{'Unable to render embedded object: File ({#this == 2}'}", hashes, "Unable to render embedded object: File ({#this == 2}");
            CheckTemplateParsing("This is the last odd number in the list: ${ListOfNumbersUpToTen.$[#this%2==1]}", dollars, "This is the last odd number in the list: 9");
            CheckTemplateParsing("Hello ${'here is a curly bracket }'}", dollars, "Hello here is a curly bracket }");
            CheckTemplateParsing("He${'${'}llo ${'here is a curly bracket }'}}", dollars, "He${llo here is a curly bracket }}");
            CheckTemplateParsing("Hello ${'()()()}{}{}{][]{}{][}[][][}{()()'} World", dollars, "Hello ()()()}{}{}{][]{}{][}[][][}{()() World");
            CheckTemplateParsing("Hello ${'inner literal that''s got {[(])]}an escaped quote in it'} World", "Hello inner literal that's got {[(])]}an escaped quote in it World");
            CheckTemplateParsingError("Hello ${", "No ending suffix '}' for expression starting at character 6: ${");
        }

        [Fact]
        public void PropertyAccessOnNullTarget_SPR5663()
        {
            var accessor = new ReflectivePropertyAccessor();
            var context = TestScenarioCreator.GetTestEvaluationContext();
            Assert.False(accessor.CanRead(context, null, "abc"));
            Assert.False(accessor.CanWrite(context, null, "abc"));
            Assert.Throws<ArgumentNullException>(() => accessor.Read(context, null, "abc"));
            Assert.Throws<ArgumentNullException>(() => accessor.Write(context, null, "abc", "foo"));
        }

        [Fact]
        public void NestedProperties_SPR6923()
        {
            var context = new StandardEvaluationContext(new Foo());
            var expr = new SpelExpressionParser().ParseRaw("Resource.Resource.Server");
            var name = expr.GetValue<string>(context);
            Assert.Equal("abc", name);
        }

        [Fact]
        public void IndexingAsAPropertyAccess_SPR6968_1()
        {
            var context = new StandardEvaluationContext(new Goo());
            string name = null;
            IExpression expr = null;
            expr = new SpelExpressionParser().ParseRaw("Instance[Bar]");
            name = expr.GetValue<string>(context);
            Assert.Equal("hello", name);
            name = expr.GetValue<string>(context);
            Assert.Equal("hello", name);
        }

        [Fact]
        public void IndexingAsAPropertyAccess_SPR6968_2()
        {
            var context = new StandardEvaluationContext(new Goo());
            context.SetVariable("bar", "Key");
            string name = null;
            IExpression expr = null;
            expr = new SpelExpressionParser().ParseRaw("Instance[#bar]");
            name = expr.GetValue<string>(context);
            Assert.Equal("hello", name);
            name = expr.GetValue<string>(context);
            Assert.Equal("hello", name);
        }

        [Fact]
        public void DollarPrefixedIdentifier_SPR7100()
        {
            var h = new Holder();
            var context = new StandardEvaluationContext(h);
            context.AddPropertyAccessor(new MapAccessor());
            h.Map.Add("$foo", "wibble");
            h.Map.Add("foo$bar", "wobble");
            h.Map.Add("foobar$$", "wabble");
            h.Map.Add("$", "wubble");
            h.Map.Add("$$", "webble");
            h.Map.Add("$_$", "tribble");
            string name = null;
            IExpression expr = null;

            expr = new SpelExpressionParser().ParseRaw("Map.$foo");
            name = expr.GetValue<string>(context);
            Assert.Equal("wibble", name);

            expr = new SpelExpressionParser().ParseRaw("Map.foo$bar");
            name = expr.GetValue<string>(context);
            Assert.Equal("wobble", name);

            expr = new SpelExpressionParser().ParseRaw("Map.foobar$$");
            name = expr.GetValue<string>(context);
            Assert.Equal("wabble", name);

            expr = new SpelExpressionParser().ParseRaw("Map.$");
            name = expr.GetValue<string>(context);
            Assert.Equal("wubble", name);

            expr = new SpelExpressionParser().ParseRaw("Map.$$");
            name = expr.GetValue<string>(context);
            Assert.Equal("webble", name);

            expr = new SpelExpressionParser().ParseRaw("Map.$_$");
            name = expr.GetValue<string>(context);
            Assert.Equal("tribble", name);
        }

        [Fact]
        public void IndexingAsAPropertyAccess_SPR6968_3()
        {
            var context = new StandardEvaluationContext(new Goo());
            Goo.Instance.Wibble = "wobble";
            context.SetVariable("bar", "Wibble");
            string name = null;
            IExpression expr = null;
            expr = new SpelExpressionParser().ParseRaw("Instance[#bar]");

            // will access the field 'wibble' and not use a getter
            name = expr.GetValue<string>(context);
            Assert.Equal("wobble", name);
            name = expr.GetValue<string>(context);
            Assert.Equal("wobble", name);
        }

        [Fact]
        public void IndexingAsAPropertyAccess_SPR6968_4()
        {
            Goo.Instance.Wibble = "wobble";
            var g = Goo.Instance;
            var context = new StandardEvaluationContext(g);
            context.SetVariable("bar", "Wibble");
            IExpression expr = null;
            expr = new SpelExpressionParser().ParseRaw("Instance[#bar]='world'");

            // will access the field 'wibble' and not use a getter
            expr.GetValue<string>(context);
            Assert.Equal("world", g.Wibble);
            expr.GetValue<string>(context);
            Assert.Equal("world", g.Wibble);
        }

        [Fact]
        public void IndexingAsAPropertyAccess_SPR6968_5()
        {
            var g = Goo.Instance;
            var context = new StandardEvaluationContext(g);
            IExpression expr = null;
            expr = new SpelExpressionParser().ParseRaw("Instance[Bar]='world'");
            expr.GetValue<string>(context);
            Assert.Equal("world", g.Value);
            expr.GetValue<string>(context);
            Assert.Equal("world", g.Value);
        }

        [Fact]
        public void Dollars()
        {
            var context = new StandardEvaluationContext(new XX());
            IExpression expr = null;
            expr = new SpelExpressionParser().ParseRaw("M['$foo']");
            context.SetVariable("file_name", "$foo");
            Assert.Equal("wibble", expr.GetValue<string>(context));
        }

        [Fact]
        public void Dollars2()
        {
            var context = new StandardEvaluationContext(new XX());
            IExpression expr = null;
            expr = new SpelExpressionParser().ParseRaw("M[$foo]");
            context.SetVariable("file_name", "$foo");
            Assert.Equal("wibble", expr.GetValue<string>(context));
        }

        [Fact]
        public void BeanResolution()
        {
            var context = new StandardEvaluationContext(new XX());
            IExpression expr = null;

            // no Resolver registered == exception
            try
            {
                expr = new SpelExpressionParser().ParseRaw("@foo");
                Assert.Equal("custard", expr.GetValue<string>(context));
            }
            catch (SpelEvaluationException see)
            {
                Assert.Equal(SpelMessage.NO_BEAN_RESOLVER_REGISTERED, see.MessageCode);
                Assert.Equal("foo", see.Inserts[0]);
            }

            context.ServiceResolver = new MyBeanResolver();

            // bean exists
            expr = new SpelExpressionParser().ParseRaw("@foo");
            Assert.Equal("custard", expr.GetValue<string>(context));

            // bean does not exist
            expr = new SpelExpressionParser().ParseRaw("@bar");
            Assert.Null(expr.GetValue<string>(context));

            // bean name will cause AccessException
            expr = new SpelExpressionParser().ParseRaw("@goo");
            try
            {
                Assert.Null(expr.GetValue<string>(context));
            }
            catch (SpelEvaluationException see)
            {
                Assert.Equal(SpelMessage.EXCEPTION_DURING_BEAN_RESOLUTION, see.MessageCode);
                Assert.Equal("goo", see.Inserts[0]);
                Assert.True(see.InnerException is AccessException);
                Assert.StartsWith("DONT", see.InnerException.Message);
            }

            // bean exists
            expr = new SpelExpressionParser().ParseRaw("@'foo.bar'");
            Assert.Equal("trouble", expr.GetValue<string>(context));

            // bean exists
            try
            {
                expr = new SpelExpressionParser().ParseRaw("@378");
                Assert.Equal("trouble", expr.GetValue<string>(context));
            }
            catch (SpelParseException spe)
            {
                Assert.Equal(SpelMessage.INVALID_BEAN_REFERENCE, spe.MessageCode);
            }
        }

        [Fact]
        public void Elvis_SPR7209_1()
        {
            var context = new StandardEvaluationContext(new XX());
            IExpression expr = null;

            // Different parts of elvis expression are null
            expr = new SpelExpressionParser().ParseRaw("(?:'default')");
            Assert.Equal("default", expr.GetValue());
            expr = new SpelExpressionParser().ParseRaw("?:'default'");
            Assert.Equal("default", expr.GetValue());
            expr = new SpelExpressionParser().ParseRaw("?:");
            Assert.Null(expr.GetValue());

            // Different parts of ternary expression are null
            var ex = Assert.Throws<SpelEvaluationException>(() => new SpelExpressionParser().ParseRaw("(?'abc':'default')").GetValue(context));
            Assert.Equal(SpelMessage.TYPE_CONVERSION_ERROR, ex.MessageCode);
            expr = new SpelExpressionParser().ParseRaw("(false?'abc':null)");
            Assert.Null(expr.GetValue());

            // Assignment
            ex = Assert.Throws<SpelEvaluationException>(() => new SpelExpressionParser().ParseRaw("(='default')").GetValue(context));
            Assert.Equal(SpelMessage.SETVALUE_NOT_SUPPORTED, ex.MessageCode);
        }

        [Fact]
        public void Elvis_SPR7209_2()
        {
            IExpression expr = null;

            // Have empty string treated as null for elvis
            expr = new SpelExpressionParser().ParseRaw("?:'default'");
            Assert.Equal("default", expr.GetValue());
            expr = new SpelExpressionParser().ParseRaw("\"\"?:'default'");
            Assert.Equal("default", expr.GetValue());
            expr = new SpelExpressionParser().ParseRaw("''?:'default'");
            Assert.Equal("default", expr.GetValue());
        }

        [Fact]
        public void MapOfMap_SPR7244()
        {
            var map = new Dictionary<string, object>();
            map.Add("uri", "http:");
            var nameMap = new Dictionary<string, string>();
            nameMap.Add("givenName", "Arthur");
            map.Add("value", nameMap);

            var context = new StandardEvaluationContext(map);
            var parser = new SpelExpressionParser();

            // var el1 = "#root['value'].get('givenName')";
            // var exp = parser.ParseExpression(el1);
            // var evaluated = exp.GetValue(context);
            // Assert.Equal("Arthur", evaluated);
            var el2 = "#root['value']['givenName']";
            var exp = parser.ParseExpression(el2);
            var evaluated = exp.GetValue(context);
            Assert.Equal("Arthur", evaluated);
        }

        [Fact]
        public void ProjectionTypes_1()
        {
            var context = new StandardEvaluationContext(new C());
            var parser = new SpelExpressionParser();
            var el1 = "Ls.![#this.Equals('abc')]";
            var exp = parser.ParseRaw(el1);
            var value = (List<object>)exp.GetValue(context);

            // value is list containing [true,false]
            Assert.IsType<bool>(value[0]);
            var evaluated = exp.GetValueType(context);
            Assert.Equal(typeof(List<object>), evaluated);
        }

        [Fact]
        public void ProjectionTypes_2()
        {
            var context = new StandardEvaluationContext(new C());
            var parser = new SpelExpressionParser();
            var el1 = "As.![#this.Equals('abc')]";
            var exp = parser.ParseRaw(el1);
            var value = (bool[])exp.GetValue(context);

            // value is array containing [true,false]
            Assert.IsType<bool>(value[0]);
            var evaluated = exp.GetValueType(context);
            Assert.Equal(typeof(bool[]), evaluated);
        }

        [Fact]
        public void ProjectionTypes_3()
        {
            var context = new StandardEvaluationContext(new C());
            var parser = new SpelExpressionParser();
            var el1 = "Ms.![Key.Equals('abc')]";
            var exp = parser.ParseRaw(el1);
            var value = (List<object>)exp.GetValue(context);

            // value is list containing [true,false]
            Assert.IsType<bool>(value[0]);
            var evaluated = exp.GetValueType(context);
            Assert.Equal(typeof(List<object>), evaluated);
        }

        [Fact]
        public void GreaterThanWithNulls_SPR7840()
        {
            var list = new List<D>();
            list.Add(new D("aaa"));
            list.Add(new D("bbb"));
            list.Add(new D(null));
            list.Add(new D("ccc"));
            list.Add(new D(null));
            list.Add(new D("zzz"));

            var context = new StandardEvaluationContext(list);
            var parser = new SpelExpressionParser();

            var el1 = "#root.?[A < 'hhh']";
            var exp = parser.ParseRaw(el1);
            var value = exp.GetValue(context) as IEnumerable<object>;
            Assert.Equal("D(aaa),D(bbb),D(),D(ccc),D()", string.Join(",", value));

            var el2 = "#root.?[A > 'hhh']";
            var exp2 = parser.ParseRaw(el2);
            var value2 = exp2.GetValue(context) as IEnumerable<object>;
            Assert.Equal("D(zzz)", string.Join(",", value2));

            // trim out the nulls first
            var el3 = "#root.?[A!=null].?[A < 'hhh']";
            var exp3 = parser.ParseRaw(el3);
            var value3 = exp3.GetValue(context) as IEnumerable<object>;
            Assert.Equal("D(aaa),D(bbb),D(ccc)", string.Join(",", value3));
        }

        [Fact]
        public void ConversionPriority_SPR8224()
        {
            var integer = 7;

            var emptyEvalContext = new StandardEvaluationContext();

            var args = new List<Type>();
            args.Add(typeof(int));

            var target = new ConversionPriority1();
            var me = new ReflectiveMethodResolver(true).Resolve(emptyEvalContext, target, "GetX", args);

            // MethodInvoker chooses getX(int i) when passing Integer
            var actual = (int)me.Execute(emptyEvalContext, target, 42).Value;

            // Compiler chooses getX(Number i) when passing Integer
            var compiler = target.GetX(integer);

            // Fails!
            Assert.Equal(compiler, actual);

            var target2 = new ConversionPriority2();
            var me2 = new ReflectiveMethodResolver(true).Resolve(emptyEvalContext, target2, "GetX", args);

            // MethodInvoker chooses getX(int i) when passing Integer
            var actual2 = (int)me2.Execute(emptyEvalContext, target2, 42).Value;

            // Compiler chooses getX(Number i) when passing Integer
            var compiler2 = target2.GetX(integer);

            // Fails!
            Assert.Equal(compiler2, actual2);
        }

        [Fact]
        public void WideningPrimitiveConversion_SPR8224()
        {
            var iNTEGER_VALUE = 7;
            var target = new WideningPrimitiveConversion();
            var emptyEvalContext = new StandardEvaluationContext();

            var args = new List<Type>();
            args.Add(typeof(int));

            var me = new ReflectiveMethodResolver(true).Resolve(emptyEvalContext, target, "GetX", args);
            var actual = (int)me.Execute(emptyEvalContext, target, iNTEGER_VALUE).Value;

            var compiler = target.GetX(iNTEGER_VALUE);
            Assert.Equal(compiler, actual);
        }

        // [Fact]
        // public void VarargsAgainstProxy_SPR16122()
        // {
        //    var parser = new SpelExpressionParser();
        //    var expr = parser.ParseExpression("Process('a', 'b')");

        // VarargsReceiver receiver = new VarargsReceiver();
        //    VarargsInterface proxy = (VarargsInterface)Proxy.newProxyInstance(
        //            getClass().getClassLoader(), new Class<?>[] { typeof(VarargsInterface) },
        //        (proxy1, method, args) => method.invoke(receiver, args));

        // Assert.Equal(expr.GetValue(new StandardEvaluationContext(receiver))).isEqualTo("OK");
        //    Assert.Equal(expr.GetValue(new StandardEvaluationContext(proxy))).isEqualTo("OK");
        // }

        // [Fact]
        // public void TestCompiledExpressionForProxy_SPR16191()
        // {
        //    SpelExpressionParser expressionParser =
        //            new SpelExpressionParser(new SpelParserConfiguration(SpelCompilerMode.IMMEDIATE, null));
        //    Expression expression = expressionParser.ParseExpression("#target.process(#root)");

        // VarargsReceiver receiver = new VarargsReceiver();
        //    VarargsInterface proxy = (VarargsInterface)Proxy.newProxyInstance(
        //            getClass().getClassLoader(), new Class<?>[] { typeof(VarargsInterface) },
        //        (proxy1, method, args)->method.invoke(receiver, args));

        // StandardEvaluationContext evaluationContext = new StandardEvaluationContext();
        //    evaluationContext.SetVariable("target", proxy);

        // String result = expression.GetValue(evaluationContext, "foo", typeof(String));
        //    result = expression.GetValue(evaluationContext, "foo", typeof(String));
        //    Assert.Equal(result).isEqualTo("OK");
        // }
        [Fact]
        public void VarargsAndPrimitives_SPR8174()
        {
            var emptyEvalContext = new StandardEvaluationContext();
            var args = new List<Type>();

            args.Add(typeof(long));
            var ru = new ReflectionUtil<int>();
            var me = new ReflectiveMethodResolver().Resolve(emptyEvalContext, ru, "MethodToCall", args);

            args[0] = typeof(int);
            me = new ReflectiveMethodResolver().Resolve(emptyEvalContext, ru, "Foo", args);
            me.Execute(emptyEvalContext, ru, 45);

            args[0] = typeof(float);
            me = new ReflectiveMethodResolver().Resolve(emptyEvalContext, ru, "Foo", args);
            me.Execute(emptyEvalContext, ru, 45f);

            args[0] = typeof(double);
            me = new ReflectiveMethodResolver().Resolve(emptyEvalContext, ru, "Foo", args);
            me.Execute(emptyEvalContext, ru, 23d);

            args[0] = typeof(short);
            me = new ReflectiveMethodResolver().Resolve(emptyEvalContext, ru, "Foo", args);
            me.Execute(emptyEvalContext, ru, (short)23);

            args[0] = typeof(long);
            me = new ReflectiveMethodResolver().Resolve(emptyEvalContext, ru, "Foo", args);
            me.Execute(emptyEvalContext, ru, 23L);

            args[0] = typeof(char);
            me = new ReflectiveMethodResolver().Resolve(emptyEvalContext, ru, "Foo", args);
            me.Execute(emptyEvalContext, ru, (char)65);

            args[0] = typeof(byte);
            me = new ReflectiveMethodResolver().Resolve(emptyEvalContext, ru, "Foo", args);
            me.Execute(emptyEvalContext, ru, (byte)23);

            args[0] = typeof(bool);
            me = new ReflectiveMethodResolver().Resolve(emptyEvalContext, ru, "Foo", args);
            me.Execute(emptyEvalContext, ru, true);

            // trickier:
            args[0] = typeof(object);
            args.Add(typeof(float));
            me = new ReflectiveMethodResolver().Resolve(emptyEvalContext, ru, "Bar", args);
            me.Execute(emptyEvalContext, ru, 12, 23f);
        }

        [Fact]
        public void ReservedWords_SPR8228()
        {
            var context = new StandardEvaluationContext(new Reserver());
            var parser = new SpelExpressionParser();
            var ex = "GetReserver().NE";
            var exp = parser.ParseRaw(ex);
            var value = exp.GetValue<string>(context);
            Assert.Equal("abc", value);

            ex = "GetReserver().ne";
            exp = parser.ParseRaw(ex);
            value = exp.GetValue<string>(context);
            Assert.Equal("def", value);

            ex = "GetReserver().M[NE]";
            exp = parser.ParseRaw(ex);
            value = exp.GetValue<string>(context);
            Assert.Equal("xyz", value);

            ex = "GetReserver().DIV";
            exp = parser.ParseRaw(ex);
            Assert.Equal(1, exp.GetValue(context));

            ex = "GetReserver().div";
            exp = parser.ParseRaw(ex);
            Assert.Equal(3, exp.GetValue(context));

            exp = parser.ParseRaw("NE");
            Assert.Equal("abc", exp.GetValue(context));
        }

        [Fact]
        public void ReservedWordProperties_SPR9862()
        {
            var context = new StandardEvaluationContext();
            var parser = new SpelExpressionParser();
            var expression = parser.ParseRaw("T(Steeltoe.Common.Expression.Spring.TestResources.le.div.mod.reserved.Reserver).CONST");
            var value = expression.GetValue(context);
            Assert.Equal(value, Steeltoe.Common.Expression.Spring.TestResources.le.div.mod.reserved.Reserver.CONST);
        }

        [Fact]
        public void PropertyAccessorOrder_SPR8211()
        {
            var expressionParser = new SpelExpressionParser();
            var evaluationContext = new StandardEvaluationContext(new ContextObject());

            evaluationContext.AddPropertyAccessor(new TestPropertyAccessor("FirstContext"));
            evaluationContext.AddPropertyAccessor(new TestPropertyAccessor("SecondContext"));
            evaluationContext.AddPropertyAccessor(new TestPropertyAccessor("ThirdContext"));
            evaluationContext.AddPropertyAccessor(new TestPropertyAccessor("FourthContext"));

            Assert.Equal("first", expressionParser.ParseExpression("shouldBeFirst").GetValue(evaluationContext));
            Assert.Equal("second", expressionParser.ParseExpression("shouldBeSecond").GetValue(evaluationContext));
            Assert.Equal("third", expressionParser.ParseExpression("shouldBeThird").GetValue(evaluationContext));
            Assert.Equal("fourth", expressionParser.ParseExpression("shouldBeFourth").GetValue(evaluationContext));
        }

        [Fact]
        public void CustomStaticFunctions_SPR9038()
        {
            var parser = new SpelExpressionParser();
            var context = new StandardEvaluationContext();
            var methodResolvers = new List<IMethodResolver>();
            methodResolvers.Add(new ParseReflectiveMethodResolver());
            context.MethodResolvers = methodResolvers;
            var type = typeof(System.Globalization.NumberStyles);
            context.SetVariable("parseFormat", NumberStyles.HexNumber);
            var expression = parser.ParseExpression("-Parse('FF', #parseFormat)");

            var result = expression.GetValue<int>(context, typeof(int));
            Assert.Equal(-255, result);
        }

        [Fact]
        public void Array()
        {
            var parser = new SpelExpressionParser();
            var context = new StandardEvaluationContext();
            IExpression expression = null;
            object result = null;

            expression = parser.ParseExpression("new Int64(0).GetType()");
            result = expression.GetValue(context, string.Empty);
            Assert.Equal("System.Int64", result.ToString());

            expression = parser.ParseExpression("T(System.Int64[])");
            result = expression.GetValue(context, string.Empty);
            Assert.Equal("System.Int64[]", result.ToString());

            expression = parser.ParseExpression("T(System.String[][][])");
            result = expression.GetValue(context, string.Empty);
            Assert.Equal("System.String[][][]", result.ToString());
            Assert.Equal("T(System.String[][][])", ((SpelExpression)expression).ToStringAST());

            expression = parser.ParseExpression("new Int32[0].GetType()");
            result = expression.GetValue(context, string.Empty);
            Assert.Equal("System.Int32[]", result.ToString());

            expression = parser.ParseExpression("T(Int32[][])");
            result = expression.GetValue(context, string.Empty);
            Assert.Equal("System.Int32[][]", result.ToString());
        }

        [Fact]
        public void SPR9486_FloatFunctionResolver()
        {
            var expectedResult = Math.Abs(-10.2f);
            var parser = new SpelExpressionParser();
            var testObject = new SPR9486_FunctionsClass();

            var context = new StandardEvaluationContext();
            var expression = parser.ParseExpression("Abs(-10.2f)");
            var result = expression.GetValue<float>(context, testObject);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void SPR9486_AddFloatWithDouble()
        {
            var expectedNumber = 10.21f + 10.2;
            var parser = new SpelExpressionParser();
            var context = new StandardEvaluationContext();
            var expression = parser.ParseExpression("10.21f + 10.2");
            var result = expression.GetValue(context, null);
            Assert.Equal(expectedNumber, result);
        }

        [Fact]
        public void SPR9486_AddFloatWithFloat()
        {
            var expectedNumber = 10.21f + 10.2f;
            var parser = new SpelExpressionParser();
            var context = new StandardEvaluationContext();
            var expression = parser.ParseExpression("10.21f + 10.2f");
            var result = expression.GetValue(context, null);
            Assert.Equal(expectedNumber, result);
        }

        [Fact]
        public void SPR9486_SubtractFloatWithDouble()
        {
            var expectedNumber = 10.21f - 10.2;
            var parser = new SpelExpressionParser();
            var context = new StandardEvaluationContext();
            var expression = parser.ParseExpression("10.21f - 10.2");
            var result = expression.GetValue(context, null);
            Assert.Equal(expectedNumber, result);
        }

        [Fact]
        public void SPR9486_SubtractFloatWithFloat()
        {
            var expectedNumber = 10.21f - 10.2f;
            var parser = new SpelExpressionParser();
            var context = new StandardEvaluationContext();
            var expression = parser.ParseExpression("10.21f - 10.2f");
            var result = expression.GetValue(context, null);
            Assert.Equal(expectedNumber, result);
        }

        [Fact]
        public void SPR9486_MultiplyFloatWithDouble()
        {
            var expectedNumber = 10.21f * 10.2;
            var parser = new SpelExpressionParser();
            var context = new StandardEvaluationContext();
            var expression = parser.ParseExpression("10.21f * 10.2");
            var result = expression.GetValue(context, null);
            Assert.Equal(expectedNumber, result);
        }

        [Fact]
        public void SPR9486_MultiplyFloatWithFloat()
        {
            var expectedNumber = 10.21f * 10.2f;
            var parser = new SpelExpressionParser();
            var context = new StandardEvaluationContext();
            var expression = parser.ParseExpression("10.21f * 10.2f");
            var result = expression.GetValue(context, null);
            Assert.Equal(expectedNumber, result);
        }

        [Fact]
        public void SPR9486_FloatDivideByFloat()
        {
            var expectedNumber = -10.21f / -10.2f;
            var parser = new SpelExpressionParser();
            var context = new StandardEvaluationContext();
            var expression = parser.ParseExpression("-10.21f / -10.2f");
            var result = expression.GetValue(context, null);
            Assert.Equal(expectedNumber, result);
        }

        [Fact]
        public void SPR9486_FloatDivideByDouble()
        {
            var expectedNumber = -10.21f / -10.2;
            var parser = new SpelExpressionParser();
            var context = new StandardEvaluationContext();
            var expression = parser.ParseExpression("-10.21f / -10.2");
            var result = expression.GetValue(context, null);
            Assert.Equal(expectedNumber, result);
        }

        [Fact]
        public void SPR9486_FloatEqFloatUnaryMinus()
        {
            var expectedResult = -10.21f == -10.2f;
            var parser = new SpelExpressionParser();
            var context = new StandardEvaluationContext();
            var expression = parser.ParseExpression("-10.21f == -10.2f");
            var result = expression.GetValue(context, null);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void SPR9486_FloatEqDoubleUnaryMinus()
        {
            var expectedResult = -10.21f == -10.2;
            var parser = new SpelExpressionParser();
            var context = new StandardEvaluationContext();
            var expression = parser.ParseExpression("-10.21f == -10.2");
            var result = expression.GetValue(context, null);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void SPR9486_FloatEqFloat()
        {
            var expectedResult = 10.215f == 10.2109f;
            var parser = new SpelExpressionParser();
            var context = new StandardEvaluationContext();
            var expression = parser.ParseExpression("10.215f == 10.2109f");
            var result = expression.GetValue(context, null);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void SPR9486_FloatEqDouble()
        {
            var expectedResult = 10.215f == 10.2109;
            var parser = new SpelExpressionParser();
            var context = new StandardEvaluationContext();
            var expression = parser.ParseExpression("10.215f == 10.2109");
            var result = expression.GetValue(context, null);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void SPR9486_FloatNotEqFloat()
        {
            var expectedResult = 10.215f != 10.2109f;
            var parser = new SpelExpressionParser();
            var context = new StandardEvaluationContext();
            var expression = parser.ParseExpression("10.215f != 10.2109f");
            var result = expression.GetValue(context, null);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void SPR9486_FloatNotEqDouble()
        {
            var expectedResult = 10.215f != 10.2109;
            var parser = new SpelExpressionParser();
            var context = new StandardEvaluationContext();
            var expression = parser.ParseExpression("10.215f != 10.2109");
            var result = expression.GetValue(context, null);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void SPR9486_FloatLessThanFloat()
        {
            var expectedNumber = -10.21f < -10.2f;
            var parser = new SpelExpressionParser();
            var context = new StandardEvaluationContext();
            var expression = parser.ParseExpression("-10.21f < -10.2f");
            var result = expression.GetValue(context, null);
            Assert.Equal(expectedNumber, result);
        }

        [Fact]
        public void SPR9486_FloatLessThanDouble()
        {
            var expectedNumber = -10.21f < -10.2;
            var parser = new SpelExpressionParser();
            var context = new StandardEvaluationContext();
            var expression = parser.ParseExpression("-10.21f < -10.2");
            var result = expression.GetValue(context, null);
            Assert.Equal(expectedNumber, result);
        }

        [Fact]
        public void SPR9486_FloatLessThanOrEqualFloat()
        {
            var expectedNumber = -10.21f <= -10.22f;
            var parser = new SpelExpressionParser();
            var context = new StandardEvaluationContext();
            var expression = parser.ParseExpression("-10.21f <= -10.22f");
            var result = expression.GetValue(context, null);
            Assert.Equal(expectedNumber, result);
        }

        [Fact]
        public void SPR9486_FloatLessThanOrEqualDouble()
        {
            var expectedNumber = -10.21f <= -10.2;
            var parser = new SpelExpressionParser();
            var context = new StandardEvaluationContext();
            var expression = parser.ParseExpression("-10.21f <= -10.2");
            var result = expression.GetValue(context, null);
            Assert.Equal(expectedNumber, result);
        }

        [Fact]
        public void SPR9486_FloatGreaterThanFloat()
        {
            var expectedNumber = -10.21f > -10.2f;
            var parser = new SpelExpressionParser();
            var context = new StandardEvaluationContext();
            var expression = parser.ParseExpression("-10.21f > -10.2f");
            var result = expression.GetValue(context, null);
            Assert.Equal(expectedNumber, result);
        }

        [Fact]
        public void SPR9486_FloatGreaterThanDouble()
        {
            var expectedResult = -10.21f > -10.2;
            var parser = new SpelExpressionParser();
            var context = new StandardEvaluationContext();
            var expression = parser.ParseExpression("-10.21f > -10.2");
            var result = expression.GetValue(context, null);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void SPR9486_FloatGreaterThanOrEqualFloat()
        {
            var expectedNumber = -10.21f >= -10.2f;
            var parser = new SpelExpressionParser();
            var context = new StandardEvaluationContext();
            var expression = parser.ParseExpression("-10.21f >= -10.2f");
            var result = expression.GetValue(context, null);
            Assert.Equal(expectedNumber, result);
        }

        [Fact]
        public void SPR9486_FloatGreaterThanEqualDouble()
        {
            var expectedResult = -10.21f >= -10.2;
            var parser = new SpelExpressionParser();
            var context = new StandardEvaluationContext();
            var expression = parser.ParseExpression("-10.21f >= -10.2");
            var result = expression.GetValue(context, null);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void SPR9486_FloatModulusFloat()
        {
            var expectedResult = 10.21f % 10.2f;
            var parser = new SpelExpressionParser();
            var context = new StandardEvaluationContext();
            var expression = parser.ParseExpression("10.21f % 10.2f");
            var result = expression.GetValue(context, null);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void SPR9486_FloatModulusDouble()
        {
            var expectedResult = 10.21f % 10.2;
            var parser = new SpelExpressionParser();
            var context = new StandardEvaluationContext();
            var expression = parser.ParseExpression("10.21f % 10.2");
            var result = expression.GetValue(context, null);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void SPR9486_FloatPowerFloat()
        {
            var expectedResult = Math.Pow(10.21f, -10.2f);
            var parser = new SpelExpressionParser();
            var context = new StandardEvaluationContext();
            var expression = parser.ParseExpression("10.21f ^ -10.2f");
            var result = expression.GetValue(context, null);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void SPR9486_FloatPowerDouble()
        {
            var expectedResult = Math.Pow(10.21f, 10.2);
            var parser = new SpelExpressionParser();
            var context = new StandardEvaluationContext();
            var expression = parser.ParseExpression("10.21f ^ 10.2");
            var result = expression.GetValue(context, null);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void SPR10091_simpleTestValueType()
        {
            var parser = new SpelExpressionParser();
            var evaluationContext = new StandardEvaluationContext(new BooleanHolder());
            var valueType = parser.ParseExpression("SimpleProperty").GetValueType(evaluationContext);
            Assert.Equal(typeof(bool), valueType);
        }

        [Fact]
        public void SPR10091_simpleTestValue()
        {
            var parser = new SpelExpressionParser();
            var evaluationContext = new StandardEvaluationContext(new BooleanHolder());
            var value = parser.ParseExpression("SimpleProperty").GetValue(evaluationContext);
            Assert.IsType<bool>(value);
        }

        [Fact]
        public void SPR10091_primitiveTestValueType()
        {
            var parser = new SpelExpressionParser();
            var evaluationContext = new StandardEvaluationContext(new BooleanHolder());
            var valueType = parser.ParseExpression("PrimitiveProperty").GetValueType(evaluationContext);
            Assert.Equal(typeof(bool), valueType);
        }

        [Fact]
        public void SPR10091_primitiveTestValue()
        {
            var parser = new SpelExpressionParser();
            var evaluationContext = new StandardEvaluationContext(new BooleanHolder());
            var value = parser.ParseExpression("PrimitiveProperty").GetValue(evaluationContext);
            Assert.IsType<bool>(value);
        }

        [Fact]
        public void SPR16123()
        {
            var parser = new SpelExpressionParser();
            parser.ParseExpression("SimpleProperty").SetValue(new BooleanHolder(), null);
            Assert.Throws<SpelEvaluationException>(() => parser.ParseExpression("PrimitiveProperty").SetValue(new BooleanHolder(), null));
        }

        [Fact]
        public void SPR10146_MalformedExpressions()
        {
            DoTestSpr10146("/foo", "EL1070E: Problem parsing left operand");
            DoTestSpr10146("*foo", "EL1070E: Problem parsing left operand");
            DoTestSpr10146("%foo", "EL1070E: Problem parsing left operand");
            DoTestSpr10146("<foo", "EL1070E: Problem parsing left operand");
            DoTestSpr10146(">foo", "EL1070E: Problem parsing left operand");
            DoTestSpr10146("&&foo", "EL1070E: Problem parsing left operand");
            DoTestSpr10146("||foo", "EL1070E: Problem parsing left operand");
            DoTestSpr10146("|foo", "EL1069E: Missing expected character ''|''");
        }

        // TODO: support values in interfaces
        // [Fact]
        // public void SPR10125()
        // {
        //    var context = new StandardEvaluationContext();
        //    var fromInterface = parser.ParseExpression("T(" + typeof(StaticFinalImpl1).FullName.Replace("+", "$") + ").VALUE").GetValue<string>(context);
        //    Assert.Equal("interfaceValue", fromInterface);
        //    var fromClass = parser.ParseExpression("T(" + typeof(StaticFinalImpl2).FullName.Replace("+", "$") + ").VALUE").GetValue<string>(context);
        //    Assert.Equal("interfaceValue", fromClass);
        // }
        [Fact]
        public void SPR10210()
        {
            var context = new StandardEvaluationContext();
            context.SetVariable("bridgeExample", new SPR10210.D());
            var parseExpression = parser.ParseExpression("#bridgeExample.BridgeMethod()");
            parseExpression.GetValue(context);
        }

        [Fact]
        public void SPR10328()
        {
            var ex = Assert.Throws<SpelParseException>(() => parser.ParseExpression("$[]"));
            Assert.Contains("EL1071E: A required selection expression has not been specified", ex.Message);
        }

        [Fact]
        public void SPR10452()
        {
            var configuration = new SpelParserOptions(false, false);
            var parser = new SpelExpressionParser(configuration);

            var context = new StandardEvaluationContext();
            var spel = parser.ParseExpression("T(Enum).GetValues(#enumType)");

            context.SetVariable("enumType", typeof(ABC));
            var result = spel.GetValue(context);
            Assert.NotNull(result);
            Assert.True(result.GetType().IsArray);
            var asArray = result as Array;
            Assert.Equal(ABC.A, asArray.GetValue(0));
            Assert.Equal(ABC.B, asArray.GetValue(1));
            Assert.Equal(ABC.C, asArray.GetValue(2));

            context.SetVariable("enumType", typeof(XYZ));
            result = spel.GetValue(context);
            Assert.NotNull(result);
            Assert.True(result.GetType().IsArray);
            asArray = result as Array;
            Assert.Equal(XYZ.X, asArray.GetValue(0));
            Assert.Equal(XYZ.Y, asArray.GetValue(1));
            Assert.Equal(XYZ.Z, asArray.GetValue(2));
        }

        [Fact]
        public void SPR9495()
        {
            var configuration = new SpelParserOptions(false, false);
            var parser = new SpelExpressionParser(configuration);

            var context = new StandardEvaluationContext();
            var spel = parser.ParseExpression("T(Enum).GetValues(#enumType)");

            context.SetVariable("enumType", typeof(ABC));
            var result = spel.GetValue(context);
            Assert.NotNull(result);
            Assert.True(result.GetType().IsArray);
            var asArray = result as Array;
            Assert.Equal(ABC.A, asArray.GetValue(0));
            Assert.Equal(ABC.B, asArray.GetValue(1));
            Assert.Equal(ABC.C, asArray.GetValue(2));

            context.AddMethodResolver(new ValuesMethodReslover());
            result = spel.GetValue(context);
            Assert.NotNull(result);
            Assert.True(result.GetType().IsArray);
            asArray = result as Array;
            Assert.Equal(XYZ.X, asArray.GetValue(0));
            Assert.Equal(XYZ.Y, asArray.GetValue(1));
            Assert.Equal(XYZ.Z, asArray.GetValue(2));
        }

        [Fact]
        public void SPR10486()
        {
            var parser = new SpelExpressionParser();
            var context = new StandardEvaluationContext();
            var rootObject = new Spr10486();
            var classNameExpression = parser.ParseExpression("GetType().FullName");
            var nameExpression = parser.ParseExpression("Name");
            Assert.Equal(typeof(Spr10486).FullName, classNameExpression.GetValue(context, rootObject));
            Assert.Equal("name", nameExpression.GetValue(context, rootObject));
        }

        [Fact]
        public void SPR11142()
        {
            var parser = new SpelExpressionParser();
            var context = new StandardEvaluationContext();
            var rootObject = new Spr11142();
            var expression = parser.ParseExpression("Something");
            var ex = Assert.Throws<SpelEvaluationException>(() => expression.GetValue(context, rootObject));
            Assert.Contains("''Something'' cannot be found", ex.Message);
        }

        [Fact]
        public void SPR9194()
        {
            TestClass2 one = new TestClass2("abc");
            TestClass2 two = new TestClass2("abc");
            var map = new Dictionary<string, TestClass2>();
            map.Add("one", one);
            map.Add("two", two);

            var parser = new SpelExpressionParser();
            var expr = parser.ParseExpression("['one'] == ['two']");
            Assert.True(expr.GetValue<bool>(map));
        }

        [Fact]
        public void SPR11348()
        {
            var coll = new HashSet<string>();
            coll.Add("one");
            coll.Add("two");

            // coll = Collections.unmodifiableCollection(coll);
            var parser = new SpelExpressionParser();
            var expr = parser.ParseExpression("new System.Collections.ArrayList(#root)");
            var value = expr.GetValue(coll);
            Assert.IsType<ArrayList>(value);

            var list = (ArrayList)value;
            Assert.Equal("one", list[0]);
            Assert.Equal("two", list[1]);
        }

        [Fact]
        public void SPR11445_Simple()
        {
            var context = new StandardEvaluationContext(new Spr11445Class());
            var expr = new SpelExpressionParser().ParseRaw("Echo(Parameter())");
            Assert.Equal(1, expr.GetValue(context));
        }

        [Fact]
        public void SPR11445_BeanReference()
        {
            var context = new StandardEvaluationContext();
            context.ServiceResolver = new Spr11445Class();
            var expr = new SpelExpressionParser().ParseRaw("@bean.Echo(@bean.Parameter())");
            Assert.Equal(1, expr.GetValue(context));
        }

        [Fact]
        public void SPR11609()
        {
            var sec = new StandardEvaluationContext();
            sec.AddPropertyAccessor(new MapAccessor());
            var exp = new SpelExpressionParser().ParseExpression(
                    "T(Steeltoe.Common.Expression.Spring.SpelReproTests$MapWithConstant).X");
            Assert.Equal(1, exp.GetValue(sec));
        }

        [Fact]
        public void SPR9735()
        {
            Item item = new Item();
            item.Name = "parent";

            Item item1 = new Item();
            item1.Name = "child1";

            Item item2 = new Item();
            item2.Name = "child2";

            item.Add(item1);
            item.Add(item2);

            var parser = new SpelExpressionParser();
            var context = new StandardEvaluationContext();
            var exp = parser.ParseExpression("#item[0].Name");
            context.SetVariable("item", item);

            Assert.Equal("child1", exp.GetValue(context));
        }

        [Fact]
        public void SPR12502()
        {
            var parser = new SpelExpressionParser();
            var expression = parser.ParseExpression("#root.GetType().Name");
            Assert.Equal(typeof(UnnamedUser).Name, expression.GetValue(new UnnamedUser()));
            Assert.Equal(typeof(NamedUser).Name, expression.GetValue(new NamedUser()));
        }

        [Fact]
        public void SPR12522()
        {
            var parser = new SpelExpressionParser();
            var expression = parser.ParseExpression("T(Array).CreateInstance(T(String), 0)");
            var value = expression.GetValue();
            Assert.True(value is IList);
            Assert.Empty((IList)value);
        }

        // TODO: Would be nice to support generic methods.
        // [Fact]
        // public void SPR12803()
        // {
        //    var sec = new StandardEvaluationContext();
        //    sec.SetVariable("iterable", new ArrayList());
        //    var parser = new SpelExpressionParser();
        //    var expression = parser.ParseExpression("T(Steeltoe.Common.Expression.Spring.SpelReproTests.FooLists).NewArrayList(#iterable)");
        //    Assert.True(expression.GetValue(sec) is ArrayList);
        // }

        // TODO: Would be nice to support generic methods.
        // [Fact]

        // public void SPR11494()
        // {
        //    var exp = new SpelExpressionParser().ParseExpression("T(System.Linq.Enumerable).ToList(new String[] {'a', 'b'})");
        //    var list = (ArrayList)exp.GetValue();
        //    Assert.Equal(2, list.Count);
        // }
        [Fact]
        public void SPR12808()
        {
            var parser = new SpelExpressionParser();
            var expression = parser.ParseExpression("T(Steeltoe.Common.Expression.Spring.SpelReproTests$DistanceEnforcer).From(#no)");
            var sec = new StandardEvaluationContext();
            sec.SetVariable("no", 1);
            Assert.StartsWith("Integer", expression.GetValue(sec).ToString());
            sec = new StandardEvaluationContext();
            sec.SetVariable("no", 1.0F);
            Assert.StartsWith("ValueType", expression.GetValue(sec).ToString());
            sec = new StandardEvaluationContext();
            sec.SetVariable("no", "1.0");
            Assert.StartsWith("Object", expression.GetValue(sec).ToString());
        }

        [Fact]
        public void SPR13055()
        {
            var myPayload = new List<Dictionary<string, object>>();

            var v1 = new Dictionary<string, object>();
            var v2 = new Dictionary<string, object>();

            v1.Add("test11", "test11");
            v1.Add("test12", "test12");
            v2.Add("test21", "test21");
            v2.Add("test22", "test22");

            myPayload.Add(v1);
            myPayload.Add(v2);

            var context = new StandardEvaluationContext(myPayload);

            var parser = new SpelExpressionParser();

            var ex = "#root.![T(String).Join(',', #this.Values)]";
            var res = parser.ParseExpression(ex).GetValue<string>(context);
            Assert.Equal("test11,test12,test21,test22", res);

            res = parser.ParseExpression("#root.![#this.Values]").GetValue<string>(context);
            Assert.Equal("test11,test12,test21,test22", res);

            res = parser.ParseExpression("#root.![Values]").GetValue<string>(context);
            Assert.Equal("test11,test12,test21,test22", res.ToString());
        }

        [Fact]
        public void AccessingFactoryBean_spr9511()
        {
            var context = new StandardEvaluationContext();
            context.ServiceResolver = new MyBeanResolver();
            var expr = new SpelExpressionParser().ParseRaw("@foo");
            Assert.Equal("custard", expr.GetValue(context));
            expr = new SpelExpressionParser().ParseRaw("&foo");
            Assert.Equal("foo factory", expr.GetValue(context));

            var ex = Assert.Throws<SpelParseException>(() => new SpelExpressionParser().ParseRaw("&@foo"));
            Assert.Equal(SpelMessage.INVALID_BEAN_REFERENCE, ex.MessageCode);
            Assert.Equal(0, ex.Position);

            ex = Assert.Throws<SpelParseException>(() => new SpelExpressionParser().ParseRaw("@&foo"));
            Assert.Equal(SpelMessage.INVALID_BEAN_REFERENCE, ex.MessageCode);
            Assert.Equal(0, ex.Position);
        }

        [Fact]
        public void SPR12035()
        {
            var parser = new SpelExpressionParser();

            var expression1 = parser.ParseExpression("List.?[ Value>2 ].Count!=0");
            Assert.True(expression1.GetValue<bool>(new BeanClass(new ListOf(1.1), new ListOf(2.2))));

            var expression2 = parser.ParseExpression("List.?[ T(Math).Abs(Value) > 2 ].Count!=0");
            Assert.True(expression2.GetValue<bool>(new BeanClass(new ListOf(1.1), new ListOf(-2.2))));
        }

        [Fact]
        public void SPR13055_maps()
        {
            var context = new StandardEvaluationContext();
            var parser = new SpelExpressionParser();

            var ex = parser.ParseExpression("{'a':'y','b':'n'}.![Value=='y'?Key:null]");
            Assert.Equal("a,", string.Join(",", ex.GetValue<IEnumerable<string>>(context)));

            ex = parser.ParseExpression("{2:4,3:6}.![T(Math).Abs(#this.Key) + 5]");
            Assert.Equal("7,8", string.Join(",", ex.GetValue<IEnumerable<string>>(context)));

            ex = parser.ParseExpression("{2:4,3:6}.![T(Math).Abs(#this.Value) + 5]");
            Assert.Equal("9,11", string.Join(",", ex.GetValue<IEnumerable<string>>(context)));
        }

        [Fact]
        public void SPR10417()
        {
            var list1 = new ArrayList();
            list1.Add("a");
            list1.Add("b");
            list1.Add("x");
            var list2 = new ArrayList();
            list2.Add("c");
            list2.Add("x");
            var context = new StandardEvaluationContext();
            context.SetVariable("list1", list1);
            context.SetVariable("list2", list2);

            // #this should be the element from list1
            var ex = parser.ParseExpression("#list1.?[#list2.Contains(#this)]");
            var result = ex.GetValue<IEnumerable<string>>(context);
            Assert.Equal("x", string.Join(",", result));

            // toString() should be called on the element from list1
            ex = parser.ParseExpression("#list1.?[#list2.Contains(ToString())]");
            result = ex.GetValue<IEnumerable<string>>(context);
            Assert.Equal("x", string.Join(",", result));

            var list3 = new ArrayList();
            list3.Add(1);
            list3.Add(2);
            list3.Add(3);
            list3.Add(4);

            context = new StandardEvaluationContext();
            context.SetVariable("list3", list3);
            ex = parser.ParseExpression("#list3.?[#this > 2]");
            result = ex.GetValue<IEnumerable<string>>(context);
            Assert.Equal("3,4", string.Join(",", result));

            ex = parser.ParseExpression("#list3.?[#this >= T(Math).Abs(T(Math).Abs(#this))]");
            result = ex.GetValue<IEnumerable<string>>(context);
            Assert.Equal("1,2,3,4", string.Join(",", result));
        }

        [Fact]
        public void SPR10417_maps()
        {
            var map1 = new Dictionary<string, int>();
            map1.Add("A", 65);
            map1.Add("B", 66);
            map1.Add("X", 66);
            var map2 = new Dictionary<string, int>();
            map2.Add("X", 66);

            var context = new StandardEvaluationContext();
            context.SetVariable("map1", map1);
            context.SetVariable("map2", map2);

            // #this should be the element from list1
            var ex = parser.ParseExpression("#map1.?[#map2.ContainsKey(#this.Key)]");
            var result = ex.GetValue<IDictionary>(context);
            Assert.Single(result);
            Assert.Equal(66, result["X"]);

            ex = parser.ParseExpression("#map1.?[#map2.ContainsKey(Key)]");
            result = ex.GetValue<IDictionary>(context);
            Assert.Single(result);
            Assert.Equal(66, result["X"]);
        }

        [Fact]
        public void SPR13918()
        {
            var context = new StandardEvaluationContext();
            context.SetVariable("encoding", "UTF-8");

            var ex = parser.ParseExpression("T(System.Text.Encoding).GetEncoding(#encoding)");
            var result = ex.GetValue(context);
            Assert.Equal(Encoding.UTF8, result);
        }

        [Fact]
        public void SPR16032()
        {
            var context = new StandardEvaluationContext();
            context.SetVariable("str", "a\0b");

            var ex = parser.ParseExpression("#str?.Split('\0')");
            var result = ex.GetValue(context);
            Assert.True(ObjectUtils.NullSafeEquals(result, new string[] { "a", "b" }));
        }

        private void DoTestSpr10146(string expression, string expectedMessage)
        {
            var ex = Assert.Throws<SpelParseException>(() => new SpelExpressionParser().ParseExpression(expression));
            Assert.Contains(expectedMessage, ex.Message);
        }

        private void CheckTemplateParsing(string expression, string expectedValue)
        {
            CheckTemplateParsing(expression, TemplateExpressionParsingTests.DEFAULT_TEMPLATE_PARSER_CONTEXT, expectedValue);
        }

        private void CheckTemplateParsing(string expression, IParserContext context, string expectedValue)
        {
            var parser = new SpelExpressionParser();
            var expr = parser.ParseExpression(expression, context);
            Assert.Equal(expectedValue, expr.GetValue(TestScenarioCreator.GetTestEvaluationContext()));
        }

        private void CheckTemplateParsingError(string expression, string expectedMessage)
        {
            CheckTemplateParsingError(expression, TemplateExpressionParsingTests.DEFAULT_TEMPLATE_PARSER_CONTEXT, expectedMessage);
        }

        private void CheckTemplateParsingError(string expression, IParserContext context, string expectedMessage)
        {
            var parser = new SpelExpressionParser();
            var ex = Assert.Throws<ParseException>(() => parser.ParseExpression(expression, context));
            var message = ex.Message;
            if (ex is ExpressionException)
            {
                message = ((ExpressionException)ex).SimpleMessage;
            }

            Assert.Equal(expectedMessage, message);
        }

        #region Test Types
        public class ValuesMethodReslover : IMethodResolver
        {
            public IMethodExecutor Resolve(IEvaluationContext context, object targetObject, string name, List<Type> argumentTypes)
            {
                return new ValuesMethodExecutor();
            }
        }

        public class ValuesMethodExecutor : IMethodExecutor
        {
            public ITypedValue Execute(IEvaluationContext context, object target, params object[] arguments)
            {
                try
                {
                    var method = typeof(Enum).GetMethod("GetValues", new Type[] { typeof(Type) });
                    var value = method.Invoke(null, new object[] { typeof(XYZ) });
                    return new TypedValue(value, value == null ? typeof(object) : value.GetType());
                }
                catch (Exception ex)
                {
                    throw new AccessException(ex.Message, ex);
                }
            }
        }

        public class ParseReflectiveMethodResolver : ReflectiveMethodResolver
        {
            protected override MethodInfo[] GetMethods(Type type)
            {
                try
                {
                    return new MethodInfo[] { typeof(int).GetMethod("Parse", new Type[] { typeof(string), typeof(System.Globalization.NumberStyles) }) };
                }
                catch (Exception)
                {
                    return new MethodInfo[0];
                }
            }
        }

        public class Reserver
        {
            public Reserver GetReserver() => this;

            public string NE = "abc";
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
            public string ne = "def";
#pragma warning restore SA1307 // Accessible fields should begin with upper-case letter

            public int DIV = 1;
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
            public int div = 3;
#pragma warning restore SA1307 // Accessible fields should begin with upper-case letter

            public Dictionary<string, string> M = new Dictionary<string, string>();

            public Reserver()
            {
                M.Add("NE", "xyz");
            }
        }

        public class WideningPrimitiveConversion
        {
            public int GetX(long i)
            {
                return 10;
            }
        }

        public class ConversionPriority1
        {
            public int GetX(ValueType i)
            {
                return 20;
            }

            public int GetX(int i)
            {
                return 10;
            }
        }

        public class ConversionPriority2
        {
            public int GetX(int i)
            {
                return 10;
            }

            public int GetX(ValueType i)
            {
                return 20;
            }
        }

        public class DollarSquareTemplateParserContext : IParserContext
        {
            public bool IsTemplate => true;

            public string ExpressionPrefix => "$[";

            public string ExpressionSuffix => "]";
        }

        public class MyTypeLocator : StandardTypeLocator
        {
            public override Type FindType(string typeName)
            {
                if (typeName.Equals("Spr5899Class"))
                {
                    return typeof(Spr5899Class);
                }

                if (typeName.Equals("Outer"))
                {
                    return typeof(Outer);
                }

                return base.FindType(typeName);
            }
        }

        public class Spr5899Class
        {
            public Spr5899Class()
            {
            }

            public Spr5899Class(int i)
            {
            }

            public Spr5899Class(int i, params string[] s)
            {
            }

            public int TryToInvokeWithNull(int value)
            {
                return value;
            }

            public int TryToInvokeWithNull2(int i)
            {
                return i;
            }

            public string TryToInvokeWithNull3(int value, params string[] strings)
            {
                var sb = new StringBuilder();
                foreach (var str in strings)
                {
                    if (str == null)
                    {
                        sb.Append("null");
                    }
                    else
                    {
                        sb.Append(str);
                    }
                }

                return sb.ToString();
            }

            public override string ToString()
            {
                return "instance";
            }
        }

        public class TestProperties
        {
            public Dictionary<string, string> JdbcProperties = new Dictionary<string, string>();

            public Dictionary<string, string> Foo = new Dictionary<string, string>();

            public TestProperties()
            {
                JdbcProperties.Add("username", "Dave");
                JdbcProperties.Add("alias", "Dave2");
                JdbcProperties.Add("foo.bar", "Elephant");
                Foo.Add("bar", "alias");
            }
        }

        public class MapAccessor : IPropertyAccessor
        {
            public IList<Type> GetSpecificTargetClasses()
            {
                return new List<Type>() { typeof(IDictionary) };
            }

            public bool CanRead(IEvaluationContext context, object target, string name)
            {
                return ((IDictionary)target).Contains(name);
            }

            public ITypedValue Read(IEvaluationContext context, object target, string name)
            {
                return new TypedValue(((IDictionary)target)[name]);
            }

            public bool CanWrite(IEvaluationContext context, object target, string name)
            {
                return true;
            }

            public void Write(IEvaluationContext context, object target, string name, object newValue)
            {
                ((IDictionary)target).Add(name, newValue);
            }
        }

        public class Outer
        {
            public class Inner
            {
                public Inner()
                {
                }

                public static int Run()
                {
                    return 12;
                }

                public int Run2()
                {
                    return 13;
                }
            }
        }

        public class XX
        {
            public Dictionary<string, string> M;

            public string Floo = "bar";

            public XX()
            {
                M = new Dictionary<string, string>();
                M.Add("$foo", "wibble");
                M.Add("bar", "siddle");
            }
        }

        public class MyBeanResolver : IServiceResolver
        {
            public object Resolve(IEvaluationContext context, string beanName)
            {
                if (beanName.Equals("foo"))
                {
                    return "custard";
                }
                else if (beanName.Equals("foo.bar"))
                {
                    return "trouble";
                }
                else if (beanName.Equals("&foo"))
                {
                    return "foo factory";
                }
                else if (beanName.Equals("goo"))
                {
                    throw new AccessException("DONT ASK ME ABOUT GOO");
                }

                return null;
            }
        }

        public class CCC
        {
            public bool Method(object o)
            {
                Console.WriteLine(o);
                return false;
            }
        }

        public class C
        {
            public List<string> Ls;

            public string[] As;

            public Dictionary<string, string> Ms;

            public C()
            {
                Ls = new List<string>
                {
                    "abc",
                    "def"
                };

                As = new string[] { "abc", "def" };
                Ms = new Dictionary<string, string>
                {
                    ["abc"] = "xyz",
                    ["def"] = "pqr"
                };
            }
        }

        public class D
        {
            public string A;

            public D(string s)
            {
                A = s;
            }

            public override string ToString()
            {
                return "D(" + A + ")";
            }
        }

        public class Resource
        {
            public string Server => "abc";
        }

        public class ResourceSummary
        {
            private readonly Resource resource;

            public ResourceSummary()
            {
                resource = new Resource();
            }

            public Resource Resource => resource;
        }

        public class Foo
        {
            public ResourceSummary Resource = new ResourceSummary();
        }

        public class Foo2
        {
            public void Execute(string str)
            {
                Console.WriteLine("Value: " + str);
            }
        }

        public class Message
        {
            public string Payload { get; set; }
        }

        public class Goo
        {
            public static Goo Instance = new Goo();

            public string Bar = "Key";

            public string Value = null;

            public string Wibble = "wobble";

            public string Key
            {
                get => "hello";
                set => Value = value;
            }
        }

        public class Holder
        {
            public Dictionary<string, string> Map = new Dictionary<string, string>();
        }

        public class SPR9486_FunctionsClass
        {
            public int Abs(int value)
            {
                return Math.Abs(value);
            }

            public float Abs(float value)
            {
                return Math.Abs(value);
            }
        }

        public interface IVarargsInterface
        {
            string Process(params string[] args);
        }

        public class VarargsReceiver : IVarargsInterface
        {
            public string Process(params string[] args)
            {
                return "OK";
            }
        }

        public class ReflectionUtil<T>
        {
            public object MethodToCall(T param)
            {
                Console.WriteLine(param + " " + param.GetType());
                return "object MethodToCall(T param)";
            }

            public void Foo(params int[] array)
            {
                if (array.Length == 0)
                {
                    throw new SystemException();
                }
            }

            public void Foo(params float[] array)
            {
                if (array.Length == 0)
                {
                    throw new SystemException();
                }
            }

            public void Foo(params double[] array)
            {
                if (array.Length == 0)
                {
                    throw new SystemException();
                }
            }

            public void Foo(params short[] array)
            {
                if (array.Length == 0)
                {
                    throw new SystemException();
                }
            }

            public void Foo(params long[] array)
            {
                if (array.Length == 0)
                {
                    throw new SystemException();
                }
            }

            public void Foo(params bool[] array)
            {
                if (array.Length == 0)
                {
                    throw new SystemException();
                }
            }

            public void Foo(params char[] array)
            {
                if (array.Length == 0)
                {
                    throw new SystemException();
                }
            }

            public void Foo(params byte[] array)
            {
                if (array.Length == 0)
                {
                    throw new SystemException();
                }
            }

            public void Bar(params int[] array)
            {
                if (array.Length == 0)
                {
                    throw new SystemException();
                }
            }
        }

        public class TestPropertyAccessor : IPropertyAccessor
        {
            private string mapName;

            public TestPropertyAccessor(string mapName)
            {
                this.mapName = mapName;
            }

            public Dictionary<string, string> GetMap(object target)
            {
                try
                {
                    var f = target.GetType().GetField(mapName);
                    return (Dictionary<string, string>)f.GetValue(target);
                }
                catch (Exception ex)
                {
                }

                return null;
            }

            public bool CanRead(IEvaluationContext context, object target, string name)
            {
                return GetMap(target).ContainsKey(name);
            }

            public bool CanWrite(IEvaluationContext context, object target, string name)
            {
                return GetMap(target).ContainsKey(name);
            }

            public IList<Type> GetSpecificTargetClasses()
            {
                return new List<Type>() { typeof(ContextObject) };
            }

            public ITypedValue Read(IEvaluationContext context, object target, string name)
            {
                return new TypedValue(GetMap(target)[name]);
            }

            public void Write(IEvaluationContext context, object target, string name, object newValue)
            {
                GetMap(target)[name] = (string)newValue;
            }
        }

        public class ContextObject
        {
            public Dictionary<string, string> FirstContext = new Dictionary<string, string>();

            public Dictionary<string, string> SecondContext = new Dictionary<string, string>();

            public Dictionary<string, string> ThirdContext = new Dictionary<string, string>();

            public Dictionary<string, string> FourthContext = new Dictionary<string, string>();

            public ContextObject()
            {
                FirstContext.Add("shouldBeFirst", "first");
                SecondContext.Add("shouldBeFirst", "second");
                ThirdContext.Add("shouldBeFirst", "third");
                FourthContext.Add("shouldBeFirst", "fourth");

                SecondContext.Add("shouldBeSecond", "second");
                ThirdContext.Add("shouldBeSecond", "third");
                FourthContext.Add("shouldBeSecond", "fourth");

                ThirdContext.Add("shouldBeThird", "third");
                FourthContext.Add("shouldBeThird", "fourth");

                FourthContext.Add("shouldBeFourth", "fourth");
            }
        }

        public class ListOf
        {
            public double Value { get; }

            public ListOf(double v)
            {
                Value = v;
            }
        }

        public class BeanClass
        {
            public List<ListOf> List { get; }

            public BeanClass(params ListOf[] list)
            {
                List = new List<ListOf>(list);
            }
        }

        public enum ABC
        {
            A,
            B,
            C
        }

        public enum XYZ
        {
            X,
            Y,
            Z
        }

        public class BooleanHolder
        {
            public object SimpleProperty { get; set; } = true;

            public bool PrimitiveProperty { get; set; } = true;

            public object IsSimpleProperty => SimpleProperty;

            public bool IsPrimitiveProperty => PrimitiveProperty;
        }

        public interface IGenericInterface<T>
        {
            T Property { get; set; }
        }

        public class GenericImplementation : IGenericInterface<int>
        {
            public int Property { get; set; }
        }

        public class PackagePrivateClassWithGetter
        {
            public int Property { get; }
        }

        public class OnlyBridgeMethod : PackagePrivateClassWithGetter
        {
        }

        public interface IStaticFinal
        {
            string VALUE => "interfaceValue";
        }

        public abstract class AbstractStaticFinal : IStaticFinal
        {
        }

        public class StaticFinalImpl1 : AbstractStaticFinal, IStaticFinal
        {
        }

        public class StaticFinalImpl2 : AbstractStaticFinal
        {
        }

        public class Spr10486
        {
            public string Name { get; set; } = "name";
        }

        public class Spr11142
        {
            public string IsSomething => string.Empty;
        }

        public class TestClass2
        {
            // SPR-9194
            private readonly string str;

            public TestClass2(string str)
            {
                this.str = str;
            }

            public override bool Equals(object other)
            {
                return this == other || (other is TestClass2 &&
                        str.Equals(((TestClass2)other).str));
            }

            public override int GetHashCode()
            {
                return str.GetHashCode();
            }
        }

        public class Spr11445Class : IServiceResolver
        {
            private readonly AtomicInteger counter = new AtomicInteger();

            public int Echo(int invocation)
            {
                return invocation;
            }

            public int Parameter()
            {
                return this.counter.IncrementAndGet();
            }

            public object Resolve(IEvaluationContext context, string beanName)
            {
                return beanName.Equals("bean") ? this : null;
            }
        }

        public class MapWithConstant : Hashtable
        {
            public static readonly int X = 1;
        }

        public class Item : IList<Item>, IList
        {
            private List<Item> _children = new List<Item>();

            public Item this[int index] { get => _children[index]; set => _children[index] = value; }

            object IList.this[int index] { get => _children[index]; set => _children[index] = (Item)value; }

            public string Name { get; set; }

            public bool IsReadOnly => false;

            public int Count => _children.Count;

            public bool IsFixedSize => false;

            public bool IsSynchronized => false;

            public object SyncRoot => ((IList)_children).SyncRoot;

            public void Add(Item item)
            {
                _children.Add(item);
            }

            public int Add(object value)
            {
                throw new NotImplementedException();
            }

            public void Clear()
            {
                _children.Clear();
            }

            public bool Contains(Item o)
            {
                return _children.Contains(o);
            }

            public bool Contains(object value)
            {
                return ((IList)_children).Contains(value);
            }

            public void CopyTo(Item[] array, int arrayIndex)
            {
                _children.CopyTo(array, arrayIndex);
            }

            public void CopyTo(Array array, int index)
            {
                ((IList)_children).CopyTo(array, index);
            }

            public IEnumerator<Item> GetEnumerator()
            {
                return _children.GetEnumerator();
            }

            public int IndexOf(Item item)
            {
                return _children.IndexOf(item);
            }

            public int IndexOf(object value)
            {
                return ((IList)_children).IndexOf(value);
            }

            public void Insert(int index, Item item)
            {
                _children.Insert(index, item);
            }

            public void Insert(int index, object value)
            {
                ((IList)_children).Insert(index, value);
            }

            public bool Remove(Item item)
            {
                return _children.Remove(item);
            }

            public void Remove(object value)
            {
                ((IList)_children).Remove(value);
            }

            public void RemoveAt(int index)
            {
                _children.RemoveAt(index);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _children.GetEnumerator();
            }
        }

        public class UnnamedUser
        {
        }

        public class NamedUser
        {
            public string Name => "foo";
        }

        public class FooLists
        {
            public static List<T> NewArrayList<T>(IEnumerable<T> iterable)
            {
                return new List<T>(iterable);
            }

            public static List<T> NewArrayList<T>(params object[] elements)
            {
                throw new InvalidOperationException();
            }
        }

        public class DistanceEnforcer
        {
            public static string From(ValueType no)
            {
                return "ValueType:" + no.ToString();
            }

            public static string From(int no)
            {
                return "Integer:" + no.ToString();
            }

            public static string From(object no)
            {
                return "Object:" + no.ToString();
            }
        }
        #endregion Test Types
    }
}
