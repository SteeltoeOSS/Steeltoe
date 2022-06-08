// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Contexts;
using Steeltoe.Connector.RabbitMQ;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Listener;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Steeltoe.Messaging.RabbitMQ.Extensions;

public class RabbitServiceExtensionsTest
{
    [Fact]
    public void AddRabbitTemplate_DefaultName()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRabbitHostingServices();
        services.AddRabbitConnectionFactory();
        services.AddRabbitDefaultMessageConverter();
        services.AddRabbitTemplate();
        var provider = services.BuildServiceProvider();
        var template = provider.GetRabbitTemplate();
        Assert.NotNull(template);
        Assert.Equal(RabbitTemplate.DEFAULT_SERVICE_NAME, template.ServiceName);
        var context = provider.GetService<IApplicationContext>();
        Assert.Same(template, context.GetService<RabbitTemplate>(RabbitTemplate.DEFAULT_SERVICE_NAME));
        Assert.Same(template, provider.GetService<IRabbitTemplate>());
        Assert.Same(template, context.GetService<IRabbitTemplate>());
    }

    [Fact]
    public void AddRabbitTemplate_SingleName()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRabbitHostingServices();
        services.AddRabbitConnectionFactory();
        services.AddRabbitDefaultMessageConverter();
        services.AddRabbitTemplate("foo");
        var provider = services.BuildServiceProvider();
        var template = provider.GetRabbitTemplate("foo");
        Assert.NotNull(template);
        Assert.Equal("foo", template.ServiceName);
        var context = provider.GetService<IApplicationContext>();
        Assert.Same(template, context.GetService<RabbitTemplate>("foo"));
        Assert.Same(template, context.GetService<IRabbitTemplate>("foo"));
    }

    [Fact]
    public void AddRabbitTemplate_MultipleNames()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRabbitHostingServices();
        services.AddRabbitConnectionFactory();
        services.AddRabbitDefaultMessageConverter();
        services.AddRabbitTemplate();
        services.AddRabbitTemplate("foo");
        services.AddRabbitTemplate("bar");
        var provider = services.BuildServiceProvider();
        var context = provider.GetService<IApplicationContext>();
        var template = provider.GetRabbitTemplate();
        Assert.NotNull(template);
        Assert.Equal(RabbitTemplate.DEFAULT_SERVICE_NAME, template.ServiceName);
        Assert.Same(template, context.GetService<RabbitTemplate>(RabbitTemplate.DEFAULT_SERVICE_NAME));
        Assert.Same(template, context.GetService<IRabbitTemplate>(RabbitTemplate.DEFAULT_SERVICE_NAME));
        var template1 = provider.GetRabbitTemplate("foo");
        Assert.NotNull(template1);
        Assert.Same(template1, context.GetService<RabbitTemplate>("foo"));
        Assert.Same(template1, context.GetService<IRabbitTemplate>("foo"));
        Assert.Equal("foo", template1.ServiceName);
        var template2 = provider.GetRabbitTemplate("bar");
        Assert.NotNull(template2);
        Assert.Same(template2, context.GetService<RabbitTemplate>("bar"));
        Assert.Same(template2, context.GetService<IRabbitTemplate>("bar"));
        Assert.Equal("bar", template2.ServiceName);
        var all = provider.GetServices<RabbitTemplate>();
        Assert.Equal(3, all.Count());
    }

    [Fact]
    public void AddRabbitTemplate_Configure()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRabbitHostingServices();
        services.AddRabbitConnectionFactory();
        services.AddRabbitDefaultMessageConverter();
        services.AddRabbitTemplate((p, t) =>
        {
            t.CorrelationKey = "foobar";
        });

        var provider = services.BuildServiceProvider();
        var template = provider.GetRabbitTemplate();
        Assert.NotNull(template);
        Assert.Equal("foobar", template.CorrelationKey);
    }

    [Fact]
    public void AddRabbitAdmin_DefaultName()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRabbitHostingServices();
        services.AddRabbitConnectionFactory();
        services.AddRabbitDefaultMessageConverter();
        services.AddRabbitAdmin();
        var provider = services.BuildServiceProvider();
        var admin = provider.GetRabbitAdmin();
        Assert.NotNull(admin);
        Assert.Equal(RabbitAdmin.DEFAULT_SERVICE_NAME, admin.ServiceName);
        var context = provider.GetService<IApplicationContext>();
        Assert.Same(admin, context.GetService<RabbitAdmin>(RabbitAdmin.DEFAULT_SERVICE_NAME));
        Assert.Same(admin, context.GetService<IRabbitAdmin>(RabbitAdmin.DEFAULT_SERVICE_NAME));
    }

    [Fact]
    public void AddRabbitAdmin_SingleName()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRabbitHostingServices();
        services.AddRabbitConnectionFactory();
        services.AddRabbitDefaultMessageConverter();
        services.AddRabbitAdmin("foo");
        var provider = services.BuildServiceProvider();
        var admin = provider.GetRabbitAdmin("foo");
        Assert.NotNull(admin);
        Assert.Equal("foo", admin.ServiceName);
        var context = provider.GetService<IApplicationContext>();
        Assert.Same(admin, context.GetService<RabbitAdmin>("foo"));
        Assert.Same(admin, context.GetService<IRabbitAdmin>("foo"));
    }

    [Fact]
    public void AddRabbitAdmin_MultipleNames()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRabbitHostingServices();
        services.AddRabbitConnectionFactory();
        services.AddRabbitDefaultMessageConverter();
        services.AddRabbitAdmin();
        services.AddRabbitAdmin("foo");
        services.AddRabbitAdmin("bar");
        var provider = services.BuildServiceProvider();
        var context = provider.GetService<IApplicationContext>();
        var admin = provider.GetRabbitAdmin();
        Assert.NotNull(admin);
        Assert.Equal(RabbitAdmin.DEFAULT_SERVICE_NAME, admin.ServiceName);
        Assert.Same(admin, context.GetService<RabbitAdmin>(RabbitAdmin.DEFAULT_SERVICE_NAME));
        Assert.Same(admin, context.GetService<IRabbitAdmin>(RabbitAdmin.DEFAULT_SERVICE_NAME));
        var admin1 = provider.GetRabbitAdmin("foo");
        Assert.NotNull(admin1);
        Assert.Same(admin1, context.GetService<RabbitAdmin>("foo"));
        Assert.Same(admin1, context.GetService<IRabbitAdmin>("foo"));
        Assert.Equal("foo", admin1.ServiceName);
        var admin2 = provider.GetRabbitAdmin("bar");
        Assert.NotNull(admin2);
        Assert.Same(admin2, context.GetService<RabbitAdmin>("bar"));
        Assert.Same(admin2, context.GetService<IRabbitAdmin>("bar"));
        Assert.Equal("bar", admin2.ServiceName);

        var all = provider.GetServices<RabbitAdmin>();
        Assert.Equal(3, all.Count());
    }

    [Fact]
    public void AddRabbitAdmin_Configure()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRabbitHostingServices();
        services.AddRabbitConnectionFactory();
        services.AddRabbitDefaultMessageConverter();
        services.AddRabbitAdmin((p, a) =>
        {
            a.RetryDisabled = true;
        });
        var provider = services.BuildServiceProvider();
        var admin = provider.GetRabbitAdmin();
        Assert.NotNull(admin);
        Assert.True(admin.RetryDisabled);
    }

    [Fact]
    public void AddRabbitQueues()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRabbitHostingServices();
        services.AddRabbitQueues(new Queue("1"), new Queue("2"));
        var provider = services.BuildServiceProvider();
        var context = provider.GetService<IApplicationContext>();
        var q1 = context.GetService<IQueue>("1");
        var q2 = context.GetService<IQueue>("2");
        Assert.NotNull(q1);
        Assert.NotNull(q2);
    }

    [Fact]
    public void AddRabbitQueue_Configure()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRabbitHostingServices();
        services.AddRabbitQueue("myQueue", (p, q) =>
        {
            q.IsDurable = false;
            q.ShouldDeclare = false;
        });
        var provider = services.BuildServiceProvider();
        var context = provider.GetService<IApplicationContext>();
        var q1 = context.GetService<IQueue>("myQueue");
        Assert.NotNull(q1);
        Assert.Equal("myQueue", q1.QueueName);
        Assert.False(q1.ShouldDeclare);
        Assert.False(q1.IsDurable);
    }

    [Fact]
    public void AddRabbitBindings()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRabbitHostingServices();
        services.AddRabbitBindings(new QueueBinding("1"), new ExchangeBinding("2"));
        var provider = services.BuildServiceProvider();
        var context = provider.GetService<IApplicationContext>();
        var b1 = context.GetService<IBinding>("1");
        var b2 = context.GetService<IBinding>("2");
        Assert.NotNull(b1);
        Assert.NotNull(b2);
    }

    [Fact]
    public void AddRabbitBinding_Configure()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRabbitHostingServices();
        services.AddRabbitBinding("myBinding", Binding.DestinationType.QUEUE, (p, b) =>
        {
            b.ShouldDeclare = false;
            b.IgnoreDeclarationExceptions = false;
        });
        var provider = services.BuildServiceProvider();
        var context = provider.GetService<IApplicationContext>();
        var b1 = context.GetService<IBinding>("myBinding") as QueueBinding;
        Assert.NotNull(b1);
        Assert.Equal("myBinding", b1.BindingName);
        Assert.False(b1.ShouldDeclare);
        Assert.False(b1.IgnoreDeclarationExceptions);
    }

    [Fact]
    public void AddRabbitExchanges()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRabbitHostingServices();
        services.AddRabbitExchanges(new DirectExchange("1"), new FanoutExchange("2"));
        var provider = services.BuildServiceProvider();
        var context = provider.GetService<IApplicationContext>();
        var e1 = context.GetService<IExchange>("1");
        var e2 = context.GetService<IExchange>("2");
        Assert.NotNull(e1);
        Assert.NotNull(e2);
    }

    [Fact]
    public void AddRabbitExchange_Configure()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRabbitHostingServices();
        services.AddRabbitExchange("myExchange", ExchangeType.DIRECT, (p, e) =>
        {
            e.IsDurable = false;
            e.ShouldDeclare = false;
        });
        var provider = services.BuildServiceProvider();
        var context = provider.GetService<IApplicationContext>();
        var e1 = context.GetService<IExchange>("myExchange") as DirectExchange;
        Assert.NotNull(e1);
        Assert.Equal("myExchange", e1.ExchangeName);
        Assert.False(e1.ShouldDeclare);
        Assert.False(e1.IsDurable);
    }

    [Fact]
    public void AddRabbitListenerAttributeProcessor_Configure()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRabbitHostingServices();
        services.AddRabbitMessageHandlerMethodFactory();
        services.AddRabbitListenerEndpointRegistry();
        services.AddRabbitListenerEndpointRegistrar();
        services.AddRabbitListenerAttributeProcessor((p, a) =>
        {
            a.Charset = Encoding.UTF32;
        });
        var provider = services.BuildServiceProvider();
        var ap = provider.GetService<IRabbitListenerAttributeProcessor>() as RabbitListenerAttributeProcessor;
        Assert.NotNull(ap);
        Assert.Equal(Encoding.UTF32, ap.Charset);
    }

    [Fact]
    public void AddRabbitListenerEndpointRegistrar_Configure()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRabbitHostingServices();
        services.AddRabbitListenerEndpointRegistry();
        services.AddRabbitListenerEndpointRegistrar((p, r) =>
        {
            r.ContainerFactoryServiceName = "foobar";
        });

        var provider = services.BuildServiceProvider();
        var r = provider.GetService<IRabbitListenerEndpointRegistrar>() as RabbitListenerEndpointRegistrar;
        Assert.NotNull(r);
        Assert.Equal("foobar", r.ContainerFactoryServiceName);
    }

    [Fact]
    public void AddRabbitListenerEndpointRegistry_Configure()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRabbitHostingServices();
        services.AddRabbitListenerEndpointRegistry((p, r) =>
        {
            r.Phase = 100;
        });

        var provider = services.BuildServiceProvider();
        var r = provider.GetService<IRabbitListenerEndpointRegistry>() as RabbitListenerEndpointRegistry;
        Assert.NotNull(r);
        Assert.Equal(100, r.Phase);
    }

    [Fact]
    public void AddRabbitListenerContainerFactory_Configure()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRabbitHostingServices();
        services.AddRabbitConnectionFactory();
        services.AddRabbitListenerContainerFactory((p, f) =>
        {
            f.ServiceName = "foobar";
            f.AckTimeout = 111111;
        });

        var provider = services.BuildServiceProvider();
        var f = provider.GetService<IRabbitListenerContainerFactory>() as DirectRabbitListenerContainerFactory;
        Assert.NotNull(f);
        Assert.Equal(111111, f.AckTimeout);
        Assert.Equal("foobar", f.ServiceName);
        var context = provider.GetService<IApplicationContext>();
        Assert.Same(f, context.GetService<IRabbitListenerContainerFactory>("foobar"));
    }

    [Fact]
    public void AddRabbitListenerContainerFactory_MultipleNamed()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRabbitHostingServices();
        services.AddRabbitConnectionFactory();
        services.AddRabbitListenerContainerFactory("foobar");
        services.AddRabbitListenerContainerFactory("barfoo");

        var provider = services.BuildServiceProvider();
        var context = provider.GetService<IApplicationContext>();
        var f1 = context.GetService<IRabbitListenerContainerFactory>("foobar");
        var f2 = context.GetService<IRabbitListenerContainerFactory>("barfoo");
        Assert.NotSame(f1, f2);
        Assert.Equal("foobar", f1.ServiceName);
        Assert.Equal("barfoo", f2.ServiceName);
    }

    [Fact]
    public void AddRabbitListenerContainerFactory_MultipleConfigure()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRabbitHostingServices();
        services.AddRabbitConnectionFactory();
        services.AddRabbitListenerContainerFactory((p, f) =>
        {
            f.ServiceName = "foobar";
            f.AckTimeout = 111111;
        });
        services.AddRabbitListenerContainerFactory((p, f) =>
        {
            f.ServiceName = "barfoo";
            f.AckTimeout = 222222;
        });

        var provider = services.BuildServiceProvider();
        var context = provider.GetService<IApplicationContext>();
        var f1 = context.GetService<IRabbitListenerContainerFactory>("foobar") as DirectRabbitListenerContainerFactory;
        var f2 = context.GetService<IRabbitListenerContainerFactory>("barfoo") as DirectRabbitListenerContainerFactory;
        Assert.NotSame(f1, f2);
        Assert.Equal("foobar", f1.ServiceName);
        Assert.Equal(111111, f1.AckTimeout);
        Assert.Equal("barfoo", f2.ServiceName);
        Assert.Equal(222222, f2.AckTimeout);
    }

    [Fact]
    public void AddRabbitContainerFactory_Configure()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRabbitHostingServices();
        services.AddRabbitConnectionFactory((p, f) =>
        {
            f.Port = 9999;
        });

        var provider = services.BuildServiceProvider();
        var f = provider.GetService<IConnectionFactory>() as CachingConnectionFactory;
        Assert.NotNull(f);
        Assert.Equal(9999, f.Port);
    }

    [Fact]
    public void AddDefaultRabbitMessageConverter_Configure()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRabbitHostingServices();
        services.AddRabbitDefaultMessageConverter((p, c) =>
        {
            c.CreateMessageIds = true;
        });

        var provider = services.BuildServiceProvider();
        var c = provider.GetService<ISmartMessageConverter>() as Support.Converter.SimpleMessageConverter;
        Assert.NotNull(c);
        Assert.True(c.CreateMessageIds);
    }

    [Fact]
    public void AddRabbitMessageConverter_Configure()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRabbitHostingServices();
        services.AddRabbitMessageConverter<Support.Converter.JsonMessageConverter>((p, c) =>
        {
            c.AssumeSupportedContentType = false;
        });

        var provider = services.BuildServiceProvider();
        var c = provider.GetService<ISmartMessageConverter>() as Support.Converter.JsonMessageConverter;
        Assert.NotNull(c);
        Assert.False(c.AssumeSupportedContentType);
    }

    [Fact]
    public void ConfigureRabbitOptions_Configure()
    {
        var hostPrefix = "spring:rabbitmq:host";
        var portPrefix = "spring:rabbitmq:port";
        var usernamePrefix = "spring:rabbitmq:username";
        var passwordPrefix = "spring:rabbitmq:password";
        var services = new ServiceCollection();

        var appsettings = new Dictionary<string, string>
        {
            [hostPrefix] = "this.is.test",
            [portPrefix] = "12345",
            [usernamePrefix] = "fakeusername",
            [passwordPrefix] = "CHANGEME",
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        var configuration = configurationBuilder.Build();

        services.ConfigureRabbitOptions(configuration);

        var provider = services.BuildServiceProvider();
        var rabbitOptions = provider.GetService<IOptions<RabbitOptions>>().Value;

        Assert.Equal(appsettings[hostPrefix], rabbitOptions.Host);
        Assert.Equal(appsettings[portPrefix], rabbitOptions.Port.ToString());
        Assert.Equal(appsettings[usernamePrefix], rabbitOptions.Username);
        Assert.Equal(appsettings[passwordPrefix], rabbitOptions.Password);
    }

    [Fact]
    public void ConfigureRabbitOptions_OverrideAddressWithServiceInfo()
    {
        var usernamePrefix = "spring:rabbitmq:username";
        var passwordPrefix = "spring:rabbitmq:password";
        var services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", GetCloudFoundryRabbitMqConfiguration());

        var appsettings = new Dictionary<string, string>
        {
            [usernamePrefix] = "fakeusername",
            [passwordPrefix] = "CHANGEME",
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        configurationBuilder.AddCloudFoundry();
        var configuration = configurationBuilder.Build();

        services.AddRabbitMQConnection(configuration);
        services.ConfigureRabbitOptions(configuration);

        var provider = services.BuildServiceProvider();
        var rabbitOptions = provider.GetRequiredService<IOptions<RabbitOptions>>().Value;

        Assert.Equal("Dd6O1BPXUHdrmzbP", rabbitOptions.Username);
        Assert.Equal("7E1LxXnlH2hhlPVt", rabbitOptions.Password);
        Assert.Equal("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", rabbitOptions.VirtualHost);
        Assert.Equal("Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306", rabbitOptions.Addresses);
    }

    [Fact]
    public void AddRabbitConnectionFactory_AddRabbitConnector()
    {
        var services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", GetCloudFoundryRabbitMqConfiguration());

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddCloudFoundry();
        var configuration = configurationBuilder.Build();

        services.AddRabbitMQConnection(configuration);
        services.AddRabbitConnectionFactory();

        var provider = services.BuildServiceProvider();
        var rabbitConnectionFactory = provider.GetRequiredService<IConnectionFactory>();

        Assert.Equal("192.168.0.90", rabbitConnectionFactory.Host);
        Assert.Equal(3306, rabbitConnectionFactory.Port);
    }

    private static string GetCloudFoundryRabbitMqConfiguration() => @"
        {
            ""p-rabbitmq"": [{
                ""credentials"": {
                    ""uri"": ""amqp://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355""
                },
                ""syslog_drain_url"": null,
                ""label"": ""p-rabbitmq"",
                ""provider"": null,
                ""plan"": ""standard"",
                ""name"": ""myRabbitMQService1"",
                ""tags"": [
                    ""rabbitmq"",
                    ""amqp""
                ]
            }]
        }";
}
