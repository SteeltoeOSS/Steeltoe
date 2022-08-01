// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.RabbitMQ.Attributes;

namespace Steeltoe.Messaging.RabbitMQ.Listener;

public class Listener
{
    public CountdownEvent Latch { get; set; } = new (2);

    public int Counter { get; set; }

    [RabbitListener("test.expiry.main")]
    public Task Listen(string foo)
    {
        Latch.Signal();
        Counter++;
        return Task.FromException(new MessageConversionException("test.expiry"));
    }
}
