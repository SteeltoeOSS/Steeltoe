// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Messaging.RabbitMQ.Config
{
    public class BindingBuilderTest
    {
        private readonly IQueue queue;

        public BindingBuilderTest()
        {
            queue = new Queue("q");
        }

        [Fact]
        public void FanoutBinding()
        {
            var fanoutExchange = new FanoutExchange("f");
            var binding = BindingBuilder.Bind(queue).To(fanoutExchange);
            Assert.NotNull(binding);
            Assert.Equal(fanoutExchange.ExchangeName, binding.Exchange);
            Assert.Equal(string.Empty, binding.RoutingKey);
            Assert.Equal(Binding.DestinationType.QUEUE, binding.Type);
            Assert.Equal(queue.QueueName, binding.Destination);
        }

        [Fact]
        public void DirectBinding()
        {
            var directExchange = new DirectExchange("d");
            var routingKey = "r";
            var binding = BindingBuilder.Bind(queue).To(directExchange).With(routingKey);
            Assert.NotNull(binding);
            Assert.Equal(directExchange.ExchangeName, binding.Exchange);
            Assert.Equal(Binding.DestinationType.QUEUE, binding.Type);
            Assert.Equal(queue.QueueName, binding.Destination);
            Assert.Equal(routingKey, binding.RoutingKey);
        }

        [Fact]
        public void DirectBindingWithQueueName()
        {
            var directExchange = new DirectExchange("d");
            var binding = BindingBuilder.Bind(queue).To(directExchange).WithQueueName();
            Assert.NotNull(binding);
            Assert.Equal(directExchange.ExchangeName, binding.Exchange);
            Assert.Equal(Binding.DestinationType.QUEUE, binding.Type);
            Assert.Equal(queue.QueueName, binding.Destination);
            Assert.Equal(queue.QueueName, binding.RoutingKey);
        }

        [Fact]
        public void TopicBinding()
        {
            var topicExchange = new TopicExchange("t");
            var routingKey = "r";
            var binding = BindingBuilder.Bind(queue).To(topicExchange).With(routingKey);
            Assert.NotNull(binding);
            Assert.Equal(topicExchange.ExchangeName, binding.Exchange);
            Assert.Equal(Binding.DestinationType.QUEUE, binding.Type);
            Assert.Equal(queue.QueueName, binding.Destination);
            Assert.Equal(routingKey, binding.RoutingKey);
        }

        [Fact]
        public void HeaderBinding()
        {
            var headersExchange = new HeadersExchange("h");
            var headerKey = "headerKey";
            var binding = BindingBuilder.Bind(queue).To(headersExchange).Where(headerKey).Exists();
            Assert.NotNull(binding);
            Assert.Equal(headersExchange.ExchangeName, binding.Exchange);
            Assert.Equal(Binding.DestinationType.QUEUE, binding.Type);
            Assert.Equal(queue.QueueName, binding.Destination);
            Assert.Equal(string.Empty, binding.RoutingKey);
        }

        [Fact]
        public void CustomBinding()
        {
            var argumentobject = new object();
            var customExchange = new CustomExchange("c");
            var routingKey = "r";
            var binding = BindingBuilder.
                    Bind(queue).
                    To(customExchange).
                    With(routingKey).
                    And(new Dictionary<string, object> { { "k", argumentobject } });
            Assert.NotNull(binding);
            Assert.Equal(argumentobject, binding.Arguments["k"]);
            Assert.Equal(customExchange.ExchangeName, binding.Exchange);
            Assert.Equal(Binding.DestinationType.QUEUE, binding.Type);
            Assert.Equal(queue.QueueName, binding.Destination);
            Assert.Equal(routingKey, binding.RoutingKey);
        }

        [Fact]
        public void ExchangeBinding()
        {
            var directExchange = new DirectExchange("d");
            var fanoutExchange = new FanoutExchange("f");
            var binding = BindingBuilder.Bind(directExchange).To(fanoutExchange);
            Assert.NotNull(binding);
            Assert.Equal(fanoutExchange.ExchangeName, binding.Exchange);
            Assert.Equal(Binding.DestinationType.EXCHANGE, binding.Type);
            Assert.Equal(directExchange.ExchangeName, binding.Destination);
            Assert.Equal(string.Empty, binding.RoutingKey);
        }

        private sealed class CustomExchange : AbstractExchange
        {
            public CustomExchange(string name)
                : base(name)
            {
            }

            public override string Type { get; } = "x-custom";
        }
    }
}
