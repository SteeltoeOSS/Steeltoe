// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Steeltoe.Messaging.Rabbit.Support.Converter
{
    public class JsonMessageConverterTest
    {
        private JsonMessageConverter converter;
        private JsonMessageConverter jsonConverterWithDefaultType;
        private SimpleTrade trade;

        public JsonMessageConverterTest()
        {
            converter = new JsonMessageConverter();
            trade = new SimpleTrade();
            trade.AccountName = "Acct1";
            trade.BuyRequest = true;
            trade.OrderType = "Market";
            trade.Price = 103.30M;
            trade.Quantity = 100;
            trade.RequestId = "R123";
            trade.Ticker = "VMW";
            trade.UserName = "Joe Trader";
            jsonConverterWithDefaultType = new JsonMessageConverter();
            var classMapper = new DefaultTypeMapper();
            classMapper.DefaultType = typeof(Foo);
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
            var hashtable = new Dictionary<string, string>();
            hashtable.Add("TICKER", "VMW");
            hashtable.Add("PRICE", "103.2");

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
            var messageProperties = new RabbitHeaderAccessor();
            messageProperties.ContentType = "application/json";
            var message = Message.Create(bytes, messageProperties.MessageHeaders);
            var converter = new JsonMessageConverter();
            var classMapper = new DefaultTypeMapper();
            classMapper.DefaultType = typeof(Foo);
            converter.TypeMapper = classMapper;
            var foo = converter.FromMessage(message, null);
            Assert.IsType<Foo>(foo);
        }

        [Fact]
        public void TestDefaultTypeConfig()
        {
            var bytes = Encoding.UTF8.GetBytes("{\"name\" : \"foo\" }");
            var messageProperties = new RabbitHeaderAccessor();
            messageProperties.ContentType = "application/json";
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
            byte[] bytes = Encoding.UTF8.GetBytes("{\"name\" : { \"foo\" : \"bar\" } }");
            var messageProperties = new RabbitHeaderAccessor();
            messageProperties.ContentType = "application/json";
            var message = Message.Create(bytes, messageProperties.MessageHeaders);
            var foo = this.converter.FromMessage(message, null);
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
            var messageProperties = new RabbitHeaderAccessor();
            messageProperties.ContentType = "application/json";
            messageProperties.InferredArgumentType = typeof(Foo);
            var message = Message.Create(bytes, messageProperties.MessageHeaders);
            var foo = converter.FromMessage(message, null);
            Assert.IsType<Foo>(foo);
        }

        [Fact]
        public void TestInferredGenericTypeInfo()
        {
            var bytes = Encoding.UTF8.GetBytes("[ {\"name\" : \"foo\" } ]");
            var messageProperties = new RabbitHeaderAccessor();
            messageProperties.ContentType = "application/json";
            messageProperties.InferredArgumentType = typeof(List<Foo>);
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
            var messageProperties = new RabbitHeaderAccessor();
            messageProperties.ContentType = "application/json";
            messageProperties.InferredArgumentType = typeof(Dictionary<string, List<Bar>>);
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
            var messageProperties = new RabbitHeaderAccessor();
            messageProperties.ContentType = "application/json";
            messageProperties.InferredArgumentType = typeof(Dictionary<string, Dictionary<string, Bar>>);
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
            var typeMapper = new DefaultTypeMapper();
            typeMapper.DefaultType = typeof(Foo);
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
                int prime = 31;
                int result = 1;
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
