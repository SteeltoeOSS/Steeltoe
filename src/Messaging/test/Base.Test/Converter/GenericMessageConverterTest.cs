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

using Steeltoe.Common.Converter;
using Steeltoe.Messaging.Support;
using System.Globalization;
using Xunit;

namespace Steeltoe.Messaging.Converter.Test
{
    public class GenericMessageConverterTest
    {
        private readonly IConversionService conversionService = new DefaultConversionService();

        [Fact]
        public void FromMessageWithConversion()
        {
            var converter = new GenericMessageConverter(conversionService);
            var content = MessageBuilder<string>.WithPayload("33").Build();
            Assert.Equal(33, converter.FromMessage<int>(content));
        }

        [Fact]
        public void FromMessageNoConverter()
        {
            var converter = new GenericMessageConverter(conversionService);
            var content = MessageBuilder<long>.WithPayload(1234L).Build();
            Assert.Null(converter.FromMessage<CultureInfo>(content));
        }

        [Fact]
        public void FromMessageWithFailedConversion()
        {
            var converter = new GenericMessageConverter(conversionService);
            var content = MessageBuilder<string>.WithPayload("test not a number").Build();
            Assert.Throws<MessageConversionException>(() => converter.FromMessage<int>(content));
        }
    }
}
