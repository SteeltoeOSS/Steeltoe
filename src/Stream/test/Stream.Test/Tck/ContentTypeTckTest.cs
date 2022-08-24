// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Common.Util;
using Steeltoe.Integration.Channel;
using Steeltoe.Integration.Configuration;
using Steeltoe.Integration.Extensions;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Support;
using Steeltoe.Stream.Binder;
using Steeltoe.Stream.Binding;
using Steeltoe.Stream.Extensions;
using Steeltoe.Stream.TestBinder;
using Xunit;

namespace Steeltoe.Stream.Tck;

public class ContentTypeTckTest : AbstractTest
{
    private IServiceCollection _container;

    public ContentTypeTckTest()
    {
        List<string> searchDirectories = GetSearchDirectories("TestBinder");
        _container = CreateStreamsContainerWithDefaultBindings(searchDirectories);
    }

    [Fact]
    public async Task StringToMapStreamListener()
    {
        _container.AddStreamListeners<StringToMapStreamListener>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart

        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();
        const string jsonPayload = "{\"name\":\"oleg\"}";
        IMessage outputMessage = DoSendReceive(provider, jsonPayload);
        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal("oleg", Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task StringToMapMessageStreamListener()
    {
        _container.AddStreamListeners<StringToMapMessageStreamListener>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart

        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        const string jsonPayload = "{\"name\":\"oleg\"}";
        IMessage outputMessage = DoSendReceive(provider, jsonPayload);
        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal("oleg", Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task StringToMapMessageStreamListenerOriginalContentType()
    {
        _container.AddStreamListeners<StringToMapMessageStreamListener>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart

        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        var source = provider.GetService<InputDestination>();
        var target = provider.GetService<OutputDestination>();
        const string jsonPayload = "{\"name\":\"oleg\"}";

        IMessage message = MessageBuilder.WithPayload(Encoding.UTF8.GetBytes(jsonPayload)).SetHeader(MessageHeaders.ContentType, "text/plain")
            .SetHeader("originalContentType", "application/json;charset=UTF-8").Build();

        source.Send((IMessage<byte[]>)message);
        IMessage outputMessage = target.Receive();
        Assert.NotNull(outputMessage);
        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal("oleg", Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task WithInternalPipeline()
    {
        _container.AddStreamListeners<InternalPipeLine>();
        _container.AddSingleton<IMessageChannel>(p => new DirectChannel(p.GetService<IApplicationContext>(), "internalchannel"));
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart

        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        const string jsonPayload = "{\"name\":\"oleg\"}";
        IMessage outputMessage = DoSendReceive(provider, jsonPayload);
        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal("OLEG", Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task PojoToPojo()
    {
        _container.AddStreamListeners<PojoToPojoStreamListener>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart

        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        const string jsonPayload = "{\"name\":\"oleg\"}";
        IMessage outputMessage = DoSendReceive(provider, jsonPayload);
        Assert.Equal(MimeTypeUtils.ApplicationJson, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task PojoToString()
    {
        _container.AddStreamListeners<PojoToStringStreamListener>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart

        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        const string jsonPayload = "{\"name\":\"oleg\"}";
        IMessage outputMessage = DoSendReceive(provider, jsonPayload);
        Assert.Equal(MimeTypeUtils.ApplicationJson, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal("oleg", Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task PojoToStringOutboundContentTypeBinding()
    {
        List<string> searchDirectories = GetSearchDirectories("TestBinder");
        _container = CreateStreamsContainerWithDefaultBindings(searchDirectories, "spring.cloud.stream.bindings.output.contentType=text/plain");
        _container.AddStreamListeners<PojoToStringStreamListener>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart

        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        const string jsonPayload = "{\"name\":\"oleg\"}";
        IMessage outputMessage = DoSendReceive(provider, jsonPayload);

        Assert.Equal(MimeTypeUtils.TextPlain, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal("oleg", Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task PojoToByteArray()
    {
        _container.AddStreamListeners<PojoToByteArrayStreamListener>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart

        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        const string jsonPayload = "{\"name\":\"oleg\"}";
        IMessage outputMessage = DoSendReceive(provider, jsonPayload);

        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal("oleg", Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task PojoToByteArrayOutboundContentTypeBinding()
    {
        List<string> searchDirectories = GetSearchDirectories("TestBinder");
        _container = CreateStreamsContainerWithDefaultBindings(searchDirectories, "spring.cloud.stream.bindings.output.contentType=text/plain");
        _container.AddStreamListeners<PojoToByteArrayStreamListener>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart

        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        const string jsonPayload = "{\"name\":\"oleg\"}";
        IMessage outputMessage = DoSendReceive(provider, jsonPayload);

        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal("oleg", Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task StringToPojoInboundContentTypeBinding()
    {
        List<string> searchDirectories = GetSearchDirectories("TestBinder");
        _container = CreateStreamsContainerWithDefaultBindings(searchDirectories, "spring.cloud.stream.bindings.input.contentType=text/plain");
        _container.AddStreamListeners<StringToPojoStreamListener>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart

        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        const string jsonPayload = "{\"name\":\"oleg\"}";
        IMessage outputMessage = DoSendReceive(provider, jsonPayload);

        Assert.Equal(MimeTypeUtils.ApplicationJson, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task TypelessToPojoInboundContentTypeBinding()
    {
        List<string> searchDirectories = GetSearchDirectories("TestBinder");
        _container = CreateStreamsContainerWithDefaultBindings(searchDirectories, "spring.cloud.stream.bindings.input.contentType=text/plain");
        _container.AddStreamListeners<TypelessToPojoStreamListener>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart

        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        const string jsonPayload = "{\"name\":\"oleg\"}";
        IMessage outputMessage = DoSendReceive(provider, jsonPayload);

        Assert.Equal(MimeTypeUtils.ApplicationJson, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task TypelessToPojoInboundContentTypeBindingJson()
    {
        List<string> searchDirectories = GetSearchDirectories("TestBinder");
        _container = CreateStreamsContainerWithDefaultBindings(searchDirectories, "spring.cloud.stream.bindings.input.contentType=application/json");
        _container.AddStreamListeners<TypelessToPojoStreamListener>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        const string jsonPayload = "{\"name\":\"oleg\"}";
        IMessage outputMessage = DoSendReceive(provider, jsonPayload);

        Assert.Equal(MimeTypeUtils.ApplicationJson, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task TypelessMessageToPojoInboundContentTypeBinding()
    {
        List<string> searchDirectories = GetSearchDirectories("TestBinder");
        _container = CreateStreamsContainerWithDefaultBindings(searchDirectories, "spring.cloud.stream.bindings.input.contentType=text/plain");
        _container.AddStreamListeners<TypelessMessageToPojoStreamListener>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        const string jsonPayload = "{\"name\":\"oleg\"}";
        IMessage outputMessage = DoSendReceive(provider, jsonPayload);

        Assert.Equal(MimeTypeUtils.ApplicationJson, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task TypelessMessageToPojoInboundContentTypeBindingJson()
    {
        List<string> searchDirectories = GetSearchDirectories("TestBinder");
        _container = CreateStreamsContainerWithDefaultBindings(searchDirectories, "spring.cloud.stream.bindings.input.contentType=application/json");
        _container.AddStreamListeners<TypelessMessageToPojoStreamListener>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        const string jsonPayload = "{\"name\":\"oleg\"}";
        IMessage outputMessage = DoSendReceive(provider, jsonPayload);

        Assert.Equal(MimeTypeUtils.ApplicationJson, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task TypelessToPojoWithTextHeaderContentTypeBinding()
    {
        _container.AddStreamListeners<TypelessMessageToPojoStreamListener>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        const string jsonPayload = "{\"name\":\"oleg\"}";

        IMessage outputMessage = DoSendReceive(provider, jsonPayload,
            new KeyValuePair<string, object>(MessageHeaders.ContentType, MimeType.ToMimeType("text/plain")));

        Assert.Equal(MimeTypeUtils.ApplicationJson, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task TypelessToPojoOutboundContentTypeBinding()
    {
        List<string> searchDirectories = GetSearchDirectories("TestBinder");
        _container = CreateStreamsContainerWithDefaultBindings(searchDirectories, "spring.cloud.stream.bindings.output.contentType=text/plain");
        _container.AddStreamListeners<TypelessToMessageStreamListener>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        const string jsonPayload = "{\"name\":\"oleg\"}";

        IMessage outputMessage = DoSendReceive(provider, jsonPayload,
            new KeyValuePair<string, object>(MessageHeaders.ContentType, MimeType.ToMimeType("text/plain")));

        Assert.Equal(MimeTypeUtils.TextPlain, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task OutboundMessageWithTextContentTypeOnly()
    {
        _container.AddStreamListeners<TypelessToMessageTextOnlyContentTypeStreamListener>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        const string jsonPayload = "{\"name\":\"oleg\"}";
        IMessage outputMessage = DoSendReceive(provider, jsonPayload, new KeyValuePair<string, object>(MessageHeaders.ContentType, new MimeType("text")));

        Assert.Equal(MimeTypeUtils.TextPlain, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        string str = Encoding.UTF8.GetString(payload);
        Assert.Equal(jsonPayload, str);
    }

    [Fact]
    public async Task StringToPojoInboundContentTypeHeader()
    {
        _container.AddStreamListeners<StringToPojoStreamListener>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        const string jsonPayload = "{\"name\":\"oleg\"}";
        IMessage outputMessage = DoSendReceive(provider, jsonPayload, new KeyValuePair<string, object>(MessageHeaders.ContentType, MimeTypeUtils.TextPlain));

        Assert.Equal(MimeTypeUtils.ApplicationJson, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task ByteArrayToPojoInboundContentTypeBinding()
    {
        List<string> searchDirectories = GetSearchDirectories("TestBinder");
        _container = CreateStreamsContainerWithDefaultBindings(searchDirectories, "spring.cloud.stream.bindings.input.contentType=text/plain");
        _container.AddStreamListeners<ByteArrayToPojoStreamListener>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        const string jsonPayload = "{\"name\":\"oleg\"}";
        IMessage outputMessage = DoSendReceive(provider, jsonPayload);

        Assert.Equal(MimeTypeUtils.ApplicationJson, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task ByteArrayToPojoInboundContentTypeHeader()
    {
        _container.AddStreamListeners<StringToPojoStreamListener>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        const string jsonPayload = "{\"name\":\"oleg\"}";

        IMessage<byte[]> message = Message.Create(Encoding.UTF8.GetBytes(jsonPayload), new MessageHeaders(new Dictionary<string, object>
        {
            { MessageHeaders.ContentType, MimeTypeUtils.TextPlain }
        }));

        IMessage outputMessage = DoSendReceive(provider, message);

        Assert.Equal(MimeTypeUtils.ApplicationJson, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task ByteArrayToByteArray()
    {
        _container.AddStreamListeners<ByteArrayToByteArrayStreamListener>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        const string jsonPayload = "{\"name\":\"oleg\"}";
        IMessage outputMessage = DoSendReceive(provider, jsonPayload);

        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task ByteArrayToByteArrayInboundOutboundContentTypeBinding()
    {
        List<string> searchDirectories = GetSearchDirectories("TestBinder");

        _container = CreateStreamsContainerWithDefaultBindings(searchDirectories, "spring.cloud.stream.bindings.input.contentType=text/plain",
            "spring.cloud.stream.bindings.output.contentType=text/plain");

        _container.AddStreamListeners<ByteArrayToByteArrayStreamListener>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        const string jsonPayload = "{\"name\":\"oleg\"}";
        IMessage outputMessage = DoSendReceive(provider, jsonPayload);

        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task PojoMessageToStringMessage()
    {
        _container.AddStreamListeners<PojoMessageToStringMessageStreamListener>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        const string jsonPayload = "{\"name\":\"oleg\"}";
        IMessage outputMessage = DoSendReceive(provider, jsonPayload);

        Assert.Equal(MimeTypeUtils.TextPlain, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal("oleg", Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task PojoMessageToStringMessageServiceActivator()
    {
        _container.AddServiceActivators<PojoMessageToStringMessageServiceActivator>();

        ServiceProvider provider = _container.BuildServiceProvider();

        var activatorProcessor = provider.GetRequiredService<ServiceActivatorAttributeProcessor>();
        activatorProcessor.Initialize();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart

        const string jsonPayload = "{\"name\":\"oleg\"}";
        IMessage outputMessage = DoSendReceive(provider, jsonPayload);

        Assert.Equal(MimeTypeUtils.TextPlain, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal("oleg", Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task ByteArrayMessageToStringJsonMessageServiceActivator()
    {
        _container.AddServiceActivators<ByteArrayMessageToStringJsonMessageServiceActivator>();
        ServiceProvider provider = _container.BuildServiceProvider();

        var activatorProcessor = provider.GetRequiredService<ServiceActivatorAttributeProcessor>();
        activatorProcessor.Initialize();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart

        const string jsonPayload = "{\"name\":\"oleg\"}";
        IMessage outputMessage = DoSendReceive(provider, jsonPayload);

        Assert.Equal(MimeTypeUtils.ApplicationJson, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal("{\"name\":\"bob\"}", Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task ByteArrayMessageToStringMessageServiceActivator()
    {
        _container.AddServiceActivators<StringMessageToStringMessageServiceActivator>();
        ServiceProvider provider = _container.BuildServiceProvider();

        var activatorProcessor = provider.GetRequiredService<ServiceActivatorAttributeProcessor>();
        activatorProcessor.Initialize();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart

        const string jsonPayload = "{\"name\":\"oleg\"}";
        IMessage outputMessage = DoSendReceive(provider, jsonPayload);

        Assert.Equal(MimeTypeUtils.TextPlain, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal("oleg", Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task OverrideMessageConverter_DefaultContentTypeBinding()
    {
        List<string> searchDirectories = GetSearchDirectories("TestBinder");
        _container = CreateStreamsContainerWithDefaultBindings(searchDirectories, "spring.cloud.stream.default.contentType=application/x-java-object");
        _container.AddStreamListeners<StringToStringStreamListener>();
        _container.AddSingleton<IMessageConverter, AlwaysStringMessageConverter>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        const string jsonPayload = "{\"name\":\"oleg\"}";
        IMessage outputMessage = DoSendReceive(provider, jsonPayload);

        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal("AlwaysStringMessageConverter", Encoding.UTF8.GetString(payload));
        Assert.Equal(MimeType.ToMimeType("application/x-java-object"), outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
    }

    [Fact]
    public async Task CustomMessageConverter_DefaultContentTypeBinding()
    {
        List<string> searchDirectories = GetSearchDirectories("TestBinder");
        _container = CreateStreamsContainerWithDefaultBindings(searchDirectories, "spring.cloud.stream.default.contentType=foo/bar");
        _container.AddStreamListeners<StringToStringStreamListener>();
        _container.AddSingleton<IMessageConverter, FooBarMessageConverter>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        const string jsonPayload = "{\"name\":\"oleg\"}";
        IMessage outputMessage = DoSendReceive(provider, jsonPayload);

        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal("FooBarMessageConverter", Encoding.UTF8.GetString(payload));
        Assert.Equal(MimeType.ToMimeType("foo/bar"), outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
    }

    [Fact]
    public async Task JsonToPojoWrongDefaultContentTypeProperty()
    {
        List<string> searchDirectories = GetSearchDirectories("TestBinder");
        _container = CreateStreamsContainerWithDefaultBindings(searchDirectories, "spring.cloud.stream.default.contentType=text/plain");
        _container.AddStreamListeners<PojoToPojoStreamListener>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        const string jsonPayload = "{\"name\":\"oleg\"}";
        DoSendReceive(provider, jsonPayload, typeof(MessagingException));
    }

    [Fact]
    public async Task ToStringDefaultContentTypePropertyUnknownContentType()
    {
        List<string> searchDirectories = GetSearchDirectories("TestBinder");
        _container = CreateStreamsContainerWithDefaultBindings(searchDirectories, "spring.cloud.stream.default.contentType=foo/bar");
        _container.AddStreamListeners<StringToStringStreamListener>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        const string jsonPayload = "{\"name\":\"oleg\"}";
        DoSendReceive(provider, jsonPayload, typeof(MessageConversionException));
    }

    [Fact]
    public async Task ToCollectionWithParameterizedType()
    {
        _container.AddStreamListeners<CollectionWithParameterizedTypesStreamListener>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        const string jsonPayload = "[{\"person\":{\"name\":\"jon\"},\"id\":123},{\"person\":{\"name\":\"jane\"},\"id\":456}]";
        IMessage outputMessage = DoSendReceive(provider, jsonPayload);

        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);

        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        var serializer = JsonSerializer.Create(settings);
        var textReader = new StringReader(Encoding.UTF8.GetString(payload));
        var list = (List<Person>)serializer.Deserialize(textReader, typeof(List<Person>));
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task TestWithMapInputParameter()
    {
        _container.AddStreamListeners<MapInputConfiguration>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        const string jsonPayload = "{\"name\":\"oleg\"}";
        IMessage outputMessage = DoSendReceive(provider, jsonPayload);

        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task TestWithMapPayloadParameter()
    {
        _container.AddStreamListeners<MapPayloadConfiguration>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        const string jsonPayload = "{\"name\":\"oleg\"}";
        IMessage outputMessage = DoSendReceive(provider, jsonPayload);

        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task TestWithListInputParameter()
    {
        _container.AddStreamListeners<ListInputConfiguration>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        const string jsonPayload = "[\"foo\",\"bar\"]";
        IMessage outputMessage = DoSendReceive(provider, jsonPayload);

        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task TestWithMessageHeadersInputParameter()
    {
        _container.AddStreamListeners<MessageHeadersInputConfiguration>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        const string jsonPayload = "{\"name\":\"oleg\"}";
        IMessage outputMessage = DoSendReceive(provider, jsonPayload);

        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.NotEqual(jsonPayload, Encoding.UTF8.GetString(payload));
        Assert.True(outputMessage.Headers.ContainsKey(MessageHeaders.IdName));
        Assert.True(outputMessage.Headers.ContainsKey(MessageHeaders.ContentType));
    }

    [Fact]
    public async Task TestWithTypelessInputParameterAndOctetStream()
    {
        _container.AddStreamListeners<TypelessPayloadConfiguration>();
        ServiceProvider provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        const string jsonPayload = "[\"foo\",\"bar\"]";

        IMessage message = MessageBuilder.WithPayload(Encoding.UTF8.GetBytes(jsonPayload)).SetHeader(MessageHeaders.ContentType, MimeTypeUtils.ApplicationJson)
            .Build();

        IMessage outputMessage = DoSendReceive(provider, (IMessage<byte[]>)message);

        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task TestWithTypelessInputParameterAndServiceActivator()
    {
        _container.AddServiceActivators<TypelessPayloadConfigurationSa>();
        ServiceProvider provider = _container.BuildServiceProvider();

        var streamProcessor = provider.GetRequiredService<ServiceActivatorAttributeProcessor>();
        streamProcessor.Initialize();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart

        const string jsonPayload = "[\"foo\",\"bar\"]";
        IMessage message = MessageBuilder.WithPayload(Encoding.UTF8.GetBytes(jsonPayload)).Build();
        IMessage outputMessage = DoSendReceive(provider, (IMessage<byte[]>)message);

        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task TestWithTypelessMessageInputParameterAndServiceActivator()
    {
        _container.AddServiceActivators<TypelessMessageConfigurationSa>();
        ServiceProvider provider = _container.BuildServiceProvider();

        var streamProcessor = provider.GetRequiredService<ServiceActivatorAttributeProcessor>();
        streamProcessor.Initialize();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart

        const string jsonPayload = "[\"foo\",\"bar\"]";
        IMessage message = MessageBuilder.WithPayload(Encoding.UTF8.GetBytes(jsonPayload)).Build();
        IMessage outputMessage = DoSendReceive(provider, (IMessage<byte[]>)message);

        byte[] payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    private void DoSendReceive(ServiceProvider provider, string jsonPayload, Type lastError)
    {
        var source = provider.GetService<InputDestination>();
        _ = provider.GetService<OutputDestination>();
        source.Send(Message.Create(Encoding.UTF8.GetBytes(jsonPayload)));
        var binder = provider.GetService<IBinder>() as TestChannelBinder;
        Assert.Equal(lastError, binder.LastError?.Payload?.GetType());
    }

    private IMessage DoSendReceive(ServiceProvider provider, string jsonPayload)
    {
        var source = provider.GetService<InputDestination>();
        var target = provider.GetService<OutputDestination>();
        source.Send(Message.Create(Encoding.UTF8.GetBytes(jsonPayload)));
        IMessage outputMessage = target.Receive();
        Assert.NotNull(outputMessage);
        return outputMessage;
    }

    private IMessage DoSendReceive(ServiceProvider provider, IMessage<byte[]> message)
    {
        var source = provider.GetService<InputDestination>();
        var target = provider.GetService<OutputDestination>();
        source.Send(message);
        IMessage outputMessage = target.Receive();
        Assert.NotNull(outputMessage);
        return outputMessage;
    }

    private IMessage DoSendReceive(ServiceProvider provider, string jsonPayload, params KeyValuePair<string, object>[] headers)
    {
        var source = provider.GetService<InputDestination>();
        var target = provider.GetService<OutputDestination>();
        AbstractMessageBuilder builder = MessageBuilder.WithPayload(Encoding.UTF8.GetBytes(jsonPayload));

        foreach (KeyValuePair<string, object> header in headers)
        {
            builder.SetHeader(header.Key, header.Value);
        }

        source.Send((IMessage<byte[]>)builder.Build());
        IMessage outputMessage = target.Receive();
        Assert.NotNull(outputMessage);
        return outputMessage;
    }
}
