// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Services;
using static Steeltoe.Messaging.RabbitMQ.Config.Binding;

namespace Steeltoe.Messaging.RabbitMQ.Config
{
    public interface IBinding : IDeclarable, IServiceNameAware
    {
        public string BindingName { get; set; }

        public string Destination { get; set; }

        public string Exchange { get; set; }

        public string RoutingKey { get; set; }

        public DestinationType Type { get; set; }

        public bool IsDestinationQueue { get; }
    }
}
