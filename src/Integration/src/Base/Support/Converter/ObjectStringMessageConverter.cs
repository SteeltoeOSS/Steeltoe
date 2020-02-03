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

using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using System;

namespace Steeltoe.Integration.Support.Converter
{
    public class ObjectStringMessageConverter : StringMessageConverter
    {
        protected override object ConvertFromInternal(IMessage message, Type targetClass, object conversionHint)
        {
            var payload = message.Payload;
            if (payload is string || payload is byte[])
            {
                return base.ConvertFromInternal(message, targetClass, conversionHint);
            }
            else
            {
                return payload.ToString();
            }
        }
    }
}
