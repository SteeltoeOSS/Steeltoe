// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using RabbitMQ.Client;
using Steeltoe.Messaging.Rabbit.Core;
using System.Text;

namespace Steeltoe.Messaging.Rabbit.Support
{
    public interface IMessageHeadersConverter
    {
        IMessageHeaders ToMessageHeaders(IBasicProperties source, Envelope envelope, Encoding charset);

        void FromMessageHeaders(IMessageHeaders source, IBasicProperties target, Encoding charset);
    }
}
