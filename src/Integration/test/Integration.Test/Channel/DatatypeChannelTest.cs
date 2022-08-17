// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Converter;
using Steeltoe.Integration.Support;
using Steeltoe.Integration.Support.Converter;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using Xunit;

namespace Steeltoe.Integration.Channel.Test;

public class DataTypeChannelTest
{
    [Fact]
    public void SupportedType()
    {
        IMessageChannel channel = CreateChannel(typeof(string));
        Assert.True(channel.Send(Message.Create("test")));
    }

    [Fact]
    public void UnsupportedTypeAndNoConversionService()
    {
        IMessageChannel channel = CreateChannel(typeof(int));
        Assert.Throws<MessageDeliveryException>(() => channel.Send(Message.Create("123")));
    }

    [Fact]
    public void UnsupportedTypeButConversionServiceSupports()
    {
        QueueChannel channel = CreateChannel(typeof(int));
        IConversionService conversionService = new DefaultConversionService();
        var converter = new DefaultDataTypeChannelMessageConverter(conversionService);
        channel.MessageConverter = converter;
        Assert.True(channel.Send(Message.Create("123")));
    }

    [Fact]
    public void UnsupportedTypeAndConversionServiceDoesNotSupport()
    {
        QueueChannel channel = CreateChannel(typeof(int));
        IConversionService conversionService = new DefaultConversionService();
        var converter = new DefaultDataTypeChannelMessageConverter(conversionService);
        channel.MessageConverter = converter;
        Assert.Throws<MessageDeliveryException>(() => channel.Send(Message.Create(true)));
    }

    [Fact]
    public void UnsupportedTypeButCustomConversionServiceSupports()
    {
        QueueChannel channel = CreateChannel(typeof(int));
        GenericConversionService conversionService = new DefaultConversionService();
        conversionService.AddConverter(new BoolToIntConverter());
        var converter = new DefaultDataTypeChannelMessageConverter(conversionService);
        channel.MessageConverter = converter;
        Assert.True(channel.Send(Message.Create(true)));
        Assert.Equal(1, channel.Receive().Payload);
    }

    [Fact]
    public void ConversionServiceUsedByDefault()
    {
        var converter = new BoolToIntConverter();
        var convService = new GenericConversionService();
        convService.AddConverter(converter);
        var services = new ServiceCollection();
        IConfigurationRoot config = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(config);
        services.AddSingleton<IApplicationContext, GenericApplicationContext>();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        services.AddSingleton<IConversionService>(convService);
        services.AddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();
        services.AddSingleton<DefaultDataTypeChannelMessageConverter>();
        ServiceProvider provider = services.BuildServiceProvider();

        var channel = new QueueChannel(provider.GetService<IApplicationContext>(), "testChannel")
        {
            DataTypes = new List<Type>
            {
                typeof(int),
                typeof(DateTime)
            }
        };

        Assert.True(channel.Send(Message.Create(true)));
        Assert.Equal(1, channel.Receive().Payload);
    }

    [Fact]
    public void MultipleTypes()
    {
        IMessageChannel channel = CreateChannel(typeof(string), typeof(int));
        Assert.True(channel.Send(Message.Create("test1")));
        Assert.True(channel.Send(Message.Create(2)));
        Exception exception = null;

        try
        {
            channel.Send(Message.Create<DateTime>(default));
        }
        catch (MessageDeliveryException e)
        {
            exception = e;
        }

        Assert.NotNull(exception);
    }

    [Fact]
    public void SubclassOfAcceptedType()
    {
        IMessageChannel channel = CreateChannel(typeof(Exception));
        Assert.True(channel.Send(new ErrorMessage(new MessagingException("test"))));
    }

    [Fact]
    public void SuperclassOfAcceptedTypeNotAccepted()
    {
        IMessageChannel channel = CreateChannel(typeof(InvalidOperationException));
        Assert.Throws<MessageDeliveryException>(() => channel.Send(new ErrorMessage(new Exception("test"))));
    }

    [Fact]
    public void GenericConverters()
    {
        QueueChannel channel = CreateChannel(typeof(Foo));
        var conversionService = new DefaultConversionService();
        conversionService.AddConverter(new StringToBarConverter());
        conversionService.AddConverter(new IntegerToBazConverter());
        var converter = new DefaultDataTypeChannelMessageConverter(conversionService);
        channel.MessageConverter = converter;
        Assert.True(channel.Send(Message.Create("foo")));
        IMessage outMessage = channel.Receive(0);
        Assert.IsType<Bar>(outMessage.Payload);
        Assert.True(channel.Send(Message.Create(42)));
        outMessage = channel.Receive(0);
        Assert.IsType<Baz>(outMessage.Payload);
    }

    private static QueueChannel CreateChannel(params Type[] dataTypes)
    {
        var services = new ServiceCollection();
        IConfigurationRoot config = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(config);
        services.AddSingleton<IApplicationContext, GenericApplicationContext>();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        services.AddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();
        ServiceProvider provider = services.BuildServiceProvider();

        var channel = new QueueChannel(provider.GetService<IApplicationContext>(), "testChannel")
        {
            DataTypes = new List<Type>(dataTypes)
        };

        return channel;
    }

    private sealed class BoolToIntConverter : IGenericConverter
    {
        public ISet<(Type SourceType, Type TargetType)> ConvertibleTypes { get; } = new HashSet<(Type, Type)>
        {
            (typeof(bool), typeof(int))
        };

        public object Convert(object source, Type sourceType, Type targetType)
        {
            bool asBool = (bool)source;
            return asBool ? 1 : 0;
        }
    }

    private abstract class Foo
    {
    }

    private sealed class Bar : Foo
    {
    }

    private sealed class Baz : Foo
    {
    }

    private sealed class StringToBarConverter : IGenericConverter
    {
        public ISet<(Type SourceType, Type TargetType)> ConvertibleTypes { get; } = new HashSet<(Type, Type)>
        {
            (typeof(string), typeof(Foo)),
            (typeof(string), typeof(Bar))
        };

        public object Convert(object source, Type sourceType, Type targetType)
        {
            return new Bar();
        }
    }

    private sealed class IntegerToBazConverter : IGenericConverter
    {
        public ISet<(Type SourceType, Type TargetType)> ConvertibleTypes { get; } = new HashSet<(Type, Type)>
        {
            (typeof(int), typeof(Foo)),
            (typeof(int), typeof(Baz))
        };

        public object Convert(object source, Type sourceType, Type targetType)
        {
            return new Baz();
        }
    }
}
