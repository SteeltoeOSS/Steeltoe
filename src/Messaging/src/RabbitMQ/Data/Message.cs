// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
