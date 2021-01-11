// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Ast;
using Steeltoe.Common.Expression.Internal.Spring.Standard;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Spring
{
    public class MapTests : AbstractExpressionTests
    {
        // if the list is full of literals then it will be of the type unmodifiableClass
        // rather than HashMap (or similar)
        private Type unmodifiableClass = typeof(ReadOnlyDictionary<object, object>);

        [Fact]
        public void TestInlineMapCreation01()
        {
            Evaluate("{'a':1, 'b':2, 'c':3, 'd':4, 'e':5}", "{a=1,b=2,c=3,d=4,e=5}", unmodifiableClass);
            Evaluate("{'a':1}", "{a=1}", unmodifiableClass);
        }

        [Fact]
        public void TestInlineMapCreation02()
        {
            Evaluate("{'abc':'def', 'uvw':'xyz'}", "{abc=def,uvw=xyz}", unmodifiableClass);
        }

        [Fact]
        public void TestInlineMapCreation03()
        {
            Evaluate("{:}", "{}", unmodifiableClass);
        }

        [Fact]
        public void TestInlineMapCreation04()
        {
            Evaluate("{'key':'abc'=='xyz'}", "{key=False}", typeof(Dictionary<object, object>));
            Evaluate("{key:'abc'=='xyz'}", "{key=False}", typeof(Dictionary<object, object>));
            Evaluate("{key:'abc'=='xyz',key2:true}[key]", "False", typeof(bool));

            // TODO: No Get() method Evaluate("{key:'abc'=='xyz',key2:true}.get('key2')", "True", typeof(bool));
            Evaluate("{key:'abc'=='xyz',key2:true}['key2']", "True", typeof(bool));
        }

        [Fact]
        public void TestInlineMapAndNesting()
        {
            Evaluate("{a:{a:1,b:2,c:3},b:{d:4,e:5,f:6}}", "{a={a=1,b=2,c=3},b={d=4,e=5,f=6}}", unmodifiableClass);
            Evaluate("{a:{x:1,y:'2',z:3},b:{u:4,v:{'a','b'},w:5,x:6}}", "{a={x=1,y=2,z=3},b={u=4,v=[a,b],w=5,x=6}}", unmodifiableClass);
            Evaluate("{a:{1,2,3},b:{4,5,6}}", "{a=[1,2,3],b=[4,5,6]}", unmodifiableClass);
        }

        [Fact]
        public void TestInlineMapWithFunkyKeys()
        {
            Evaluate("{#root.Name:true}", "{Nikola Tesla=True}", typeof(Dictionary<object, object>));
        }

        [Fact]
        public void TestInlineMapError()
        {
            ParseAndCheckError("{key:'abc'", SpelMessage.OOD);
        }

        [Fact]
        public void TestRelOperatorsIs02()
        {
            Evaluate("{a:1, b:2, c:3, d:4, e:5} instanceof T(System.Collections.IDictionary)", "True", typeof(bool));
        }

        [Fact]
        public void TestInlineMapAndProjectionSelection()
        {
            Evaluate("{a:1,b:2,c:3,d:4,e:5,f:6}.![Value>3]", "[False,False,False,True,True,True]", typeof(List<object>));
            Evaluate("{a:1,b:2,c:3,d:4,e:5,f:6}.?[Value>3]", "{d=4,e=5,f=6}", typeof(Dictionary<object, object>));
            Evaluate("{a:1,b:2,c:3,d:4,e:5,f:6,g:7,h:8,i:9,j:10}.?[Value%2==0]", "{b=2,d=4,f=6,h=8,j=10}", typeof(Dictionary<object, object>));

            // TODO this looks like a serious issue (but not a new one): the context object against which arguments are Evaluated seems wrong:
            // Evaluate("{a:1,b:2,c:3,d:4,e:5,f:6,g:7,h:8,i:9,j:10}.?[isEven(value) == 'y']", "[2, 4, 6, 8, 10]", typeof(ArrayList));
        }

        [Fact]
        public void TestSetConstruction01()
        {
            var expected = new Hashtable();
            expected.Add("a", "a");
            expected.Add("b", "b");
            expected.Add("c", "c");
            Evaluate("new System.Collections.Hashtable({a:'a',b:'b',c:'c'})", expected, typeof(Hashtable));
        }

        [Fact]
        public void TestConstantRepresentation1()
        {
            CheckConstantMap("{f:{'a','b','c'}}", true);
            CheckConstantMap("{'a':1,'b':2,'c':3,'d':4,'e':5}", true);
            CheckConstantMap("{aaa:'abc'}", true);
            CheckConstantMap("{:}", true);
            CheckConstantMap("{a:#a,b:2,c:3}", false);
            CheckConstantMap("{a:1,b:2,c:Integer.valueOf(4)}", false);
            CheckConstantMap("{a:1,b:2,c:{#a}}", false);
            CheckConstantMap("{#root.name:true}", false);
            CheckConstantMap("{a:1,b:2,c:{d:true,e:false}}", true);
            CheckConstantMap("{a:1,b:2,c:{d:{1,2,3},e:{4,5,6},f:{'a','b','c'}}}", true);
        }

        [Fact]
        public void TestInlineMapWriting()
        {
            // list should be unmodifiable
            Assert.Throws<NotSupportedException>(() => Evaluate("{a:1, b:2, c:3, d:4, e:5}[a]=6", "[a:1,b: 2,c: 3,d: 4,e: 5]", unmodifiableClass));
        }

        [Fact]
        public void TestMapKeysThatAreAlsoSpELKeywords()
        {
            var parser = new SpelExpressionParser();
            SpelExpression expression = null;
            object o = null;

            expression = (SpelExpression)parser.ParseExpression("Foo[T]");
            o = expression.GetValue(new MapHolder());
            Assert.Equal("TV", o);

            expression = (SpelExpression)parser.ParseExpression("Foo[t]");
            o = expression.GetValue(new MapHolder());
            Assert.Equal("tv", o);

            expression = (SpelExpression)parser.ParseExpression("Foo[NEW]");
            o = expression.GetValue(new MapHolder());
            Assert.Equal("VALUE", o);

            expression = (SpelExpression)parser.ParseExpression("Foo[new]");
            o = expression.GetValue(new MapHolder());
            Assert.Equal("value", o);

            expression = (SpelExpression)parser.ParseExpression("Foo['abc.def']");
            o = expression.GetValue(new MapHolder());
            Assert.Equal("value", o);

            expression = (SpelExpression)parser.ParseExpression("Foo[Foo[NEW]]");
            o = expression.GetValue(new MapHolder());
            Assert.Equal("37", o);

            expression = (SpelExpression)parser.ParseExpression("Foo[Foo[new]]");
            o = expression.GetValue(new MapHolder());
            Assert.Equal("38", o);

            expression = (SpelExpression)parser.ParseExpression("Foo[Foo[Foo[T]]]");
            o = expression.GetValue(new MapHolder());
            Assert.Equal("value", o);
        }

        private void CheckConstantMap(string expressionText, bool expectedToBeConstant)
        {
            var parser = new SpelExpressionParser();
            var expression = (SpelExpression)parser.ParseExpression(expressionText);
            var node = expression.AST;
            var condition = node is InlineMap;
            Assert.True(condition);
            var inlineMap = (InlineMap)node;
            if (expectedToBeConstant)
            {
                Assert.True(inlineMap.IsConstant);
            }
            else
            {
                Assert.False(inlineMap.IsConstant);
            }
        }

        public class MapHolder
        {
            public IDictionary Foo;

            public MapHolder()
            {
                Foo = new Dictionary<string, string>();
                Foo.Add("NEW", "VALUE");
                Foo.Add("new", "value");
                Foo.Add("T", "TV");
                Foo.Add("t", "tv");
                Foo.Add("abc.def", "value");
                Foo.Add("VALUE", "37");
                Foo.Add("value", "38");
                Foo.Add("TV", "new");
            }
        }
    }
}
