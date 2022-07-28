// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Converter;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Steeltoe.Messaging.RabbitMQ.Support.Converter;

public class JsonMessageConverterTest
{
    private readonly JsonMessageConverter _converter;
    private readonly JsonMessageConverter _jsonConverterWithDefaultType;
    private readonly SimpleTrade _trade;

    public JsonMessageConverterTest()
    {
        _converter = new JsonMessageConverter();
        _trade = new SimpleTrade
        {
            AccountName = "Acct1",
            BuyRequest = true,
            OrderType = "Market",
            Price = 103.30M,
            Quantity = 100,
            RequestId = "R123",
            Ticker = "VMW",
            UserName = "Joe Trader"
        };
        _jsonConverterWithDefaultType = new JsonMessageConverter();
        var classMapper = new DefaultTypeMapper
        {
            DefaultType = typeof(Foo)
        };
        _jsonConverterWithDefaultType.TypeMapper = classMapper;
    }

    [Fact]
    public void SimpleTrade()
    {
        var message = _converter.ToMessage(_trade, new MessageHeaders());
        var marshaledTrade = _converter.FromMessage<SimpleTrade>(message);
        Assert.Equal(_trade, marshaledTrade);
    }

    [Fact]
    public void NestedBean()
    {
        var bar = new Bar { Foo = { Name = "spam" } };

        var message = _converter.ToMessage(bar, new MessageHeaders());

        var marshaled = _converter.FromMessage<Bar>(message);
        Assert.Equal(bar, marshaled);
    }

    [Fact]
    public void Dictionary()
    {
        var hashtable = new Dictionary<string, string>
        {
            { "TICKER", "VMW" },
            { "PRICE", "103.2" }
        };

        var message = _converter.ToMessage(hashtable, new MessageHeaders());
        var marshaledHashtable = _converter.FromMessage<Dictionary<string, string>>(message);

        Assert.Equal("VMW", marshaledHashtable["TICKER"]);
        Assert.Equal("103.2", marshaledHashtable["PRICE"]);
    }

    [Fact]
    public void TestAmqp330StringArray()
    {
        var testData = new[] { "test" };
        var message = _converter.ToMessage(testData, new MessageHeaders());
        var result = _converter.FromMessage<string[]>(message);
        Assert.Single(result);
        Assert.Equal("test", result[0]);
    }

    [Fact]
    public void TestAmqp330ObjectArray()
    {
        var testData = new[] { _trade };
        var message = _converter.ToMessage(testData, new MessageHeaders());
        var result = _converter.FromMessage<SimpleTrade[]>(message);
        Assert.Single(result);
        Assert.Equal(_trade, result[0]);
    }

    [Fact]
    public void TestDefaultType()
    {
        var bytes = Encoding.UTF8.GetBytes("{\"name\" : \"foo\" }");
        var messageProperties = new RabbitHeaderAccessor
        {
            ContentType = "application/json"
        };
        var message = Message.Create(bytes, messageProperties.MessageHeaders);
        var converter = new JsonMessageConverter();
        var classMapper = new DefaultTypeMapper
        {
            DefaultType = typeof(Foo)
        };
        converter.TypeMapper = classMapper;
        var foo = converter.FromMessage(message, null);
        Assert.IsType<Foo>(foo);
    }

    [Fact]
    public void TestDefaultTypeConfig()
    {
        var bytes = Encoding.UTF8.GetBytes("{\"name\" : \"foo\" }");
        var messageProperties = new RabbitHeaderAccessor
        {
            ContentType = "application/json"
        };
        var message = Message.Create(bytes, messageProperties.MessageHeaders);
        var foo = _jsonConverterWithDefaultType.FromMessage(message, null);
        Assert.IsType<Foo>(foo);
    }

    [Fact]
    public void TestNoJsonContentType()
    {
        var bytes = Encoding.UTF8.GetBytes("{\"name\" : \"foo\" }");
        var messageProperties = new MessageHeaders();
        var message = Message.Create(bytes, messageProperties);
        _jsonConverterWithDefaultType.AssumeSupportedContentType = false;
        var foo = _jsonConverterWithDefaultType.FromMessage(message, null);
        Assert.IsType<byte[]>(foo);
    }

    [Fact(Skip = "Need to handle nested dictionaries")]
    public void TestNoTypeInfo()
    {
        var bytes = Encoding.UTF8.GetBytes("{\"name\" : { \"foo\" : \"bar\" } }");
        var messageProperties = new RabbitHeaderAccessor
        {
            ContentType = "application/json"
        };
        var message = Message.Create(bytes, messageProperties.MessageHeaders);
        var foo = _converter.FromMessage(message, null);
        Assert.IsType<Dictionary<object, object>>(foo);
        var fooDict = foo as Dictionary<object, object>;
        var nameObj = fooDict["name"];
        Assert.NotNull(nameObj);
        Assert.IsType<Dictionary<object, object>>(nameObj);
    }

    [Fact]
    public void TestInferredTypeInfo()
    {
        var bytes = Encoding.UTF8.GetBytes("{\"name\" : \"foo\" }");
        var messageProperties = new RabbitHeaderAccessor
        {
            ContentType = "application/json",
            InferredArgumentType = typeof(Foo)
        };
        var message = Message.Create(bytes, messageProperties.MessageHeaders);
        var foo = _converter.FromMessage(message, null);
        Assert.IsType<Foo>(foo);
    }

    [Fact]
    public void TestInferredGenericTypeInfo()
    {
        var bytes = Encoding.UTF8.GetBytes("[ {\"name\" : \"foo\" } ]");
        var messageProperties = new RabbitHeaderAccessor
        {
            ContentType = "application/json",
            InferredArgumentType = typeof(List<Foo>)
        };
        var message = Message.Create(bytes, messageProperties.MessageHeaders);
        var foo = _converter.FromMessage(message, null);
        Assert.IsType<List<Foo>>(foo);
        var asList = foo as List<Foo>;
        Assert.NotNull(asList[0]);
    }

    [Fact]
    public void TestInferredGenericMap1()
    {
        var bytes = Encoding.UTF8.GetBytes("{\"qux\" : [ { \"foo\" : { \"name\" : \"bar\" } } ] }");
        var messageProperties = new RabbitHeaderAccessor
        {
            ContentType = "application/json",
            InferredArgumentType = typeof(Dictionary<string, List<Bar>>)
        };
        var message = Message.Create(bytes, messageProperties.MessageHeaders);
        var foo = _converter.FromMessage(message, null);
        Assert.IsType<Dictionary<string, List<Bar>>>(foo);
        var dict = foo as Dictionary<string, List<Bar>>;
        var list = dict["qux"];
        Assert.NotNull(list);
        Assert.IsType<Bar>(list[0]);
    }

    [Fact]
    public void TestInferredGenericMap2()
    {
        var bytes = Encoding.UTF8.GetBytes("{\"qux\" : { \"baz\" : { \"foo\" : { \"name\" : \"bar\" } } } }");
        var messageProperties = new RabbitHeaderAccessor
        {
            ContentType = "application/json",
            InferredArgumentType = typeof(Dictionary<string, Dictionary<string, Bar>>)
        };
        var message = Message.Create(bytes, messageProperties.MessageHeaders);
        var foo = _converter.FromMessage(message, null);
        Assert.IsType<Dictionary<string, Dictionary<string, Bar>>>(foo);
        var dict = foo as Dictionary<string, Dictionary<string, Bar>>;
        var dict2 = dict["qux"];
        Assert.NotNull(dict2);
        Assert.IsType<Bar>(dict2["baz"]);
    }

    [Fact]
    public void TestMissingContentType()
    {
        var bytes = Encoding.UTF8.GetBytes("{\"name\" : \"foo\" }");
        var messageProperties = new MessageHeaders();
        var message = Message.Create(bytes, messageProperties);
        var j2Converter = new JsonMessageConverter();
        var typeMapper = new DefaultTypeMapper
        {
            DefaultType = typeof(Foo)
        };
        j2Converter.TypeMapper = typeMapper;
        var foo = j2Converter.FromMessage(message, null);
        Assert.IsType<Foo>(foo);
        var accessor = RabbitHeaderAccessor.GetMutableAccessor(messageProperties);
        accessor.ContentType = null;

        foo = j2Converter.FromMessage(message, null);
        Assert.IsType<Foo>(foo);
        j2Converter.AssumeSupportedContentType = false;
        foo = j2Converter.FromMessage(message, null);
        Assert.Same(foo, bytes);
    }

    public class Foo
    {
        public string Name { get; set; } = "foo";

        public Foo()
        {
        }

        public Foo(string name)
        {
            Name = name;
        }

        public override int GetHashCode()
        {
            return Name?.GetHashCode() ?? 0;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is not Foo other || GetType() != obj.GetType())
            {
                return false;
            }

            return Name == other.Name;
        }
    }

    public class Bar
    {
        public string Name { get; set; } = "bar";

        public Foo Foo { get; set; } = new ();

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Foo);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is not Bar other || GetType() != obj.GetType())
            {
                return false;
            }

            return Name == other.Name && Equals(Foo, other.Foo);
        }
    }
}
