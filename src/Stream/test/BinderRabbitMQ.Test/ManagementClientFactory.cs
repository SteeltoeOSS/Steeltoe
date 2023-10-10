// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using EasyNetQ.Management.Client;

namespace Steeltoe.Stream.Binder.RabbitMQ.Test;

internal static class ManagementClientFactory
{
    public static ManagementClient CreateDefault(string hostUrl = "http://localhost:15672", string username = "guest", string password = "guest")
    {
        return new ManagementClient(new Uri(hostUrl), username, password);
    }
}
