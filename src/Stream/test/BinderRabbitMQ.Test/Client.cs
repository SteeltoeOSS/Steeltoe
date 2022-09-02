// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using EasyNetQ.Management.Client;
using EasyNetQ.Management.Client.Model;
using EnqBinding = EasyNetQ.Management.Client.Model.Binding;

namespace Steeltoe.Stream.Binder.Rabbit;

public class Client : ManagementClient
{
    internal Client(string hostUrl = "http://localhost", string username = "guest", string password = "guest")
        : base(hostUrl, username, password)
    {
    }

    internal async Task<IEnumerable<EnqBinding>> GetBindingsBySourceAsync(string vhost, string exchangeName)
    {
        return await GetBindingsWithSourceAsync(await GetExchangeAsync(vhost, exchangeName));
    }

    internal async Task<Exchange> GetExchangeAsync(string vhost, string exchange)
    {
        return await GetExchangeAsync(exchange, this.GetVhost(vhost));
    }

    internal async Task<Queue> GetQueueAsync(string vhost, string queueName)
    {
        return await GetQueueAsync(queueName, this.GetVhost(vhost));
    }
}
