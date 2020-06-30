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

using Moq;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Support;
using System;
using System.Reflection;
using Xunit;

namespace Steeltoe.Messaging.Handler.Attributes.Support.Test
{
    public class MessageMethodArgumentResolverTest
    {
        private readonly IMessageConverter converter;

        private readonly MethodInfo method;

        private readonly Mock<IMessageConverter> mock;

        private MessageMethodArgumentResolver resolver;

        public MessageMethodArgumentResolverTest()
        {
            method = typeof(MessageMethodArgumentResolverTest)
                .GetMethod(
                "Handle",
                BindingFlags.NonPublic | BindingFlags.Instance);

            mock = new Mock<IMessageConverter>();
            converter = mock.Object;
            resolver = new MessageMethodArgumentResolver(converter);
        }

        [Fact]
        public void ResolveWithPayloadTypeAsObject()
        {
            var message = MessageBuilder.WithPayload("test").Build();
            var parameter = method.GetParameters()[0];

            Assert.True(resolver.SupportsParameter(parameter));
            Assert.Same(message, resolver.ResolveArgument(parameter, message));
        }

        [Fact]
        public void ResolveWithMatchingPayloadType()
        {
            var message = MessageBuilder.WithPayload(123).Build();
            var parameter = method.GetParameters()[1];

            Assert.True(resolver.SupportsParameter(parameter));
            Assert.Same(message, resolver.ResolveArgument(parameter, message));
        }

        [Fact]
        public void ResolveWithConversion()
        {
            var message = MessageBuilder.WithPayload("test").Build();
            var parameter = method.GetParameters()[1];
            mock.Setup((c) => c.FromMessage(message, typeof(int))).Returns(4);

            var actual = (IMessage)resolver.ResolveArgument(parameter, message);

            Assert.NotNull(actual);
            Assert.Equal(message.Headers, actual.Headers);
            Assert.Equal(4, actual.Payload);
        }

        [Fact]
        public void ResolveWithConversionNoMatchingConverter()
        {
            var message = MessageBuilder.WithPayload("test").Build();
            var parameter = method.GetParameters()[1];

            Assert.True(resolver.SupportsParameter(parameter));
            var ex = Assert.Throws<MessageConversionException>(() => resolver.ResolveArgument(parameter, message));
            Assert.Contains("Int32", ex.Message);
            Assert.Contains("String", ex.Message);
        }

        [Fact]
        public void ResolveWithConversionEmptyPayload()
        {
            var message = MessageBuilder.WithPayload(string.Empty).Build();
            var parameter = method.GetParameters()[1];
            Assert.True(resolver.SupportsParameter(parameter));
            var ex = Assert.Throws<MessageConversionException>(() => resolver.ResolveArgument(parameter, message));
            Assert.Contains("payload is empty", ex.Message);
            Assert.Contains("Int32", ex.Message);
            Assert.Contains("String", ex.Message);
        }

        // [Fact]
        // public void ResolveWithPayloadTypeUpperBound()
        // {
        //    IMessage<int> message = MessageBuilder<int>.WithPayload(123).Build();
        //    ParameterInfo parameter = this.method.GetParameters()[3];

        // Assert.True(this.resolver.SupportsParameter(parameter));
        //    Assert.Same(message, this.resolver.ResolveArgument(parameter, message));
        // }

        // [Fact]
        // public void ResolveWithPayloadTypeOutOfBound()
        // {
        //    IMessage<CultureInfo> message = MessageBuilder<CultureInfo>.WithPayload(CultureInfo.CurrentCulture).Build();
        //    ParameterInfo parameter = this.method.GetParameters()[3];

        // Assert.True(this.resolver.SupportsParameter(parameter));
        //    var ex = Assert.Throws<MessageConversionException>(() => this.resolver.ResolveArgument(parameter, message));
        //    Assert.Contains("Number", ex.Message);
        //    Assert.Contains("CultureInfo", ex.Message);
        // }
        [Fact]
        public void ResolveMessageSubclassMatch()
        {
            var message = new ErrorMessage(new InvalidOperationException());
            var parameter = method.GetParameters()[4];

            Assert.True(resolver.SupportsParameter(parameter));
            Assert.Same(message, resolver.ResolveArgument(parameter, message));
        }

        [Fact]
        public void ResolveWithMessageSubclassAndPayloadWildcard()
        {
            var message = new ErrorMessage(new InvalidOperationException());
            var parameter = method.GetParameters()[0];
            Assert.True(resolver.SupportsParameter(parameter));
            Assert.Same(message, resolver.ResolveArgument(parameter, message));
        }

        // [Fact]
        // public void ResolveWithWrongMessageType()
        // {
        //    var ex1 = new InvalidOperationException();
        //    IMessage<Exception> message = GenericMessage.Create<Exception>(ex1);
        //    ParameterInfo parameter = this.method.GetParameters()[0];

        // Assert.True(this.resolver.SupportsParameter(parameter));
        //    var ex = Assert.Throws<MethodArgumentTypeMismatchException>(() => this.resolver.ResolveArgument(parameter, message));
        //    Assert.Contains("ErrorMessage", ex.Message);
        //    Assert.Contains("GenericMessage", ex.Message);
        // }
        [Fact]
        public void ResolveWithPayloadTypeAsWildcardAndNoConverter()
        {
            resolver = new MessageMethodArgumentResolver();

            var message = MessageBuilder.WithPayload("test").Build();
            var parameter = method.GetParameters()[0];
            Assert.True(resolver.SupportsParameter(parameter));
            Assert.Same(message, resolver.ResolveArgument(parameter, message));
        }

        [Fact]
        public void ResolveWithConversionNeededButNoConverter()
        {
            resolver = new MessageMethodArgumentResolver();

            var message = MessageBuilder.WithPayload("test").Build();
            var parameter = method.GetParameters()[1];
            Assert.True(resolver.SupportsParameter(parameter));
            var ex = Assert.Throws<MessageConversionException>(() => resolver.ResolveArgument(parameter, message));
            Assert.Contains("Int32", ex.Message);
            Assert.Contains("String", ex.Message);
        }

        [Fact]
        public void ResolveWithConversionEmptyPayloadButNoConverter()
        {
            resolver = new MessageMethodArgumentResolver();

            var message = MessageBuilder.WithPayload(string.Empty).Build();
            var parameter = method.GetParameters()[1];
            Assert.True(resolver.SupportsParameter(parameter));
            var ex = Assert.Throws<MessageConversionException>(() => resolver.ResolveArgument(parameter, message));
            Assert.Contains("payload is empty", ex.Message);
            Assert.Contains("Int32", ex.Message);
            Assert.Contains("String", ex.Message);
        }

        [Fact]
        public void ResolveWithNewtonJSonConverter()
        {
            var inMessage = MessageBuilder.WithPayload("{\"prop\":\"bar\"}").Build();
            var parameter = method.GetParameters()[5];

            resolver = new MessageMethodArgumentResolver(new NewtonJsonMessageConverter());
            var actual = resolver.ResolveArgument(parameter, inMessage);

            var condition1 = actual is IMessage;
            Assert.True(condition1);
            var outMessage = (IMessage)actual;
            var condition = outMessage.Payload is Foo;
            Assert.True(condition);
            Assert.Equal("bar", ((Foo)outMessage.Payload).Prop);
        }

        private void Handle(
                IMessage wildcardPayload,
                IMessage<int> integerPayload,
                IMessage<long> numberPayload,
                IMessage<string> anyNumberPayload,
                ErrorMessage subClass,
                IMessage<Foo> fooPayload)
        {
        }

        internal class Foo
        {
            public string Prop { get; set; }
        }
    }
}
