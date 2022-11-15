// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Messaging.RabbitMQ.Attributes;
using Steeltoe.Messaging.RabbitMQ.Configuration;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Support.Converter;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Messaging.RabbitMQ.Test.Attributes;

[Trait("Category", "Integration")]
public class EnableRabbitReturnTypesTest
{
    public const string Q1 = "EnableRabbitReturnTypesTests.1";
    public const string Q2 = "EnableRabbitReturnTypesTests.2";
    public const string Q3 = "EnableRabbitReturnTypesTests.3";
    public const string Q4 = "EnableRabbitReturnTypesTests.4";

    private static ITestOutputHelper _output;

    public EnableRabbitReturnTypesTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task TestInterfaceReturn()
    {
        var queues = new List<IQueue>
        {
            new Queue(Q1),
            new Queue(Q2),
            new Queue(Q3),
            new Queue(Q4)
        };

        ServiceProvider provider = null;

        try
        {
            provider = await CreateAndStartServicesAsync(null, queues, typeof(Listener));
            RabbitTemplate template = provider.GetRabbitTemplate();
            var reply = template.ConvertSendAndReceive<Three>("EnableRabbitReturnTypesTests.1", "3");
            Assert.NotNull(reply);
            var reply2 = template.ConvertSendAndReceive<Four>("EnableRabbitReturnTypesTests.1", "4");
            Assert.NotNull(reply2);
        }
        finally
        {
            RabbitAdmin admin = provider.GetRabbitAdmin();
            admin.DeleteQueue(Q1);
            admin.DeleteQueue(Q2);
            admin.DeleteQueue(Q3);
            admin.DeleteQueue(Q4);
            provider?.Dispose();
        }
    }

    [Fact]
    public async Task TestAbstractReturn()
    {
        var queues = new List<IQueue>
        {
            new Queue(Q1),
            new Queue(Q2),
            new Queue(Q3),
            new Queue(Q4)
        };

        ServiceProvider provider = null;

        try
        {
            provider = await CreateAndStartServicesAsync(null, queues, typeof(Listener));
            RabbitTemplate template = provider.GetRabbitTemplate();
            var reply = template.ConvertSendAndReceive<Three>("EnableRabbitReturnTypesTests.2", "3");
            Assert.NotNull(reply);
            var reply2 = template.ConvertSendAndReceive<Four>("EnableRabbitReturnTypesTests.2", "4");
            Assert.NotNull(reply2);
        }
        finally
        {
            RabbitAdmin admin = provider.GetRabbitAdmin();
            admin.DeleteQueue(Q1);
            admin.DeleteQueue(Q2);
            admin.DeleteQueue(Q3);
            admin.DeleteQueue(Q4);
            provider?.Dispose();
        }
    }

    [Fact]
    public async Task TestListOfThree()
    {
        var queues = new List<IQueue>
        {
            new Queue(Q1),
            new Queue(Q2),
            new Queue(Q3),
            new Queue(Q4)
        };

        ServiceProvider provider = null;

        try
        {
            provider = await CreateAndStartServicesAsync(null, queues, typeof(Listener));
            RabbitTemplate template = provider.GetRabbitTemplate();
            var reply = template.ConvertSendAndReceive<List<Three>>("EnableRabbitReturnTypesTests.3", "3");
            Assert.NotNull(reply);
        }
        finally
        {
            RabbitAdmin admin = provider.GetRabbitAdmin();
            admin.DeleteQueue(Q1);
            admin.DeleteQueue(Q2);
            admin.DeleteQueue(Q3);
            admin.DeleteQueue(Q4);
            provider?.Dispose();
        }
    }

    [Fact]
    public async Task TestGenericInterfaceReturn()
    {
        var queues = new List<IQueue>
        {
            new Queue(Q1),
            new Queue(Q2),
            new Queue(Q3),
            new Queue(Q4)
        };

        ServiceProvider provider = null;

        try
        {
            provider = await CreateAndStartServicesAsync(null, queues, typeof(Listener));
            RabbitTemplate template = provider.GetRabbitTemplate();
            var reply = template.ConvertSendAndReceive<Three>("EnableRabbitReturnTypesTests.4", "3");
            Assert.NotNull(reply);
            var reply2 = template.ConvertSendAndReceive<Four>("EnableRabbitReturnTypesTests.4", "4");
            Assert.NotNull(reply2);
        }
        finally
        {
            RabbitAdmin admin = provider.GetRabbitAdmin();
            admin.DeleteQueue(Q1);
            admin.DeleteQueue(Q2);
            admin.DeleteQueue(Q3);
            admin.DeleteQueue(Q4);
            provider?.Dispose();
        }
    }

    public static async Task<ServiceProvider> CreateAndStartServicesAsync(IConfiguration configuration, List<IQueue> queues, params Type[] listeners)
    {
        var services = new ServiceCollection();
        IConfiguration effectiveConfiguration = configuration ?? new ConfigurationBuilder().Build();

        services.AddSingleton(effectiveConfiguration);
        services.AddRabbitHostingServices();
        services.AddRabbitMessageConverter<JsonMessageConverter>();
        services.AddRabbitMessageHandlerMethodFactory();
        services.AddRabbitListenerEndpointRegistry();
        services.AddRabbitListenerEndpointRegistrar();
        services.AddRabbitListenerAttributeProcessor();
        services.AddSingleton<IConnectionFactory>(_ => new CachingConnectionFactory("localhost"));

        services.AddRabbitListenerContainerFactory((_, f) =>
        {
            f.DefaultRequeueRejected = false;
        });

        services.AddRabbitAdmin();
        services.AddRabbitTemplate();
        services.AddRabbitQueues(queues.ToArray());

        foreach (Type listener in listeners)
        {
            services.AddSingleton(listener);
        }

        services.AddRabbitListeners(effectiveConfiguration, listeners);
        ServiceProvider provider = services.BuildServiceProvider();
        await provider.GetRequiredService<IHostedService>().StartAsync(default);
        return provider;
    }

    public class Listener
    {
        [RabbitListener("EnableRabbitReturnTypesTests.1")]
        public IOne Listen1(string input)
        {
            _output.WriteLine("Listen1Async " + input);

            if (input == "3")
            {
                return new Three();
            }

            return new Four();
        }

        [RabbitListener("EnableRabbitReturnTypesTests.2")]
        public Two Listen2(string input)
        {
            _output.WriteLine("Listen2Async " + input);

            if (input == "3")
            {
                return new Three();
            }

            return new Four();
        }

        [RabbitListener("EnableRabbitReturnTypesTests.3")]
        public List<Three> Listen3(string input)
        {
            _output.WriteLine("Listen3Async " + input);

            var list = new List<Three>
            {
                new()
            };

            return list;
        }

        [RabbitListener("EnableRabbitReturnTypesTests.4")]
        public IOne Listen4(string input)
        {
            _output.WriteLine("Listen4Async " + input);

            if (input == "3")
            {
                return new Three();
            }

            return new Four();
        }
    }

    public interface IOne
    {
    }

    public abstract class Two : IOne
    {
        public string Field { get; set; }
    }

    public class Three : Two
    {
    }

    public class Four : Two
    {
    }
}
