// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Messaging.RabbitMQ.Configuration;

public class BindingBuilderTest
{
    private readonly IQueue _queue;

    public BindingBuilderTest()
    {
        _queue = new Queue("q");
    }

    [Fact]
    public void FanOutBinding()
    {
        var fanOutExchange = new FanOutExchange("f");
        IBinding binding = BindingBuilder.Bind(_queue).To(fanOutExchange);
        Assert.NotNull(binding);
        Assert.Equal(fanOutExchange.ExchangeName, binding.Exchange);
        Assert.Equal(string.Empty, binding.RoutingKey);
        Assert.Equal(Binding.DestinationType.Queue, binding.Type);
        Assert.Equal(_queue.QueueName, binding.Destination);
    }

    [Fact]
    public void DirectBinding()
    {
        var directExchange = new DirectExchange("d");
        const string routingKey = "r";
        IBinding binding = BindingBuilder.Bind(_queue).To(directExchange).With(routingKey);
        Assert.NotNull(binding);
        Assert.Equal(directExchange.ExchangeName, binding.Exchange);
        Assert.Equal(Binding.DestinationType.Queue, binding.Type);
        Assert.Equal(_queue.QueueName, binding.Destination);
        Assert.Equal(routingKey, binding.RoutingKey);
    }

    [Fact]
    public void DirectBindingWithQueueName()
    {
        var directExchange = new DirectExchange("d");
        IBinding binding = BindingBuilder.Bind(_queue).To(directExchange).WithQueueName();
        Assert.NotNull(binding);
        Assert.Equal(directExchange.ExchangeName, binding.Exchange);
        Assert.Equal(Binding.DestinationType.Queue, binding.Type);
        Assert.Equal(_queue.QueueName, binding.Destination);
        Assert.Equal(_queue.QueueName, binding.RoutingKey);
    }

    [Fact]
    public void TopicBinding()
    {
        var topicExchange = new TopicExchange("t");
        const string routingKey = "r";
        IBinding binding = BindingBuilder.Bind(_queue).To(topicExchange).With(routingKey);
        Assert.NotNull(binding);
        Assert.Equal(topicExchange.ExchangeName, binding.Exchange);
        Assert.Equal(Binding.DestinationType.Queue, binding.Type);
        Assert.Equal(_queue.QueueName, binding.Destination);
        Assert.Equal(routingKey, binding.RoutingKey);
    }

    [Fact]
    public void HeaderBinding()
    {
        var headersExchange = new HeadersExchange("h");
        const string headerKey = "headerKey";
        IBinding binding = BindingBuilder.Bind(_queue).To(headersExchange).Where(headerKey).Exists();
        Assert.NotNull(binding);
        Assert.Equal(headersExchange.ExchangeName, binding.Exchange);
        Assert.Equal(Binding.DestinationType.Queue, binding.Type);
        Assert.Equal(_queue.QueueName, binding.Destination);
        Assert.Equal(string.Empty, binding.RoutingKey);
    }

    [Fact]
    public void CustomBinding()
    {
        object argumentObject = new();
        var customExchange = new CustomExchange("c");
        const string routingKey = "r";

        IBinding binding = BindingBuilder.Bind(_queue).To(customExchange).With(routingKey).And(new Dictionary<string, object>
        {
            { "k", argumentObject }
        });

        Assert.NotNull(binding);
        Assert.Equal(argumentObject, binding.Arguments["k"]);
        Assert.Equal(customExchange.ExchangeName, binding.Exchange);
        Assert.Equal(Binding.DestinationType.Queue, binding.Type);
        Assert.Equal(_queue.QueueName, binding.Destination);
        Assert.Equal(routingKey, binding.RoutingKey);
    }

    [Fact]
    public void ExchangeBinding()
    {
        var directExchange = new DirectExchange("d");
        var fanOutExchange = new FanOutExchange("f");
        IBinding binding = BindingBuilder.Bind(directExchange).To(fanOutExchange);
        Assert.NotNull(binding);
        Assert.Equal(fanOutExchange.ExchangeName, binding.Exchange);
        Assert.Equal(Binding.DestinationType.Exchange, binding.Type);
        Assert.Equal(directExchange.ExchangeName, binding.Destination);
        Assert.Equal(string.Empty, binding.RoutingKey);
    }

    private sealed class CustomExchange : AbstractExchange
    {
        public override string Type { get; } = "x-custom";

        public CustomExchange(string name)
            : base(name)
        {
        }
    }
}
