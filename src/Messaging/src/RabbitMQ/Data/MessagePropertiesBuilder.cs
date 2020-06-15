// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
