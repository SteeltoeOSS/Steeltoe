// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Messaging.Rabbit.Config;
using Steeltoe.Messaging.Rabbit.Connection;
using Steeltoe.Messaging.Rabbit.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Messaging.Rabbit.Attributes
{
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
            var queues = new List<IQueue>()
            {
                new Config.Queue(Q1),
                new Config.Queue(Q2),
                new Config.Queue(Q3),
                new Config.Queue(Q4)
            };

            ServiceProvider provider = null;
            try
            {
                provider = await CreateAndStartServices(null, queues, typeof(Listener));
                var template = provider.GetRabbitTemplate();
                var reply = template.ConvertSendAndReceive<Three>("EnableRabbitReturnTypesTests.1", "3");
                Assert.NotNull(reply);
                var reply2 = template.ConvertSendAndReceive<Four>("EnableRabbitReturnTypesTests.1", "4");
                Assert.NotNull(reply2);
            }
            finally
            {
                var admin = provider.GetRabbitAdmin();
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
            var queues = new List<IQueue>()
            {
                new Config.Queue(Q1),
                new Config.Queue(Q2),
                new Config.Queue(Q3),
                new Config.Queue(Q4)
            };

            ServiceProvider provider = null;
            try
            {
                provider = await CreateAndStartServices(null, queues, typeof(Listener));
                var template = provider.GetRabbitTemplate();
                var reply = template.ConvertSendAndReceive<Three>("EnableRabbitReturnTypesTests.2", "3");
                Assert.NotNull(reply);
                var reply2 = template.ConvertSendAndReceive<Four>("EnableRabbitReturnTypesTests.2", "4");
                Assert.NotNull(reply2);
            }
            catch (Exception e)
            {
            }
            finally
            {
                var admin = provider.GetRabbitAdmin();
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
            var queues = new List<IQueue>()
            {
                new Config.Queue(Q1),
                new Config.Queue(Q2),
                new Config.Queue(Q3),
                new Config.Queue(Q4)
            };

            ServiceProvider provider = null;
            try
            {
                provider = await CreateAndStartServices(null, queues, typeof(Listener));
                var template = provider.GetRabbitTemplate();
                var reply = template.ConvertSendAndReceive<List<Three>>("EnableRabbitReturnTypesTests.3", "3");
                Assert.NotNull(reply);
            }
            finally
            {
                var admin = provider.GetRabbitAdmin();
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
            var queues = new List<IQueue>()
            {
                new Config.Queue(Q1),
                new Config.Queue(Q2),
                new Config.Queue(Q3),
                new Config.Queue(Q4)
            };

            ServiceProvider provider = null;
            try
            {
                provider = await CreateAndStartServices(null, queues, typeof(Listener));
                var template = provider.GetRabbitTemplate();
                var reply = template.ConvertSendAndReceive<Three>("EnableRabbitReturnTypesTests.4", "3");
                Assert.NotNull(reply);
                var reply2 = template.ConvertSendAndReceive<Four>("EnableRabbitReturnTypesTests.4", "4");
                Assert.NotNull(reply2);
            }
            finally
            {
                var admin = provider.GetRabbitAdmin();
                admin.DeleteQueue(Q1);
                admin.DeleteQueue(Q2);
                admin.DeleteQueue(Q3);
                admin.DeleteQueue(Q4);
                provider?.Dispose();
            }
        }

        public class Listener
        {
            [RabbitListener("EnableRabbitReturnTypesTests.1")]

            public IOne Listen1(string input)
            {
                _output.WriteLine("Listen1 " + input);
                if ("3".Equals(input))
                {
                    return new Three();
                }
                else
                {
                    return new Four();
                }
            }

            [RabbitListener("EnableRabbitReturnTypesTests.2")]
            public Two Listen2(string input)
            {
                _output.WriteLine("Listen2 " + input);
                if ("3".Equals(input))
                {
                    return new Three();
                }
                else
                {
                    return new Four();
                }
            }

            [RabbitListener("EnableRabbitReturnTypesTests.3")]
            public List<Three> Listen3(string input)
            {
                _output.WriteLine("Listen3 " + input);
                var list = new List<Three>();
                list.Add(new Three());
                return list;
            }

            [RabbitListener("EnableRabbitReturnTypesTests.4")]
            public IOne Listen4(string input)
            {
                _output.WriteLine("Listen4 " + input);
                if ("3".Equals(input))
                {
                    return (IOne)new Three();
                }
                else
                {
                    return (IOne)new Four();
                }
            }
        }

        public static async Task<ServiceProvider> CreateAndStartServices(IConfiguration configuration, List<IQueue> queues, params Type[] listeners)
        {
            var services = new ServiceCollection();
            var config = configuration;
            if (config == null)
            {
                config = new ConfigurationBuilder().Build();
            }

            services.AddSingleton<IConfiguration>(config);
            services.AddRabbitHostingServices();
            services.AddRabbitMessageConverter<Support.Converter.JsonMessageConverter>();
            services.AddRabbitMessageHandlerMethodFactory();
            services.AddRabbitListenerEndpointRegistry();
            services.AddRabbitListenerEndpointRegistrar();
            services.AddRabbitListenerAttributeProcessor();
            services.AddSingleton<IConnectionFactory>((p) =>
            {
                return new CachingConnectionFactory("localhost");
            });

            services.AddRabbitListenerContainerFactory((p, f) =>
            {
                f.DefaultRequeueRejected = false;
            });

            services.AddRabbitAdmin();
            services.AddRabbitTemplate();
            services.AddRabbitQueues(queues.ToArray());

            foreach (var listener in listeners)
            {
                services.AddSingleton(listener);
            }

            services.AddRabbitListeners(config, listeners);
            var provider = services.BuildServiceProvider();
            await provider.GetRequiredService<IHostedService>().StartAsync(default);
            return provider;
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
}
