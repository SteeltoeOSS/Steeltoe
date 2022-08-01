// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Services;
using Steeltoe.Messaging.RabbitMQ.Config;

namespace Steeltoe.Messaging.RabbitMQ.Core;

public interface IRabbitAdmin : IServiceNameAware
{
    void DeclareExchange(IExchange exchange);

    bool DeleteExchange(string exchangeName);

    IQueue DeclareQueue();

    string DeclareQueue(IQueue queue);

    bool DeleteQueue(string queueName);

    void DeleteQueue(string queueName, bool unused, bool empty);

    void PurgeQueue(string queueName, bool noWait);

    uint PurgeQueue(string queueName);

    void DeclareBinding(IBinding binding);

    void RemoveBinding(IBinding binding);

    Dictionary<string, object> GetQueueProperties(string queueName);

    QueueInformation GetQueueInfo(string queueName);

    void Initialize();
}
