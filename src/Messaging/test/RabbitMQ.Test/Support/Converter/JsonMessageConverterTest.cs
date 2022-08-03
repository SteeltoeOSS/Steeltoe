// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Steeltoe.Messaging.Converter;
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
        IMessage message = _converter.ToMessage(_trade, new MessageHeaders());
        var marshaledTrade = _converter.FromMessage<SimpleTrade>(message);
        Assert.Equal(_trade, marshaledTrade);
    }

    [Fact]
    public void NestedBean()
    {
        var bar = new Bar
        {
            Foo =
            {
                Name = "spam"
            }
        };

        IMessage message = _converter.ToMessage(bar, new MessageHeaders());

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

        IMessage message = _converter.ToMessage(hashtable, new MessageHeaders());
        var marshaledHashtable = _converter.FromMessage<Dictionary<string, string>>(message);

        Assert.Equal("VMW", marshaledHashtable["TICKER"]);
        Assert.Equal("103.2", marshaledHashtable["PRICE"]);
    }

    [Fact]
    public void TestAmqp330StringArray()
    {
        string[] testData =
        {
            "test"
        };

        IMessage message = _converter.ToMessage(testData, new MessageHeaders());
        string[] result = _converter.FromMessage<string[]>(message);
        Assert.Single(result);
        Assert.Equal("test", result[0]);
    }

    [Fact]
    public void TestAmqp330ObjectArray()
    {
        SimpleTrade[] testData =
        {
            _trade
        };

        IMessage message = _converter.ToMessage(testData, new MessageHeaders());
        SimpleTrade[] result = _converter.FromMessage<SimpleTrade[]>(message);
        Assert.Single(result);
        Assert.Equal(_trade, result[0]);
    }

    [Fact]
    public void TestDefaultType()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("{\"name\" : \"foo\" }");

        var messageProperties = new RabbitHeaderAccessor
        {
            ContentType = "application/json"
        };

        IMessage<byte[]> message = Message.Create(bytes, messageProperties.MessageHeaders);
        var converter = new JsonMessageConverter();

        var classMapper = new DefaultTypeMapper
        {
            DefaultType = typeof(Foo)
        };

        converter.TypeMapper = classMapper;
        object foo = converter.FromMessage(message, null);
        Assert.IsType<Foo>(foo);
    }

    [Fact]
    public void TestDefaultTypeConfig()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("{\"name\" : \"foo\" }");

        var messageProperties = new RabbitHeaderAccessor
        {
            ContentType = "application/json"
        };

        IMessage<byte[]> message = Message.Create(bytes, messageProperties.MessageHeaders);
        object foo = _jsonConverterWithDefaultType.FromMessage(message, null);
        Assert.IsType<Foo>(foo);
    }

    [Fact]
    public void TestNoJsonContentType()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("{\"name\" : \"foo\" }");
        var messageProperties = new MessageHeaders();
        IMessage<byte[]> message = Message.Create(bytes, messageProperties);
        _jsonConverterWithDefaultType.AssumeSupportedContentType = false;
        object foo = _jsonConverterWithDefaultType.FromMessage(message, null);
        Assert.IsType<byte[]>(foo);
    }

    [Fact(Skip = "Need to handle nested dictionaries")]
    public void TestNoTypeInfo()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("{\"name\" : { \"foo\" : \"bar\" } }");

        var messageProperties = new RabbitHeaderAccessor
        {
            ContentType = "application/json"
        };

        IMessage<byte[]> message = Message.Create(bytes, messageProperties.MessageHeaders);
        object foo = _converter.FromMessage(message, null);
        Assert.IsType<Dictionary<object, object>>(foo);
        var fooDict = foo as Dictionary<object, object>;
        object nameObj = fooDict["name"];
        Assert.NotNull(nameObj);
        Assert.IsType<Dictionary<object, object>>(nameObj);
    }

    [Fact]
    public void TestInferredTypeInfo()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("{\"name\" : \"foo\" }");

        var messageProperties = new RabbitHeaderAccessor
        {
            ContentType = "application/json",
            InferredArgumentType = typeof(Foo)
        };

        IMessage<byte[]> message = Message.Create(bytes, messageProperties.MessageHeaders);
        object foo = _converter.FromMessage(message, null);
        Assert.IsType<Foo>(foo);
    }

    [Fact]
    public void TestInferredGenericTypeInfo()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("[ {\"name\" : \"foo\" } ]");

        var messageProperties = new RabbitHeaderAccessor
        {
            ContentType = "application/json",
            InferredArgumentType = typeof(List<Foo>)
        };

        IMessage<byte[]> message = Message.Create(bytes, messageProperties.MessageHeaders);
        object foo = _converter.FromMessage(message, null);
        Assert.IsType<List<Foo>>(foo);
        var asList = foo as List<Foo>;
        Assert.NotNull(asList[0]);
    }

    [Fact]
    public void TestInferredGenericMap1()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("{\"qux\" : [ { \"foo\" : { \"name\" : \"bar\" } } ] }");

        var messageProperties = new RabbitHeaderAccessor
        {
            ContentType = "application/json",
            InferredArgumentType = typeof(Dictionary<string, List<Bar>>)
        };

        IMessage<byte[]> message = Message.Create(bytes, messageProperties.MessageHeaders);
        object foo = _converter.FromMessage(message, null);
        Assert.IsType<Dictionary<string, List<Bar>>>(foo);
        var dict = foo as Dictionary<string, List<Bar>>;
        List<Bar> list = dict["qux"];
        Assert.NotNull(list);
        Assert.IsType<Bar>(list[0]);
    }

    [Fact]
    public void TestInferredGenericMap2()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("{\"qux\" : { \"baz\" : { \"foo\" : { \"name\" : \"bar\" } } } }");

        var messageProperties = new RabbitHeaderAccessor
        {
            ContentType = "application/json",
            InferredArgumentType = typeof(Dictionary<string, Dictionary<string, Bar>>)
        };

        IMessage<byte[]> message = Message.Create(bytes, messageProperties.MessageHeaders);
        object foo = _converter.FromMessage(message, null);
        Assert.IsType<Dictionary<string, Dictionary<string, Bar>>>(foo);
        var dict = foo as Dictionary<string, Dictionary<string, Bar>>;
        Dictionary<string, Bar> dict2 = dict["qux"];
        Assert.NotNull(dict2);
        Assert.IsType<Bar>(dict2["baz"]);
    }

    [Fact]
    public void TestMissingContentType()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("{\"name\" : \"foo\" }");
        var messageProperties = new MessageHeaders();
        IMessage<byte[]> message = Message.Create(bytes, messageProperties);
        var j2Converter = new JsonMessageConverter();

        var typeMapper = new DefaultTypeMapper
        {
            DefaultType = typeof(Foo)
        };

        j2Converter.TypeMapper = typeMapper;
        object foo = j2Converter.FromMessage(message, null);
        Assert.IsType<Foo>(foo);
        RabbitHeaderAccessor accessor = RabbitHeaderAccessor.GetMutableAccessor(messageProperties);
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

        public Foo Foo { get; set; } = new();

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
