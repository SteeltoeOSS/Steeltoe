// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.RabbitMQ.Config;

public abstract class AbstractBuilder
{
    protected Dictionary<string, object> GetOrCreateArguments()
    {
        Arguments ??= new Dictionary<string, object>();
        return Arguments;
    }

    protected Dictionary<string, object> Arguments { get; private set; }
}
