// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.ObjectModel;
using Steeltoe.Common.Expression.Internal.Spring.Ast;
using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Spring;

public class MapTests : AbstractExpressionTests
{
    // if the list is full of literals then it will be of the type unmodifiableClass
    // rather than HashMap (or similar)
    private readonly Type _unmodifiableClass = typeof(ReadOnlyDictionary<object, object>);

    [Fact]
    public void TestInlineMapCreation01()
    {
        Evaluate("{'a':1, 'b':2, 'c':3, 'd':4, 'e':5}", "{a=1,b=2,c=3,d=4,e=5}", _unmodifiableClass);
        Evaluate("{'a':1}", "{a=1}", _unmodifiableClass);
    }

    [Fact]
    public void TestInlineMapCreation02()
    {
        Evaluate("{'abc':'def', 'uvw':'xyz'}", "{abc=def,uvw=xyz}", _unmodifiableClass);
    }

    [Fact]
    public void TestInlineMapCreation03()
    {
        Evaluate("{:}", "{}", _unmodifiableClass);
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
        Evaluate("{a:{a:1,b:2,c:3},b:{d:4,e:5,f:6}}", "{a={a=1,b=2,c=3},b={d=4,e=5,f=6}}", _unmodifiableClass);
        Evaluate("{a:{x:1,y:'2',z:3},b:{u:4,v:{'a','b'},w:5,x:6}}", "{a={x=1,y=2,z=3},b={u=4,v=[a,b],w=5,x=6}}", _unmodifiableClass);
        Evaluate("{a:{1,2,3},b:{4,5,6}}", "{a=[1,2,3],b=[4,5,6]}", _unmodifiableClass);
    }

    [Fact]
    public void TestInlineMapWithFunkyKeys()
    {
        Evaluate("{#root.Name:true}", "{Nikola Tesla=True}", typeof(Dictionary<object, object>));
    }

    [Fact]
    public void TestInlineMapError()
    {
        ParseAndCheckError("{key:'abc'", SpelMessage.Ood);
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
        var expected = new Hashtable
        {
            { "a", "a" },
            { "b", "b" },
            { "c", "c" }
        };

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
        Assert.Throws<NotSupportedException>(() => Evaluate("{a:1, b:2, c:3, d:4, e:5}[a]=6", "[a:1,b: 2,c: 3,d: 4,e: 5]", _unmodifiableClass));
    }

    [Fact]
    public void TestMapKeysThatAreAlsoSpelKeywords()
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
        ISpelNode node = expression.Ast;
        bool condition = node is InlineMap;
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
        public IDictionary Foo { get; }

        public MapHolder()
        {
            Foo = new Dictionary<string, string>
            {
                { "NEW", "VALUE" },
                { "new", "value" },
                { "T", "TV" },
                { "t", "tv" },
                { "abc.def", "value" },
                { "VALUE", "37" },
                { "value", "38" },
                { "TV", "new" }
            };
        }
    }
}
