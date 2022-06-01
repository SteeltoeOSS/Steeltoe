// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Contexts;

public class DictionaryAcessorTests
{
    [Fact]
    public void MapAccessorCompilable()
    {
        var testMap = GetSimpleTestDictionary();
        var sec = new StandardEvaluationContext();
        sec.AddPropertyAccessor(new DictionaryAccessor());
        var sep = new SpelExpressionParser();

        // basic
        var ex = sep.ParseExpression("foo");
        Assert.Equal("bar", ex.GetValue(sec, testMap));

        // assertThat(SpelCompiler.compile(ex)).isTrue();
        // assertThat(ex.getValue(sec, testMap)).isEqualTo("bar");

        // compound expression
        ex = sep.ParseExpression("foo.ToUpper()");
        Assert.Equal("BAR", ex.GetValue(sec, testMap));

        // assertThat(SpelCompiler.compile(ex)).isTrue();
        // assertThat(ex.getValue(sec, testMap)).isEqualTo("BAR");

        // nested map
        var nestedMap = GetNestedTestDictionary();
        ex = sep.ParseExpression("aaa.foo.ToUpper()");
        Assert.Equal("BAR", ex.GetValue(sec, nestedMap));

        // assertThat(SpelCompiler.compile(ex)).isTrue();
        // assertThat(ex.getValue(sec, nestedMap)).isEqualTo("BAR");

        // avoiding inserting checkcast because first part of expression returns a Map
        ex = sep.ParseExpression("Map.foo");
        var mapGetter = new MapGetter();
        Assert.Equal("bar", ex.GetValue(sec, mapGetter));

        // assertThat(SpelCompiler.compile(ex)).isTrue();
        // assertThat(ex.getValue(sec, mapGetter)).isEqualTo("bar");
    }

    private Dictionary<string, object> GetSimpleTestDictionary()
    {
        var map = new Dictionary<string, object>
        {
            { "foo", "bar" }
        };
        return map;
    }

    private Dictionary<string, Dictionary<string, object>> GetNestedTestDictionary()
    {
        var map = new Dictionary<string, object>
        {
            { "foo", "bar" }
        };
        var map2 = new Dictionary<string, Dictionary<string, object>>
        {
            { "aaa", map }
        };
        return map2;
    }

    public class MapGetter
    {
        private Dictionary<string, object> _map = new ();

        public MapGetter()
        {
            _map.Add("foo", "bar");
        }

        public IDictionary Map => _map;
    }
}
