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
using System.Collections.Generic;

namespace Steeltoe.Integration.Support
{
    public class DefaultMessageBuilderFactory : IMessageBuilderFactory
    {
        public IList<string> ReadOnlyHeaders { get; set; } = new List<string>();

        public void AddReadOnlyHeaders(params string[] readOnlyHeaders)
        {
            foreach (var h in readOnlyHeaders)
            {
                ReadOnlyHeaders.Add(h);
            }
        }

        public IMessageBuilder<T> FromMessage<T>(IMessage<T> message)
        {
            return IntegrationMessageBuilder<T>.FromMessage(message)
                    .ReadOnlyHeaders(ReadOnlyHeaders);
        }

        public IMessageBuilder FromMessage(IMessage message)
        {
            return IntegrationMessageBuilder.FromMessage(message)
                    .ReadOnlyHeaders(ReadOnlyHeaders);
        }

        public IMessageBuilder<T> WithPayload<T>(T payload)
        {
            return IntegrationMessageBuilder<T>.WithPayload(payload)
                    .ReadOnlyHeaders(ReadOnlyHeaders);
        }

        public IMessageBuilder WithPayload(object payload)
        {
            return IntegrationMessageBuilder.WithPayload(payload)
                    .ReadOnlyHeaders(ReadOnlyHeaders);
        }
    }
}
