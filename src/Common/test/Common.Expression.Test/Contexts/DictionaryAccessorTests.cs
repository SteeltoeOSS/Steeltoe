// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;
using Steeltoe.Common.Expression.Internal;
using Steeltoe.Common.Expression.Internal.Contexts;
using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using Xunit;

namespace Steeltoe.Common.Expression.Test.Contexts;

public class DictionaryAccessorTests
{
    [Fact]
    public void MapAccessorCompilable()
    {
        Dictionary<string, object> testMap = GetSimpleTestDictionary();
        var sec = new StandardEvaluationContext();
        sec.AddPropertyAccessor(new DictionaryAccessor());
        var sep = new SpelExpressionParser();

        // basic
        IExpression ex = sep.ParseExpression("foo");
        Assert.Equal("bar", ex.GetValue(sec, testMap));

        // compound expression
        ex = sep.ParseExpression("foo.ToUpperInvariant()");
        Assert.Equal("BAR", ex.GetValue(sec, testMap));

        // nested map
        Dictionary<string, Dictionary<string, object>> nestedMap = GetNestedTestDictionary();
        ex = sep.ParseExpression("aaa.foo.ToUpperInvariant()");
        Assert.Equal("BAR", ex.GetValue(sec, nestedMap));

        // avoiding inserting checkcast because first part of expression returns a Map
        ex = sep.ParseExpression("Map.foo");
        var mapGetter = new MapGetter();
        Assert.Equal("bar", ex.GetValue(sec, mapGetter));
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
        private readonly Dictionary<string, object> _map = new();

        public IDictionary Map => _map;

        public MapGetter()
        {
            _map.Add("foo", "bar");
        }
    }
}
