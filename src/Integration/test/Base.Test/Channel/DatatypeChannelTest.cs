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

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Converter;
using Steeltoe.Integration.Support;
using Steeltoe.Integration.Support.Converter;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Integration.Channel.Test
{
    public class DatatypeChannelTest
    {
        [Fact]
        public void SupportedType()
        {
            IMessageChannel channel = CreateChannel(typeof(string));
            Assert.True(channel.Send(new GenericMessage("test")));
        }

        [Fact]
        public void UnsupportedTypeAndNoConversionService()
        {
            IMessageChannel channel = CreateChannel(typeof(int));
            Assert.Throws<MessageDeliveryException>(() => channel.Send(new GenericMessage("123")));
        }

        [Fact]
        public void UnsupportedTypeButConversionServiceSupports()
        {
            var channel = CreateChannel(typeof(int));
            IConversionService conversionService = new DefaultConversionService();
            var converter = new DefaultDatatypeChannelMessageConverter(conversionService);
            channel.MessageConverter = converter;
            Assert.True(channel.Send(new GenericMessage("123")));
        }

        [Fact]
        public void UnsupportedTypeAndConversionServiceDoesNotSupport()
        {
            var channel = CreateChannel(typeof(int));
            IConversionService conversionService = new DefaultConversionService();
            var converter = new DefaultDatatypeChannelMessageConverter(conversionService);
            channel.MessageConverter = converter;
            Assert.Throws<MessageDeliveryException>(() => channel.Send(new GenericMessage<bool>(true)));
        }

        [Fact]
        public void UnsupportedTypeButCustomConversionServiceSupports()
        {
            var channel = CreateChannel(typeof(int));
            GenericConversionService conversionService = new DefaultConversionService();
            conversionService.AddConverter(new BoolToIntConverter());
            var converter = new DefaultDatatypeChannelMessageConverter(conversionService);
            channel.MessageConverter = converter;
            Assert.True(channel.Send(new GenericMessage<bool>(true)));
            Assert.Equal(1, channel.Receive().Payload);
        }

        [Fact]
        public void ConversionServiceUsedByDefault()
        {
            var converter = new BoolToIntConverter();
            var convService = new GenericConversionService();
            convService.AddConverter(converter);
            var services = new ServiceCollection();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            services.AddSingleton<IConversionService>(convService);
            services.AddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();
            services.AddSingleton<DefaultDatatypeChannelMessageConverter>();
            var provider = services.BuildServiceProvider();
            var channel = new QueueChannel(provider, "testChannel");
            channel.DataTypes = new List<Type>() { typeof(int), typeof(DateTime) };
            Assert.True(channel.Send(new GenericMessage<bool>(true)));
            Assert.Equal(1, channel.Receive().Payload);
        }

        [Fact]
        public void MultipleTypes()
        {
            IMessageChannel channel = CreateChannel(typeof(string), typeof(int));
            Assert.True(channel.Send(new GenericMessage("test1")));
            Assert.True(channel.Send(new GenericMessage<int>(2)));
            Exception exception = null;
            try
            {
                channel.Send(new GenericMessage<DateTime>(default));
            }
            catch (MessageDeliveryException e)
            {
                exception = e;
            }

            Assert.NotNull(exception);
        }

        [Fact]
        public void SubclassOfAcceptedType()
        {
            IMessageChannel channel = CreateChannel(typeof(Exception));
            Assert.True(channel.Send(new ErrorMessage(new MessagingException("test"))));
        }

        [Fact]
        public void SuperclassOfAcceptedTypeNotAccepted()
        {
            IMessageChannel channel = CreateChannel(typeof(InvalidOperationException));
            Assert.Throws<MessageDeliveryException>(() => channel.Send(new ErrorMessage(new Exception("test"))));
        }

        [Fact]
        public void GenericConverters()
        {
            var channel = CreateChannel(typeof(Foo));
            var conversionService = new DefaultConversionService();
            conversionService.AddConverter(new StringToBarConverter());
            conversionService.AddConverter(new IntegerToBazConverter());
            var converter = new DefaultDatatypeChannelMessageConverter(conversionService);
            channel.MessageConverter = converter;
            Assert.True(channel.Send(new GenericMessage("foo")));
            var outmessage = channel.Receive(0);
            Assert.IsType<Bar>(outmessage.Payload);
            Assert.True(channel.Send(new GenericMessage<int>(42)));
            outmessage = channel.Receive(0);
            Assert.IsType<Baz>(outmessage.Payload);
        }

        private static QueueChannel CreateChannel(params Type[] datatypes)
        {
            var services = new ServiceCollection();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            services.AddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();
            var provider = services.BuildServiceProvider();
            var channel = new QueueChannel(provider, "testChannel");
            channel.DataTypes = new List<Type>(datatypes);
            return channel;
        }

        private class BoolToIntConverter : IGenericConverter
        {
            private readonly ISet<(Type, Type)> _types = new HashSet<(Type, Type)>() { (typeof(bool), typeof(int)) };

            public ISet<(Type Source, Type Target)> ConvertibleTypes => _types;

            public object Convert(object source, Type sourceType, Type targetType)
            {
                var asBool = (bool)source;
                return asBool ? 1 : 0;
            }
        }

        private class Foo
        {
            public Foo()
            {
            }
        }

        private class Bar : Foo
        {
            public Bar()
            {
            }
        }

        private class Baz : Foo
        {
            public Baz()
            {
            }
        }

        private class StringToBarConverter : IGenericConverter
        {
            private readonly ISet<(Type, Type)> _types = new HashSet<(Type, Type)>() { (typeof(string), typeof(Foo)), (typeof(string), typeof(Bar)) };

            public StringToBarConverter()
            {
            }

            public ISet<(Type Source, Type Target)> ConvertibleTypes => _types;

            public object Convert(object source, Type sourceType, Type targetType)
            {
                return new Bar();
            }
        }

        private class IntegerToBazConverter : IGenericConverter
        {
            private readonly ISet<(Type, Type)> _types = new HashSet<(Type, Type)>() { (typeof(int), typeof(Foo)), (typeof(int), typeof(Baz)) };

            public IntegerToBazConverter()
            {
            }

            public ISet<(Type Source, Type Target)> ConvertibleTypes => _types;

            public object Convert(object source, Type sourceType, Type targetType)
            {
                return new Baz();
            }
        }
    }
}
