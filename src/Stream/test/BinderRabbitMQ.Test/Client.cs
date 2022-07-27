// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using EasyNetQ.Management.Client;
using EasyNetQ.Management.Client.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EnqBinding = EasyNetQ.Management.Client.Model.Binding;

namespace Steeltoe.Stream.Binder.Rabbit;

public class Client : ManagementClient
{
    public Client(string hostUrl = "http://localhost", string username = "guest", string password = "guest")
        : base(hostUrl, username, password)
    {
    }

    internal async Task<IEnumerable<EnqBinding>> GetBindingsBySource(string vhost, string exchangeName)
        => await GetBindingsWithSourceAsync(await GetExchange(vhost, exchangeName));

    internal async Task<Exchange> GetExchange(string vhost, string exchange)
        => await GetExchangeAsync(exchange, this.GetVhost(vhost));

    internal async Task<Queue> GetQueue(string vhost, string queueName)
        => await GetQueueAsync(queueName, this.GetVhost(vhost));
}