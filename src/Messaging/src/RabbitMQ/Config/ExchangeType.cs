// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.RabbitMQ.Config;

public static class ExchangeType
{
    public const string Direct = "direct";
    public const string Topic = "topic";
    public const string Fanout = "fanout";
    public const string Headers = "headers";
    public const string System = "system";
}
