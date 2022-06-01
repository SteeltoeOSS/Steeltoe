// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.RabbitMQ.Core;
using System.Text;
using RC=RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Support;

public interface IMessageHeadersConverter
{
    IMessageHeaders ToMessageHeaders(RC.IBasicProperties source, Envelope envelope, Encoding charset);

    void FromMessageHeaders(IMessageHeaders source, RC.IBasicProperties target, Encoding charset);
}
