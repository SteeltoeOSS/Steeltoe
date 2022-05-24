// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Converter;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Attributes;
using Steeltoe.Messaging.Handler.Attributes.Support;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Listener;
using Steeltoe.Messaging.RabbitMQ.Support;
using Steeltoe.Messaging.RabbitMQ.Support.Converter;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Xunit;
using static Steeltoe.Messaging.RabbitMQ.Attributes.EnableRabbitIntegrationCustomConfigTest;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Attributes
{
    [Trait("Category", "Integration")]
    public class EnableRabbitIntegrationCustomConfigTest : IClassFixture<CustomStartupFixture>
    {
        private readonly ServiceProvider provider;
        private readonly CustomStartupFixture fixture;

        public EnableRabbitIntegrationCustomConfigTest(CustomStartupFixture fix)
        {
            fixture = fix;
            provider = fixture.Provider;
        }

        [Fact]
        public void TestConverted()
        {
            var template = provider.GetRabbitTemplate();
            var foo1 = new Foo1
            {
                Bar = "bar"
            };
            var ctx = provider.GetService<IApplicationContext>();
            var converter = ctx.GetService<ISmartMessageConverter>(JsonMessageConverter.DEFAULT_SERVICE_NAME) as JsonMessageConverter;
            converter.TypeMapper.DefaultType = typeof(Dictionary<string, object>);
            converter.Precedence = TypePrecedence.TYPE_ID;
            var returned = template.ConvertSendAndReceive<Foo2>("test.converted", foo1);
            Assert.IsType<Foo2>(returned);
            Assert.Equal("bar", returned.Bar);
            converter.Precedence = TypePrecedence.INFERRED;

            template.MessageConverter = new Support.Converter.SimpleMessageConverter();
            var messagePostProcessor = new MessagePostProcessor();
            var returned2 = template.ConvertSendAndReceive<byte[]>(string.Empty, "test.converted", "{ \"bar\" : \"baz\" }", messagePostProcessor);
            Assert.Equal("{\"bar\":\"baz\"}", Encoding.UTF8.GetString(returned2));

            var returned3 = template.ConvertSendAndReceive<byte[]>(string.Empty, "test.converted.list", "[{ \"bar\" : \"baz\" }]", messagePostProcessor);
            Assert.Equal("{\"bar\":\"BAZZZZ\"}", Encoding.UTF8.GetString(returned3));

            var returned4 = template.ConvertSendAndReceive<byte[]>(string.Empty, "test.converted.array", "[{ \"bar\" : \"baz\" }]", messagePostProcessor);
            Assert.Equal("{\"bar\":\"BAZZxx\"}", Encoding.UTF8.GetString(returned4));

            var returned5 = template.ConvertSendAndReceive<byte[]>(string.Empty, "test.converted.args1", "{ \"bar\" : \"baz\" }", messagePostProcessor);
            Assert.Equal("\"bar=baztest.converted.args1\"", Encoding.UTF8.GetString(returned5));

            var returned6 = template.ConvertSendAndReceive<byte[]>(string.Empty, "test.converted.args2", "{ \"bar\" : \"baz\" }", messagePostProcessor);
            Assert.Equal("\"bar=baztest.converted.args2\"", Encoding.UTF8.GetString(returned6));

            var beanMethodHeaders = new List<string>();
            var mpp = new AfterReceivePostProcessors(beanMethodHeaders);
            template.SetAfterReceivePostProcessors(mpp);
            var returned7 = template.ConvertSendAndReceive<byte[]>(string.Empty, "test.converted.message", "{ \"bar\" : \"baz\" }", messagePostProcessor);
            Assert.Equal("\"bar=bazFoo2MessageFoo2Service\"", Encoding.UTF8.GetString(returned7));
            Assert.Equal(2, beanMethodHeaders.Count);
            Assert.Equal("Foo2Service", beanMethodHeaders[0]);
            Assert.Equal("Foo2Message", beanMethodHeaders[1]);

            template.RemoveAfterReceivePostProcessor(mpp);
            var foo2Service = ctx.GetService<Foo2Service>();
            Assert.IsType<Foo2Service>(foo2Service.Bean);
            Assert.NotNull(foo2Service.Method);
            Assert.Equal("Foo2Message", foo2Service.Method.Name);

            var returned8 = template.ConvertSendAndReceive<byte[]>(string.Empty, "test.notconverted.message", "{ \"bar\" : \"baz\" }", messagePostProcessor);
            Assert.Equal("\"fooMessage`1\"", Encoding.UTF8.GetString(returned8));
            Assert.Equal("string", foo2Service.StringHeader);
            Assert.Equal(42, foo2Service.IntHeader);

            var returned9 = template.ConvertSendAndReceive<byte[]>(string.Empty, "test.notconverted.channel", "{ \"bar\" : \"baz\" }", messagePostProcessor);
            Assert.Equal("\"barAndChannel\"", Encoding.UTF8.GetString(returned9));

            var returned10 = template.ConvertSendAndReceive<byte[]>(string.Empty, "test.notconverted.messagechannel", "{ \"bar\" : \"baz\" }", messagePostProcessor);
            Assert.Equal("\"bar=bazMessage`1AndChannel\"", Encoding.UTF8.GetString(returned10));

            var returned11 = template.ConvertSendAndReceive<byte[]>(string.Empty, "test.notconverted.messagingmessage", "{ \"bar\" : \"baz\" }", messagePostProcessor);
            Assert.Equal("\"Message`1Dictionary`2\"", Encoding.UTF8.GetString(returned11));

            var returned12 = template.ConvertSendAndReceive<byte[]>(string.Empty, "test.converted.foomessage", "{ \"bar\" : \"baz\" }", messagePostProcessor);
            Assert.Equal("\"Message`1Foo2guest\"", Encoding.UTF8.GetString(returned12));

            var returned13 = template.ConvertSendAndReceive<byte[]>(string.Empty, "test.notconverted.messagingmessagenotgeneric", "{ \"bar\" : \"baz\" }", messagePostProcessor);
            Assert.Equal("\"Message`1Dictionary`2\"", Encoding.UTF8.GetString(returned13));
        }

        public class AfterReceivePostProcessors : IMessagePostProcessor
        {
            public List<string> Headers { get; set; }

            public AfterReceivePostProcessors(List<string> headers)
            {
                Headers = headers;
            }

            public IMessage PostProcessMessage(IMessage message, CorrelationData correlation)
            {
                Headers.Add(message.Headers.Get<string>("bean"));
                Headers.Add(message.Headers.Get<string>("method"));
                return message;
            }

            public IMessage PostProcessMessage(IMessage message)
            {
                Headers.Add(message.Headers.Get<string>("bean"));
                Headers.Add(message.Headers.Get<string>("method"));
                return message;
            }
        }

        public class MessagePostProcessor : IMessagePostProcessor
        {
            public IMessage PostProcessMessage(IMessage message, CorrelationData correlation)
            {
                var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
                accessor.ContentType = "application/json";
                accessor.UserId = "guest";
                accessor.SetHeader("stringHeader", "string");
                accessor.SetHeader("intHeader", 42);
                return message;
            }

            public IMessage PostProcessMessage(IMessage message)
            {
                var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
                accessor.ContentType = "application/json";
                accessor.UserId = "guest";
                accessor.SetHeader("stringHeader", "string");
                accessor.SetHeader("intHeader", 42);
                return message;
            }
        }

        public class CustomStartupFixture : IDisposable
        {
            private readonly CachingConnectionFactory adminCf;
            private readonly RabbitAdmin admin;
            private readonly IServiceCollection services;

            public ServiceProvider Provider { get; set; }

            public CustomStartupFixture()
            {
                adminCf = new CachingConnectionFactory("localhost");
                admin = new RabbitAdmin(adminCf);
                foreach (var q in Queues)
                {
                    var queue = new Queue(q);
                    admin.DeclareQueue(queue);
                }

                services = CreateContainer();
                Provider = services.BuildServiceProvider();
                Provider.GetRequiredService<IHostedService>().StartAsync(default).Wait();
            }

            public void Dispose()
            {
                foreach (var q in Queues)
                {
                    admin.DeleteQueue(q);
                }

                // admin.DeleteQueue("sendTo.replies");
                // admin.DeleteQueue("sendTo.replies.spel");
                adminCf.Dispose();

                Provider.Dispose();
            }

            public class MyRabbitListenerConfigurer : IRabbitListenerConfigurer
            {
                private readonly IApplicationContext _context;

                public MyRabbitListenerConfigurer(IApplicationContext context)
                {
                    _context = context;
                }

                public void ConfigureRabbitListeners(IRabbitListenerEndpointRegistrar registrar)
                {
                    var handler = _context.GetService<IMessageHandlerMethodFactory>("myHandlerMethodFactory");
                    registrar.MessageHandlerMethodFactory = handler;
                }
            }

            public ServiceCollection CreateContainer(IConfiguration config = null)
            {
                var services = new ServiceCollection();
                config ??= new ConfigurationBuilder().Build();

                services.AddLogging(b =>
                {
                    b.AddDebug();
                    b.AddConsole();
                });

                services.AddSingleton(config);
                services.AddRabbitHostingServices();
                services.AddRabbitJsonMessageConverter();

                services.AddRabbitListenerEndpointRegistry();
                services.AddRabbitListenerEndpointRegistrar();
                services.AddRabbitListenerAttributeProcessor();
                services.AddRabbitConnectionFactory();

                services.AddSingleton<IRabbitListenerConfigurer, MyRabbitListenerConfigurer>();
                services.AddRabbitMessageHandlerMethodFactory((p, f) =>
                {
                    f.ServiceName = "myHandlerMethodFactory";
                    var service = DefaultConversionService.Singleton as DefaultConversionService;
                    service.AddConverter(new Foo1ToFoo2Converter());
                    f.ConversionService = service;
                    f.MessageConverter = new GenericMessageConverter(service);
                });

                // Add default container factory
                services.AddRabbitListenerContainerFactory((p, f) =>
                {
                    f.SetBeforeSendReplyPostProcessors(new AddSomeHeadersPostProcessor());
                });

                services.AddRabbitAdmin();
                services.AddRabbitTemplate((p, t) =>
                {
                    t.ReplyTimeout = 60000;
                });

                services.AddSingleton<Foo2Service>();
                services.AddRabbitListeners<Foo2Service>(config);

                return services;
            }

            public static string[] Queues = new string[]
            {
            "test.converted", "test.converted.list", "test.converted.array", "test.converted.args1",
            "test.converted.args2", "test.converted.message", "test.notconverted.message",
            "test.notconverted.channel", "test.notconverted.messagechannel", "test.notconverted.messagingmessage",
            "test.converted.foomessage", "test.notconverted.messagingmessagenotgeneric", "test.simple.direct",
            };
        }

        public class Foo2Service
        {
            public object Bean { get; set; }

            public MethodInfo Method { get; set; }

            public string StringHeader { get; set; }

            public int IntHeader { get; set; }

            [RabbitListener("test.converted")]
            public Foo2 Foo2(Foo2 foo2)
            {
                return foo2;
            }

            [RabbitListener("test.converted.list")]
            public Foo2 Foo2(List<Foo2> foo2s)
            {
                var foo2 = foo2s[0];
                foo2.Bar = "BAZZZZ";
                return foo2;
            }

            [RabbitListener("test.converted.array")]
            public Foo2 Foo2(Foo2[] foo2s)
            {
                var foo2 = foo2s[0];
                foo2.Bar = "BAZZxx";
                return foo2;
            }

            [RabbitListener("test.converted.args1")]
            public string Foo2(Foo2 foo2, [Header(RabbitMessageHeaders.CONSUMER_QUEUE)] string queue)
            {
                return foo2 + queue;
            }

            [RabbitListener("test.converted.args2")]
            public string Foo2a([Payload] Foo2 foo2, [Header(RabbitMessageHeaders.CONSUMER_QUEUE)] string queue)
            {
                return foo2 + queue;
            }

            [RabbitListener("test.converted.message")]
            public string Foo2Message([Payload] Foo2 foo2, IMessage message)
            {
                Bean = message.Headers.Target();
                Method = message.Headers.TargetMethod();
                return foo2 + Method.Name + Bean.GetType().Name;
            }

            [RabbitListener("test.notconverted.message")]
            public string JustMessage(IMessage message)
            {
                StringHeader = message.Headers.Get<string>("stringHeader");
                IntHeader = message.Headers.Get<int>("intHeader");
                return $"foo{message.GetType().Name}";
            }

            [RabbitListener("test.notconverted.channel")]
            public string JustChannel(RC.IModel channel)
            {
                return "barAndChannel";
            }

            [RabbitListener("test.notconverted.messagechannel")]
            public string MessageChannel(Foo2 foo2, IMessage message, RC.IModel channel)
            {
                return $"{foo2}{message.GetType().Name}AndChannel";
            }

            [RabbitListener("test.notconverted.messagingmessage")]
            public string MessagingMessage(IMessage message)
            {
                return message.GetType().Name + message.Payload.GetType().Name;
            }

            [RabbitListener("test.converted.foomessage")]
            public string MessagingMessage(
                IMessage<Foo2> message,
                [Header("", Required = false)] string h,
                [Header(RabbitMessageHeaders.RECEIVED_USER_ID)] string userId)
            {
                return message.GetType().Name + message.Payload.GetType().Name + userId;
            }

            [RabbitListener("test.notconverted.messagingmessagenotgeneric")]
            public string MessagingMessage(
                IMessage message,
                [Header("", Required = false)] int? h)
            {
                return message.GetType().Name + message.Payload.GetType().Name;
            }
        }

        public class Foo1
        {
            public string Bar { get; set; }
        }

        public class Foo2
        {
            public string Bar { get; set; }

            public override string ToString()
            {
                return $"bar={Bar}";
            }
        }

        public class Foo1ToFoo2Converter : AbstractGenericConverter
        {
            public Foo1ToFoo2Converter()
                : base(new HashSet<(Type, Type)> { (typeof(Foo1), typeof(Foo2)) })
            {
            }

            public Foo2 Convert(Foo1 source)
            {
                var foo2 = new Foo2
                {
                    Bar = source.Bar
                };
                return foo2;
            }

            public override object Convert(object source, Type sourceType, Type targetType)
            {
                return Convert((Foo1)source);
            }
        }

        public class AddSomeHeadersPostProcessor : IMessagePostProcessor
        {
            public IMessage PostProcessMessage(IMessage message, CorrelationData correlation)
            {
                var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
                accessor.SetHeader("bean", accessor.Target.GetType().Name);
                accessor.SetHeader("method", accessor.TargetMethod.Name);
                return message;
            }

            public IMessage PostProcessMessage(IMessage message)
            {
                var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
                accessor.SetHeader("bean", accessor.Target.GetType().Name);
                accessor.SetHeader("method", accessor.TargetMethod.Name);
                return message;
            }
        }
    }
}
