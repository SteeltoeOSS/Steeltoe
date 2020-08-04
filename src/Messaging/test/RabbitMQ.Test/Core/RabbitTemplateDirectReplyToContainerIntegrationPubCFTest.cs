// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.RabbitMQ.Connection;
using Xunit;

namespace Steeltoe.Messaging.RabbitMQ.Core
{
    [Trait("Category", "Integration")]
    public class RabbitTemplateDirectReplyToContainerIntegrationPubCFTest : RabbitTemplateDirectReplyToContainerIntegrationTest
    {
        protected override RabbitTemplate CreateSendAndReceiveRabbitTemplate(IConnectionFactory connectionFactory)
        {
            var srTemplate = base.CreateSendAndReceiveRabbitTemplate(connectionFactory);
            srTemplate.UsePublisherConnection = true;
            return srTemplate;
        }
    }
}
