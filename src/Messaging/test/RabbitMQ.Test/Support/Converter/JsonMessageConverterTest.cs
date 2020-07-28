// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Converter;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Steeltoe.Messaging.Rabbit.Support.Converter
{
    public class JsonMessageConverterTest
    {
        private readonly JsonMessageConverter converter;
        private readonly JsonMessageConverter jsonConverterWithDefaultType;
        private readonly SimpleTrade trade;

        public JsonMessageConverterTest()
        {
            converter = new JsonMessageConverter();
            trade = new SimpleTrade
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
            jsonConverterWithDefaultType = new JsonMessageConverter();
            var classMapper = new DefaultTypeMapper
            {
                DefaultType = typeof(Foo)
            };
            jsonConverterWithDefaultType.TypeMapper = classMapper;
        }

        [Fact]
        public void SimpleTrade()
        {
            var message = converter.ToMessage(trade, new MessageHeaders());
            var marshalledTrade = converter.FromMessage<SimpleTrade>(message);
            Assert.Equal(trade, marshalledTrade);
        }

        [Fact]
        public void NestedBean()
        {
            var bar = new Bar();
            bar.Foo.Name = "spam";

            var message = converter.ToMessage(bar, new MessageHeaders());

            var marshalled = converter.FromMessage<Bar>(message);
            Assert.Equal(bar, marshalled);
        }

        [Fact]
        public void Dictionary()
        {
            var hashtable = new Dictionary<string, string>
            {
                { "TICKER", "VMW" },
                { "PRICE", "103.2" }
            };

            var message = converter.ToMessage(hashtable, new MessageHeaders());
            var marhsalledHashtable = converter.FromMessage<Dictionary<string, string>>(message);

            Assert.Equal("VMW", marhsalledHashtable["TICKER"]);
            Assert.Equal("103.2", marhsalledHashtable["PRICE"]);
        }

        [Fact]
        public void TestAmqp330StringArray()
        {
            var testData = new string[] { "test" };
            var message = converter.ToMessage(testData, new MessageHeaders());
            var result = converter.FromMessage<string[]>(message);
            Assert.Single(result);
            Assert.Equal("test", result[0]);
        }

        [Fact]
        public void TestAmqp330ObjectArray()
        {
            var testData = new SimpleTrade[] { trade };
            var message = converter.ToMessage(testData, new MessageHeaders());
            var result = converter.FromMessage<SimpleTrade[]>(message);
            Assert.Single(result);
            Assert.Equal(trade, result[0]);
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
            var foo = jsonConverterWithDefaultType.FromMessage(message, null);
            Assert.IsType<Foo>(foo);
        }

        [Fact]
        public void TestNoJsonContentType()
        {
            var bytes = Encoding.UTF8.GetBytes("{\"name\" : \"foo\" }");
            var messageProperties = new MessageHeaders();
            var message = Message.Create(bytes, messageProperties);
            jsonConverterWithDefaultType.AssumeSupportedContentType = false;
            var foo = jsonConverterWithDefaultType.FromMessage(message, null);
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
            var foo = converter.FromMessage(message, null);
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
            var foo = converter.FromMessage(message, null);
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
            var foo = converter.FromMessage(message, null);
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
            var foo = converter.FromMessage(message, null);
            Assert.IsType<Dictionary<string, List<Bar>>>(foo);
            var dict = foo as Dictionary<string, List<Bar>>;
            var list = dict["qux"] as List<Bar>;
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
            var foo = converter.FromMessage(message, null);
            Assert.IsType<Dictionary<string, Dictionary<string, Bar>>>(foo);
            var dict = foo as Dictionary<string, Dictionary<string, Bar>>;
            var dict2 = dict["qux"] as Dictionary<string, Bar>;
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
                var prime = 31;
                var result = 1;
                result = (prime * result) + ((Name == null) ? 0 : Name.GetHashCode());
                return result;
            }

            public override bool Equals(object obj)
            {
                if (this == obj)
                {
                    return true;
                }

                if (obj == null)
                {
                    return false;
                }

                if (GetType() != obj.GetType())
                {
                    return false;
                }

                var other = (Foo)obj;
                if (Name == null)
                {
                    if (other.Name != null)
                    {
                        return false;
                    }
                }
                else if (!Name.Equals(other.Name))
                {
                    return false;
                }

                return true;
            }
        }

        public class Bar
        {
            public string Name { get; set; } = "bar";

            public Foo Foo { get; set; } = new Foo();

            public override int GetHashCode()
            {
                var prime = 31;
                var result = 1;
                result = (prime * result) + ((Foo == null) ? 0 : Foo.GetHashCode());
                result = (prime * result) + ((Name == null) ? 0 : Name.GetHashCode());
                return result;
            }

            public override bool Equals(object obj)
            {
                if (this == obj)
                {
                    return true;
                }

                if (obj == null)
                {
                    return false;
                }

                if (GetType() != obj.GetType())
                {
                    return false;
                }

                var other = (Bar)obj;
                if (Foo == null)
                {
                    if (other.Foo != null)
                    {
                        return false;
                    }
                }
                else if (!Foo.Equals(other.Foo))
                {
                    return false;
                }

                if (Name == null)
                {
                    if (other.Name != null)
                    {
                        return false;
                    }
                }
                else if (!Name.Equals(other.Name))
                {
                    return false;
                }

                return true;
            }
        }
    }
}
