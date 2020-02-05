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

using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Attributes.Support;
using Steeltoe.Messaging.Handler.Invocation;

namespace Steeltoe.Integration.Support
{
    public class NullAwarePayloadArgumentResolver : PayloadMethodArgumentResolver
    {
        public NullAwarePayloadArgumentResolver(IMessageConverter messageConverter)
            : base(messageConverter, false)
        {
        }

        public NullAwarePayloadArgumentResolver(IMessageConverter messageConverter, bool useDefaultResolution)
        : base(messageConverter, useDefaultResolution)
        {
        }

        protected override bool IsEmptyPayload(object payload)
        {
            return base.IsEmptyPayload(payload) || "KafkaNull".Equals(payload.GetType().Name);
        }
    }
}
