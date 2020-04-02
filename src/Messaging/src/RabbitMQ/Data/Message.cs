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

using Steeltoe.Common.Util;
using System;
using System.Text;

namespace Steeltoe.Messaging.Rabbit.Data
{
    // TODO: This is an AMQP type
    public class Message
    {
        public Message(byte[] body, MessageProperties messageProperties)
        {
            Body = body;
            MessageProperties = messageProperties;
        }

        public MessageProperties MessageProperties { get; }

        public byte[] Body { get; }

        public override string ToString()
        {
            var buffer = new StringBuilder();
            buffer.Append("(");
            buffer.Append("Body:'").Append(GetBodyContentAsString()).Append("'");
            if (MessageProperties != null)
            {
                buffer.Append(" ").Append(MessageProperties.ToString());
            }

            buffer.Append(")");
            return buffer.ToString();
        }

        public override int GetHashCode()
        {
            var prime = 31;
            var result = 1;
            result = (prime * result) + ObjectUtils.NullSafeHashCode(Body);
            result = (prime * result) + ((MessageProperties == null) ? 0 : MessageProperties.GetHashCode());
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

            var other = (Message)obj;
            if (!ObjectUtils.NullSafeEquals(Body, other.Body))
            {
                return false;
            }

            if (MessageProperties == null)
            {
                if (other.MessageProperties != null)
                {
                    return false;
                }
            }
            else if (!MessageProperties.Equals(other.MessageProperties))
            {
                return false;
            }

            return true;
        }

        private string GetBodyContentAsString()
        {
            if (Body == null)
            {
                return null;
            }

            try
            {
                var nullProps = MessageProperties == null;
                var contentType = nullProps ? null : MessageProperties.ContentType;

                if (Data.MessageProperties.CONTENT_TYPE_TEXT_PLAIN.Equals(contentType)
                        || Data.MessageProperties.CONTENT_TYPE_JSON.Equals(contentType)
                        || MessageProperties.CONTENT_TYPE_JSON_ALT.Equals(contentType)
                        || MessageProperties.CONTENT_TYPE_XML.Equals(contentType))
                {
                    return EncodingUtils.Utf8.GetString(Body);
                }
            }
            catch (Exception)
            {
                // ignore
            }

            // Comes out as '[B@....b' (so harmless)
            return Body.ToString() + "(byte[" + Body.Length + "])";
        }
    }
}
