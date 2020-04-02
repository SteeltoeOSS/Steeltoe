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

namespace Steeltoe.Messaging.Rabbit.Data
{
    public class MessagePropertiesBuilder : AbstractMessageBuilder<MessageProperties>
    {
        public static MessagePropertiesBuilder NewInstance()
        {
            return new MessagePropertiesBuilder();
        }

        public static MessagePropertiesBuilder FromProperties(MessageProperties properties)
        {
            return new MessagePropertiesBuilder(properties);
        }

        public static MessagePropertiesBuilder FromClonedProperties(MessageProperties properties)
        {
            var builder = NewInstance();
            return builder.CopyProperties(properties);
        }

        private MessagePropertiesBuilder()
        {
        }

        private MessagePropertiesBuilder(MessageProperties properties)
        : base(properties)
        {
        }

        public new MessagePropertiesBuilder CopyProperties(MessageProperties properties)
        {
            base.CopyProperties(properties);
            return this;
        }

        public override MessageProperties Build()
        {
            return Properties;
        }
    }
}
