// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.RabbitMQ.Core;

namespace Steeltoe.Messaging.RabbitMQ.Config;

public class AnonymousQueue : Queue
{
    public AnonymousQueue()
        : this((Dictionary<string, object>)null)
    {
    }

    public AnonymousQueue(string serviceName)
        : this(Base64UrlNamingStrategy.Default, null)
    {
        ServiceName = serviceName;
    }

    public AnonymousQueue(Dictionary<string, object> arguments)
        : this(Base64UrlNamingStrategy.Default, arguments)
    {
    }

    public AnonymousQueue(INamingStrategy namingStrategy)
        : this(namingStrategy, null)
    {
    }

    public AnonymousQueue(INamingStrategy namingStrategy, Dictionary<string, object> arguments)
        : base(namingStrategy.GenerateName(), false, true, true, arguments)
    {
        if (!Arguments.ContainsKey(XQueueMasterLocator))
        {
            MasterLocator = "client-local";
        }
    }
}
