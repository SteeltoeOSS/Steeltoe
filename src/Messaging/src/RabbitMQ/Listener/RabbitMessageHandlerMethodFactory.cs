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
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Attributes.Support;
using Steeltoe.Messaging.Handler.Invocation;
using Steeltoe.Messaging.Rabbit.Support.Converter;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Steeltoe.Messaging.Rabbit.Listener
{
    public class RabbitMessageHandlerMethodFactory : DefaultMessageHandlerMethodFactory
    {
        public new const string DEFAULT_SERVICE_NAME = nameof(RabbitMessageHandlerMethodFactory);

        public RabbitMessageHandlerMethodFactory()
           : base(new DefaultConversionService())
        {
            var defService = ConversionService as DefaultConversionService;
            defService.AddConverter(new BytesToStringConverter(Charset));
        }

        public override string ServiceName { get; set; } = DEFAULT_SERVICE_NAME;

        public Encoding Charset { get; set; } = EncodingUtils.Utf8;
    }
}
