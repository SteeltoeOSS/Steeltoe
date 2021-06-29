// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Services;

namespace Steeltoe.Messaging.RabbitMQ.Config
{
    public interface IQueue : IDeclarable, IServiceNameAware
    {
        string QueueName { get; set; }

        string ActualName { get; set; }

        bool IsDurable { get; set; }

        bool IsExclusive { get; set; }

        bool IsAutoDelete { get; set; }

        string MasterLocator { get; set; }

        object Clone();
    }
}
