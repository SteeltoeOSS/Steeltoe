// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Common.Util;
using Steeltoe.Integration.Channel;
using Steeltoe.Integration.Config;
using Steeltoe.Integration.Extensions;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Support;
using Steeltoe.Stream.Binder;
using Steeltoe.Stream.Binding;
using Steeltoe.Stream.Extensions;
using Steeltoe.Stream.TestBinder;
using System.Text;
using Xunit;

namespace Steeltoe.Stream.Tck;

public class ContentTypeTckTest : AbstractTest
{
    private IServiceCollection _container;

    public ContentTypeTckTest()
    {
        var searchDirectories = GetSearchDirectories("TestBinder");
        _container = CreateStreamsContainerWithDefaultBindings(searchDirectories);
    }

    [Fact]
    public async Task StringToMapStreamListener()
    {
        _container.AddStreamListeners<StringToMapStreamListener>();
        var provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();
        var jsonPayload = "{\"name\":\"oleg\"}";
        var outputMessage = DoSendReceive(provider, jsonPayload);
        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal("oleg", Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task StringToMapMessageStreamListener()
    {
        _container.AddStreamListeners<StringToMapMessageStreamListener>();
        var provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        var jsonPayload = "{\"name\":\"oleg\"}";
        var outputMessage = DoSendReceive(provider, jsonPayload);
        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal("oleg", Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task StringToMapMessageStreamListenerOriginalContentType()
    {
        _container.AddStreamListeners<StringToMapMessageStreamListener>();
        var provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        var source = provider.GetService<InputDestination>();
        var target = provider.GetService<OutputDestination>();
        var jsonPayload = "{\"name\":\"oleg\"}";
        var message = MessageBuilder.WithPayload(Encoding.UTF8.GetBytes(jsonPayload))
            .SetHeader(MessageHeaders.ContentType, "text/plain")
            .SetHeader("originalContentType", "application/json;charset=UTF-8")
            .Build();
        source.Send((IMessage<byte[]>)message);
        var outputMessage = target.Receive();
        Assert.NotNull(outputMessage);
        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal("oleg", Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task WithInternalPipeline()
    {
        _container.AddStreamListeners<InternalPipeLine>();
        _container.AddSingleton<IMessageChannel>(p => new DirectChannel(p.GetService<IApplicationContext>(), "internalchannel"));
        var provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        var jsonPayload = "{\"name\":\"oleg\"}";
        var outputMessage = DoSendReceive(provider, jsonPayload);
        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal("OLEG", Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task PojoToPojo()
    {
        _container.AddStreamListeners<PojoToPojoStreamListener>();
        var provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        var jsonPayload = "{\"name\":\"oleg\"}";
        var outputMessage = DoSendReceive(provider, jsonPayload);
        Assert.Equal(MimeTypeUtils.ApplicationJson, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task PojoToString()
    {
        _container.AddStreamListeners<PojoToStringStreamListener>();
        var provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        var jsonPayload = "{\"name\":\"oleg\"}";
        var outputMessage = DoSendReceive(provider, jsonPayload);
        Assert.Equal(MimeTypeUtils.ApplicationJson, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal("oleg", Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task PojoToStringOutboundContentTypeBinding()
    {
        var searchDirectories = GetSearchDirectories("TestBinder");
        _container = CreateStreamsContainerWithDefaultBindings(
            searchDirectories,
            "spring.cloud.stream.bindings.output.contentType=text/plain");
        _container.AddStreamListeners<PojoToStringStreamListener>();
        var provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        var jsonPayload = "{\"name\":\"oleg\"}";
        var outputMessage = DoSendReceive(provider, jsonPayload);

        Assert.Equal(MimeTypeUtils.TextPlain, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal("oleg", Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task PojoToByteArray()
    {
        _container.AddStreamListeners<PojoToByteArrayStreamListener>();
        var provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        var jsonPayload = "{\"name\":\"oleg\"}";
        var outputMessage = DoSendReceive(provider, jsonPayload);

        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal("oleg", Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task PojoToByteArrayOutboundContentTypeBinding()
    {
        var searchDirectories = GetSearchDirectories("TestBinder");
        _container = CreateStreamsContainerWithDefaultBindings(
            searchDirectories,
            "spring.cloud.stream.bindings.output.contentType=text/plain");
        _container.AddStreamListeners<PojoToByteArrayStreamListener>();
        var provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        var jsonPayload = "{\"name\":\"oleg\"}";
        var outputMessage = DoSendReceive(provider, jsonPayload);

        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal("oleg", Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task StringToPojoInboundContentTypeBinding()
    {
        var searchDirectories = GetSearchDirectories("TestBinder");
        _container = CreateStreamsContainerWithDefaultBindings(
            searchDirectories,
            "spring.cloud.stream.bindings.input.contentType=text/plain");
        _container.AddStreamListeners<StringToPojoStreamListener>();
        var provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        var jsonPayload = "{\"name\":\"oleg\"}";
        var outputMessage = DoSendReceive(provider, jsonPayload);

        Assert.Equal(MimeTypeUtils.ApplicationJson, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task TypelessToPojoInboundContentTypeBinding()
    {
        var searchDirectories = GetSearchDirectories("TestBinder");
        _container = CreateStreamsContainerWithDefaultBindings(
            searchDirectories,
            "spring.cloud.stream.bindings.input.contentType=text/plain");
        _container.AddStreamListeners<TypelessToPojoStreamListener>();
        var provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        var jsonPayload = "{\"name\":\"oleg\"}";
        var outputMessage = DoSendReceive(provider, jsonPayload);

        Assert.Equal(MimeTypeUtils.ApplicationJson, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task TypelessToPojoInboundContentTypeBindingJson()
    {
        var searchDirectories = GetSearchDirectories("TestBinder");
        _container = CreateStreamsContainerWithDefaultBindings(
            searchDirectories,
            "spring.cloud.stream.bindings.input.contentType=application/json");
        _container.AddStreamListeners<TypelessToPojoStreamListener>();
        var provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        var jsonPayload = "{\"name\":\"oleg\"}";
        var outputMessage = DoSendReceive(provider, jsonPayload);

        Assert.Equal(MimeTypeUtils.ApplicationJson, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task TypelessMessageToPojoInboundContentTypeBinding()
    {
        var searchDirectories = GetSearchDirectories("TestBinder");
        _container = CreateStreamsContainerWithDefaultBindings(
            searchDirectories,
            "spring.cloud.stream.bindings.input.contentType=text/plain");
        _container.AddStreamListeners<TypelessMessageToPojoStreamListener>();
        var provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        var jsonPayload = "{\"name\":\"oleg\"}";
        var outputMessage = DoSendReceive(provider, jsonPayload);

        Assert.Equal(MimeTypeUtils.ApplicationJson, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task TypelessMessageToPojoInboundContentTypeBindingJson()
    {
        var searchDirectories = GetSearchDirectories("TestBinder");
        _container = CreateStreamsContainerWithDefaultBindings(
            searchDirectories,
            "spring.cloud.stream.bindings.input.contentType=application/json");
        _container.AddStreamListeners<TypelessMessageToPojoStreamListener>();
        var provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        var jsonPayload = "{\"name\":\"oleg\"}";
        var outputMessage = DoSendReceive(provider, jsonPayload);

        Assert.Equal(MimeTypeUtils.ApplicationJson, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task TypelessToPojoWithTextHeaderContentTypeBinding()
    {
        _container.AddStreamListeners<TypelessMessageToPojoStreamListener>();
        var provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        var jsonPayload = "{\"name\":\"oleg\"}";
        var outputMessage = DoSendReceive(provider, jsonPayload, new KeyValuePair<string, object>(MessageHeaders.ContentType, MimeType.ToMimeType("text/plain")));

        Assert.Equal(MimeTypeUtils.ApplicationJson, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task TypelessToPojoOutboundContentTypeBinding()
    {
        var searchDirectories = GetSearchDirectories("TestBinder");
        _container = CreateStreamsContainerWithDefaultBindings(
            searchDirectories,
            "spring.cloud.stream.bindings.output.contentType=text/plain");
        _container.AddStreamListeners<TypelessToMessageStreamListener>();
        var provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        var jsonPayload = "{\"name\":\"oleg\"}";
        var outputMessage = DoSendReceive(provider, jsonPayload, new KeyValuePair<string, object>(MessageHeaders.ContentType, MimeType.ToMimeType("text/plain")));

        Assert.Equal(MimeTypeUtils.TextPlain, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task OutboundMessageWithTextContentTypeOnly()
    {
        _container.AddStreamListeners<TypelessToMessageTextOnlyContentTypeStreamListener>();
        var provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        var jsonPayload = "{\"name\":\"oleg\"}";
        var outputMessage = DoSendReceive(provider, jsonPayload, new KeyValuePair<string, object>(MessageHeaders.ContentType, new MimeType("text")));

        Assert.Equal(MimeTypeUtils.TextPlain, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        var str = Encoding.UTF8.GetString(payload);
        Assert.Equal(jsonPayload, str);
    }

    [Fact]
    public async Task StringToPojoInboundContentTypeHeader()
    {
        _container.AddStreamListeners<StringToPojoStreamListener>();
        var provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        var jsonPayload = "{\"name\":\"oleg\"}";
        var outputMessage = DoSendReceive(provider, jsonPayload, new KeyValuePair<string, object>(MessageHeaders.ContentType, MimeTypeUtils.TextPlain));

        Assert.Equal(MimeTypeUtils.ApplicationJson, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task ByteArrayToPojoInboundContentTypeBinding()
    {
        var searchDirectories = GetSearchDirectories("TestBinder");
        _container = CreateStreamsContainerWithDefaultBindings(
            searchDirectories,
            "spring.cloud.stream.bindings.input.contentType=text/plain");
        _container.AddStreamListeners<ByteArrayToPojoStreamListener>();
        var provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        var jsonPayload = "{\"name\":\"oleg\"}";
        var outputMessage = DoSendReceive(provider, jsonPayload);

        Assert.Equal(MimeTypeUtils.ApplicationJson, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task ByteArrayToPojoInboundContentTypeHeader()
    {
        _container.AddStreamListeners<StringToPojoStreamListener>();
        var provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        var jsonPayload = "{\"name\":\"oleg\"}";
        var message = Message.Create(Encoding.UTF8.GetBytes(jsonPayload), new MessageHeaders(new Dictionary<string, object> { { MessageHeaders.ContentType, MimeTypeUtils.TextPlain } }));
        var outputMessage = DoSendReceive(provider, message);

        Assert.Equal(MimeTypeUtils.ApplicationJson, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task ByteArrayToByteArray()
    {
        _container.AddStreamListeners<ByteArrayToByteArrayStreamListener>();
        var provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        var jsonPayload = "{\"name\":\"oleg\"}";
        var outputMessage = DoSendReceive(provider, jsonPayload);

        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task ByteArrayToByteArrayInboundOutboundContentTypeBinding()
    {
        var searchDirectories = GetSearchDirectories("TestBinder");
        _container = CreateStreamsContainerWithDefaultBindings(
            searchDirectories,
            "spring.cloud.stream.bindings.input.contentType=text/plain",
            "spring.cloud.stream.bindings.output.contentType=text/plain");
        _container.AddStreamListeners<ByteArrayToByteArrayStreamListener>();
        var provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        var jsonPayload = "{\"name\":\"oleg\"}";
        var outputMessage = DoSendReceive(provider, jsonPayload);

        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task PojoMessageToStringMessage()
    {
        _container.AddStreamListeners<PojoMessageToStringMessageStreamListener>();
        var provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        var jsonPayload = "{\"name\":\"oleg\"}";
        var outputMessage = DoSendReceive(provider, jsonPayload);

        Assert.Equal(MimeTypeUtils.TextPlain, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal("oleg", Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task PojoMessageToStringMessageServiceActivator()
    {
        _container.AddServiceActivators<PojoMessageToStringMessageServiceActivator>();

        var provider = _container.BuildServiceProvider();

        var activatorProcessor = provider.GetRequiredService<ServiceActivatorAttributeProcessor>();
        activatorProcessor.Initialize();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

        var jsonPayload = "{\"name\":\"oleg\"}";
        var outputMessage = DoSendReceive(provider, jsonPayload);

        Assert.Equal(MimeTypeUtils.TextPlain, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal("oleg", Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task ByteArrayMessageToStringJsonMessageServiceActivator()
    {
        _container.AddServiceActivators<ByteArrayMessageToStringJsonMessageServiceActivator>();
        var provider = _container.BuildServiceProvider();

        var activatorProcessor = provider.GetRequiredService<ServiceActivatorAttributeProcessor>();
        activatorProcessor.Initialize();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

        var jsonPayload = "{\"name\":\"oleg\"}";
        var outputMessage = DoSendReceive(provider, jsonPayload);

        Assert.Equal(MimeTypeUtils.ApplicationJson, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal("{\"name\":\"bob\"}", Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task ByteArrayMessageToStringMessageServiceActivator()
    {
        _container.AddServiceActivators<StringMessageToStringMessageServiceActivator>();
        var provider = _container.BuildServiceProvider();

        var activatorProcessor = provider.GetRequiredService<ServiceActivatorAttributeProcessor>();
        activatorProcessor.Initialize();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

        var jsonPayload = "{\"name\":\"oleg\"}";
        var outputMessage = DoSendReceive(provider, jsonPayload);

        Assert.Equal(MimeTypeUtils.TextPlain, outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal("oleg", Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task OverrideMessageConverter_DefaultContentTypeBinding()
    {
        var searchDirectories = GetSearchDirectories("TestBinder");
        _container = CreateStreamsContainerWithDefaultBindings(
            searchDirectories,
            "spring.cloud.stream.default.contentType=application/x-java-object");
        _container.AddStreamListeners<StringToStringStreamListener>();
        _container.AddSingleton<IMessageConverter, AlwaysStringMessageConverter>();
        var provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        var jsonPayload = "{\"name\":\"oleg\"}";
        var outputMessage = DoSendReceive(provider, jsonPayload);

        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal("AlwaysStringMessageConverter", Encoding.UTF8.GetString(payload));
        Assert.Equal(MimeType.ToMimeType("application/x-java-object"), outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
    }

    [Fact]
    public async Task CustomMessageConverter_DefaultContentTypeBinding()
    {
        var searchDirectories = GetSearchDirectories("TestBinder");
        _container = CreateStreamsContainerWithDefaultBindings(
            searchDirectories,
            "spring.cloud.stream.default.contentType=foo/bar");
        _container.AddStreamListeners<StringToStringStreamListener>();
        _container.AddSingleton<IMessageConverter, FooBarMessageConverter>();
        var provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        var jsonPayload = "{\"name\":\"oleg\"}";
        var outputMessage = DoSendReceive(provider, jsonPayload);

        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal("FooBarMessageConverter", Encoding.UTF8.GetString(payload));
        Assert.Equal(MimeType.ToMimeType("foo/bar"), outputMessage.Headers.Get<MimeType>(MessageHeaders.ContentType));
    }

    [Fact]
    public async Task JsonToPojoWrongDefaultContentTypeProperty()
    {
        var searchDirectories = GetSearchDirectories("TestBinder");
        _container = CreateStreamsContainerWithDefaultBindings(
            searchDirectories,
            "spring.cloud.stream.default.contentType=text/plain");
        _container.AddStreamListeners<PojoToPojoStreamListener>();
        var provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        var jsonPayload = "{\"name\":\"oleg\"}";
        DoSendReceive(provider, jsonPayload, typeof(MessagingException));
    }

    [Fact]
    public async Task ToStringDefaultContentTypePropertyUnknownContentType()
    {
        var searchDirectories = GetSearchDirectories("TestBinder");
        _container = CreateStreamsContainerWithDefaultBindings(
            searchDirectories,
            "spring.cloud.stream.default.contentType=foo/bar");
        _container.AddStreamListeners<StringToStringStreamListener>();
        var provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        var jsonPayload = "{\"name\":\"oleg\"}";
        DoSendReceive(provider, jsonPayload, typeof(MessageConversionException));
    }

    [Fact]
    public async Task ToCollectionWithParameterizedType()
    {
        _container.AddStreamListeners<CollectionWithParameterizedTypesStreamListener>();
        var provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        var jsonPayload = "[{\"person\":{\"name\":\"jon\"},\"id\":123},{\"person\":{\"name\":\"jane\"},\"id\":456}]";
        var outputMessage = DoSendReceive(provider, jsonPayload);

        var payload = outputMessage.Payload as byte[];
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
        var provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        var jsonPayload = "{\"name\":\"oleg\"}";
        var outputMessage = DoSendReceive(provider, jsonPayload);

        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task TestWithMapPayloadParameter()
    {
        _container.AddStreamListeners<MapPayloadConfiguration>();
        var provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        var jsonPayload = "{\"name\":\"oleg\"}";
        var outputMessage = DoSendReceive(provider, jsonPayload);

        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task TestWithListInputParameter()
    {
        _container.AddStreamListeners<ListInputConfiguration>();
        var provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        var jsonPayload = "[\"foo\",\"bar\"]";
        var outputMessage = DoSendReceive(provider, jsonPayload);

        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task TestWithMessageHeadersInputParameter()
    {
        _container.AddStreamListeners<MessageHeadersInputConfiguration>();
        var provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        var jsonPayload = "{\"name\":\"oleg\"}";
        var outputMessage = DoSendReceive(provider, jsonPayload);

        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.NotEqual(jsonPayload, Encoding.UTF8.GetString(payload));
        Assert.True(outputMessage.Headers.ContainsKey(MessageHeaders.IdName));
        Assert.True(outputMessage.Headers.ContainsKey(MessageHeaders.ContentType));
    }

    [Fact]
    public async Task TestWithTypelessInputParameterAndOctetStream()
    {
        _container.AddStreamListeners<TypelessPayloadConfiguration>();
        var provider = _container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
        var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
        streamProcessor.Initialize();

        var jsonPayload = "[\"foo\",\"bar\"]";
        var message = MessageBuilder
            .WithPayload(Encoding.UTF8.GetBytes(jsonPayload))
            .SetHeader(MessageHeaders.ContentType, MimeTypeUtils.ApplicationJson)
            .Build();
        var outputMessage = DoSendReceive(provider, (IMessage<byte[]>)message);

        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task TestWithTypelessInputParameterAndServiceActivator()
    {
        _container.AddServiceActivators<TypelessPayloadConfigurationSa>();
        var provider = _container.BuildServiceProvider();

        var streamProcessor = provider.GetRequiredService<ServiceActivatorAttributeProcessor>();
        streamProcessor.Initialize();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

        var jsonPayload = "[\"foo\",\"bar\"]";
        var message = MessageBuilder
            .WithPayload(Encoding.UTF8.GetBytes(jsonPayload))
            .Build();
        var outputMessage = DoSendReceive(provider, (IMessage<byte[]>)message);

        var payload = outputMessage.Payload as byte[];
        Assert.NotNull(payload);
        Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task TestWithTypelessMessageInputParameterAndServiceActivator()
    {
        _container.AddServiceActivators<TypelessMessageConfigurationSa>();
        var provider = _container.BuildServiceProvider();

        var streamProcessor = provider.GetRequiredService<ServiceActivatorAttributeProcessor>();
        streamProcessor.Initialize();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

        var jsonPayload = "[\"foo\",\"bar\"]";
        var message = MessageBuilder
            .WithPayload(Encoding.UTF8.GetBytes(jsonPayload))
            .Build();
        var outputMessage = DoSendReceive(provider, (IMessage<byte[]>)message);

        var payload = outputMessage.Payload as byte[];
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
        var outputMessage = target.Receive();
        Assert.NotNull(outputMessage);
        return outputMessage;
    }

    private IMessage DoSendReceive(ServiceProvider provider, IMessage<byte[]> message)
    {
        var source = provider.GetService<InputDestination>();
        var target = provider.GetService<OutputDestination>();
        source.Send(message);
        var outputMessage = target.Receive();
        Assert.NotNull(outputMessage);
        return outputMessage;
    }

    private IMessage DoSendReceive(ServiceProvider provider, string jsonPayload, params KeyValuePair<string, object>[] headers)
    {
        var source = provider.GetService<InputDestination>();
        var target = provider.GetService<OutputDestination>();
        var builder = MessageBuilder.WithPayload(Encoding.UTF8.GetBytes(jsonPayload));
        foreach (var header in headers)
        {
            builder.SetHeader(header.Key, header.Value);
        }

        source.Send((IMessage<byte[]>)builder.Build());
        var outputMessage = target.Receive();
        Assert.NotNull(outputMessage);
        return outputMessage;
    }
}
