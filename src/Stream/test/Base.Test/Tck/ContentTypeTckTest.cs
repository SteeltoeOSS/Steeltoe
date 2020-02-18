﻿// Copyright 2017 the original author or authors.
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

using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Common.Util;
using Steeltoe.Integration.Channel;
using Steeltoe.Integration.Config;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Support;
using Steeltoe.Stream.Binder;
using Steeltoe.Stream.Binding;
using Steeltoe.Stream.Extensions;
using Steeltoe.Stream.TestBinder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Stream.Tck
{
    public class ContentTypeTckTest : AbstractTest
    {
        private IServiceCollection container;

        public ContentTypeTckTest()
        {
            var searchDirectories = GetSearchDirectories("TestBinder");
            container = CreateStreamsContainerWithDefaultBindings(searchDirectories);
        }

        [Fact]
        public async Task StringToMapStreamListener()
        {
            container.AddStreamListeners<StringToMapStreamListener>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();
            var jsonPayload = "{\"name\":\"oleg\"}";
            var outputMessage = DoSendReceive(provider, jsonPayload);
            var payload = outputMessage.Payload as byte[];
            Assert.NotNull(payload);
            Assert.Equal("oleg", Encoding.UTF8.GetString(payload));
        }

        [Fact]
        public async Task StringToMapMessageStreamListener()
        {
            container.AddStreamListeners<StringToMapMessageStreamListener>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

            var jsonPayload = "{\"name\":\"oleg\"}";
            var outputMessage = DoSendReceive(provider, jsonPayload);
            var payload = outputMessage.Payload as byte[];
            Assert.NotNull(payload);
            Assert.Equal("oleg", Encoding.UTF8.GetString(payload));
        }

        [Fact]
        public async Task StringToMapMessageStreamListenerOriginalContentType()
        {
            container.AddStreamListeners<StringToMapMessageStreamListener>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

            var source = provider.GetService<InputDestination>();
            var target = provider.GetService<OutputDestination>();
            var jsonPayload = "{\"name\":\"oleg\"}";
            var message = Steeltoe.Messaging.Support.MessageBuilder<byte[]>.WithPayload(Encoding.UTF8.GetBytes(jsonPayload))
                                        .SetHeader(MessageHeaders.CONTENT_TYPE, "text/plain")
                                        .SetHeader("originalContentType", "application/json;charset=UTF-8")
                                        .Build();
            source.Send(message);
            var outputMessage = target.Receive();
            Assert.NotNull(outputMessage);
            var payload = outputMessage.Payload as byte[];
            Assert.NotNull(payload);
            Assert.Equal("oleg", Encoding.UTF8.GetString(payload));
        }

        [Fact]
        public async Task WithInternalPipeline()
        {
            container.AddStreamListeners<InternalPipeLine>();
            container.AddSingleton<IMessageChannel>((p) => new DirectChannel(p, "internalchannel"));
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

            var jsonPayload = "{\"name\":\"oleg\"}";
            var outputMessage = DoSendReceive(provider, jsonPayload);
            var payload = outputMessage.Payload as byte[];
            Assert.NotNull(payload);
            Assert.Equal("OLEG", Encoding.UTF8.GetString(payload));
        }

        [Fact]
        public async Task PojoToPojo()
        {
            container.AddStreamListeners<PojoToPojoStreamListener>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

            var jsonPayload = "{\"name\":\"oleg\"}";
            var outputMessage = DoSendReceive(provider, jsonPayload);
            Assert.Equal(MimeTypeUtils.APPLICATION_JSON, outputMessage.Headers.Get<MimeType>(MessageHeaders.CONTENT_TYPE));
            var payload = outputMessage.Payload as byte[];
            Assert.NotNull(payload);
            Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
        }

        [Fact]
        public async Task PojoToString()
        {
            container.AddStreamListeners<PojoToStringStreamListener>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

            var jsonPayload = "{\"name\":\"oleg\"}";
            var outputMessage = DoSendReceive(provider, jsonPayload);
            Assert.Equal(MimeTypeUtils.APPLICATION_JSON, outputMessage.Headers.Get<MimeType>(MessageHeaders.CONTENT_TYPE));
            var payload = outputMessage.Payload as byte[];
            Assert.NotNull(payload);
            Assert.Equal("oleg", Encoding.UTF8.GetString(payload));
        }

        [Fact]
        public async Task PojoToStringOutboundContentTypeBinding()
        {
            var searchDirectories = GetSearchDirectories("TestBinder");
            container = CreateStreamsContainerWithDefaultBindings(
                searchDirectories,
                "spring.cloud.stream.bindings.output.contentType=text/plain");
            container.AddStreamListeners<PojoToStringStreamListener>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

            var jsonPayload = "{\"name\":\"oleg\"}";
            var outputMessage = DoSendReceive(provider, jsonPayload);

            Assert.Equal(MimeTypeUtils.TEXT_PLAIN, outputMessage.Headers.Get<MimeType>(MessageHeaders.CONTENT_TYPE));
            var payload = outputMessage.Payload as byte[];
            Assert.NotNull(payload);
            Assert.Equal("oleg", Encoding.UTF8.GetString(payload));
        }

        [Fact]
        public async Task PojoToByteArray()
        {
            container.AddStreamListeners<PojoToByteArrayStreamListener>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

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
            container = CreateStreamsContainerWithDefaultBindings(
                searchDirectories,
                "spring.cloud.stream.bindings.output.contentType=text/plain");
            container.AddStreamListeners<PojoToByteArrayStreamListener>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

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
            container = CreateStreamsContainerWithDefaultBindings(
                searchDirectories,
                "spring.cloud.stream.bindings.input.contentType=text/plain");
            container.AddStreamListeners<StringToPojoStreamListener>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

            var jsonPayload = "{\"name\":\"oleg\"}";
            var outputMessage = DoSendReceive(provider, jsonPayload);

            Assert.Equal(MimeTypeUtils.APPLICATION_JSON, outputMessage.Headers.Get<MimeType>(MessageHeaders.CONTENT_TYPE));
            var payload = outputMessage.Payload as byte[];
            Assert.NotNull(payload);
            Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
        }

        [Fact]
        public async Task TypelessToPojoInboundContentTypeBinding()
        {
            var searchDirectories = GetSearchDirectories("TestBinder");
            container = CreateStreamsContainerWithDefaultBindings(
                searchDirectories,
                "spring.cloud.stream.bindings.input.contentType=text/plain");
            container.AddStreamListeners<TypelessToPojoStreamListener>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

            var jsonPayload = "{\"name\":\"oleg\"}";
            var outputMessage = DoSendReceive(provider, jsonPayload);

            Assert.Equal(MimeTypeUtils.APPLICATION_JSON, outputMessage.Headers.Get<MimeType>(MessageHeaders.CONTENT_TYPE));
            var payload = outputMessage.Payload as byte[];
            Assert.NotNull(payload);
            Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
        }

        [Fact]
        public async Task TypelessToPojoInboundContentTypeBindingJson()
        {
            var searchDirectories = GetSearchDirectories("TestBinder");
            container = CreateStreamsContainerWithDefaultBindings(
                searchDirectories,
                "spring.cloud.stream.bindings.input.contentType=application/json");
            container.AddStreamListeners<TypelessToPojoStreamListener>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

            var jsonPayload = "{\"name\":\"oleg\"}";
            var outputMessage = DoSendReceive(provider, jsonPayload);

            Assert.Equal(MimeTypeUtils.APPLICATION_JSON, outputMessage.Headers.Get<MimeType>(MessageHeaders.CONTENT_TYPE));
            var payload = outputMessage.Payload as byte[];
            Assert.NotNull(payload);
            Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
        }

        [Fact]
        public async Task TypelessMessageToPojoInboundContentTypeBinding()
        {
            var searchDirectories = GetSearchDirectories("TestBinder");
            container = CreateStreamsContainerWithDefaultBindings(
                searchDirectories,
                "spring.cloud.stream.bindings.input.contentType=text/plain");
            container.AddStreamListeners<TypelessMessageToPojoStreamListener>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

            var jsonPayload = "{\"name\":\"oleg\"}";
            var outputMessage = DoSendReceive(provider, jsonPayload);

            Assert.Equal(MimeTypeUtils.APPLICATION_JSON, outputMessage.Headers.Get<MimeType>(MessageHeaders.CONTENT_TYPE));
            var payload = outputMessage.Payload as byte[];
            Assert.NotNull(payload);
            Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
        }

        [Fact]
        public async Task TypelessMessageToPojoInboundContentTypeBindingJson()
        {
            var searchDirectories = GetSearchDirectories("TestBinder");
            container = CreateStreamsContainerWithDefaultBindings(
                searchDirectories,
                "spring.cloud.stream.bindings.input.contentType=application/json");
            container.AddStreamListeners<TypelessMessageToPojoStreamListener>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

            var jsonPayload = "{\"name\":\"oleg\"}";
            var outputMessage = DoSendReceive(provider, jsonPayload);

            Assert.Equal(MimeTypeUtils.APPLICATION_JSON, outputMessage.Headers.Get<MimeType>(MessageHeaders.CONTENT_TYPE));
            var payload = outputMessage.Payload as byte[];
            Assert.NotNull(payload);
            Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
        }

        [Fact]
        public async Task TypelessToPojoWithTextHeaderContentTypeBinding()
        {
            container.AddStreamListeners<TypelessMessageToPojoStreamListener>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

            var jsonPayload = "{\"name\":\"oleg\"}";
            var outputMessage = DoSendReceive(provider, jsonPayload, new KeyValuePair<string, object>(MessageHeaders.CONTENT_TYPE, MimeType.ToMimeType("text/plain")));

            Assert.Equal(MimeTypeUtils.APPLICATION_JSON, outputMessage.Headers.Get<MimeType>(MessageHeaders.CONTENT_TYPE));
            var payload = outputMessage.Payload as byte[];
            Assert.NotNull(payload);
            Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
        }

        [Fact]
        public async Task TypelessToPojoOutboundContentTypeBinding()
        {
            var searchDirectories = GetSearchDirectories("TestBinder");
            container = CreateStreamsContainerWithDefaultBindings(
                searchDirectories,
                "spring.cloud.stream.bindings.output.contentType=text/plain");
            container.AddStreamListeners<TypelessToMessageStreamListener>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

            var jsonPayload = "{\"name\":\"oleg\"}";
            var outputMessage = DoSendReceive(provider, jsonPayload, new KeyValuePair<string, object>(MessageHeaders.CONTENT_TYPE, MimeType.ToMimeType("text/plain")));

            Assert.Equal(MimeTypeUtils.TEXT_PLAIN, outputMessage.Headers.Get<MimeType>(MessageHeaders.CONTENT_TYPE));
            var payload = outputMessage.Payload as byte[];
            Assert.NotNull(payload);
            Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
        }

        [Fact]
        public async Task OutboundMessageWithTextContentTypeOnly()
        {
            container.AddStreamListeners<TypelessToMessageTextOnlyContentTypeStreamListener>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

            var jsonPayload = "{\"name\":\"oleg\"}";
            var outputMessage = DoSendReceive(provider, jsonPayload, new KeyValuePair<string, object>(MessageHeaders.CONTENT_TYPE, new MimeType("text")));

            Assert.Equal(MimeTypeUtils.TEXT_PLAIN, outputMessage.Headers.Get<MimeType>(MessageHeaders.CONTENT_TYPE));
            var payload = outputMessage.Payload as byte[];
            Assert.NotNull(payload);
            Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
        }

        [Fact]
        public async Task StringToPojoInboundContentTypeHeader()
        {
            container.AddStreamListeners<StringToPojoStreamListener>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

            var jsonPayload = "{\"name\":\"oleg\"}";
            var outputMessage = DoSendReceive(provider, jsonPayload, new KeyValuePair<string, object>(MessageHeaders.CONTENT_TYPE, MimeTypeUtils.TEXT_PLAIN));

            Assert.Equal(MimeTypeUtils.APPLICATION_JSON, outputMessage.Headers.Get<MimeType>(MessageHeaders.CONTENT_TYPE));
            var payload = outputMessage.Payload as byte[];
            Assert.NotNull(payload);
            Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
        }

        [Fact]
        public async Task ByteArrayToPojoInboundContentTypeBinding()
        {
            var searchDirectories = GetSearchDirectories("TestBinder");
            container = CreateStreamsContainerWithDefaultBindings(
                searchDirectories,
                "spring.cloud.stream.bindings.input.contentType=text/plain");
            container.AddStreamListeners<ByteArrayToPojoStreamListener>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

            var jsonPayload = "{\"name\":\"oleg\"}";
            var outputMessage = DoSendReceive(provider, jsonPayload);

            Assert.Equal(MimeTypeUtils.APPLICATION_JSON, outputMessage.Headers.Get<MimeType>(MessageHeaders.CONTENT_TYPE));
            var payload = outputMessage.Payload as byte[];
            Assert.NotNull(payload);
            Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
        }

        [Fact]
        public async Task ByteArrayToPojoInboundContentTypeHeader()
        {
            container.AddStreamListeners<StringToPojoStreamListener>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

            var jsonPayload = "{\"name\":\"oleg\"}";
            var message = new GenericMessage<byte[]>(Encoding.UTF8.GetBytes(jsonPayload), new MessageHeaders(new Dictionary<string, object>() { { MessageHeaders.CONTENT_TYPE, MimeTypeUtils.TEXT_PLAIN } }));
            var outputMessage = DoSendReceive(provider, message);

            Assert.Equal(MimeTypeUtils.APPLICATION_JSON, outputMessage.Headers.Get<MimeType>(MessageHeaders.CONTENT_TYPE));
            var payload = outputMessage.Payload as byte[];
            Assert.NotNull(payload);
            Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
        }

        [Fact]
        public async Task ByteArrayToByteArray()
        {
            container.AddStreamListeners<ByteArrayToByteArrayStreamListener>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

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
            container = CreateStreamsContainerWithDefaultBindings(
                searchDirectories,
                "spring.cloud.stream.bindings.input.contentType=text/plain",
                "spring.cloud.stream.bindings.output.contentType=text/plain");
            container.AddStreamListeners<ByteArrayToByteArrayStreamListener>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

            var jsonPayload = "{\"name\":\"oleg\"}";
            var outputMessage = DoSendReceive(provider, jsonPayload);

            var payload = outputMessage.Payload as byte[];
            Assert.NotNull(payload);
            Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
        }

        [Fact]
        public async Task PojoMessageToStringMessage()
        {
            container.AddStreamListeners<PojoMessageToStringMessageStreamListener>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

            var jsonPayload = "{\"name\":\"oleg\"}";
            var outputMessage = DoSendReceive(provider, jsonPayload);

            Assert.Equal(MimeTypeUtils.TEXT_PLAIN, outputMessage.Headers.Get<MimeType>(MessageHeaders.CONTENT_TYPE));
            var payload = outputMessage.Payload as byte[];
            Assert.NotNull(payload);
            Assert.Equal("oleg", Encoding.UTF8.GetString(payload));
        }

        [Fact(Skip = "Requires ServiceActivator impl")]
        public async Task PojoMessageToStringMessageServiceActivator()
        {
            container.AddServiceActivators<PojoMessageToStringMessageServiceActivator>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
            var activatorProcessor = provider.GetRequiredService<ServiceActivatorAttributeProcessor>();
            activatorProcessor.AfterSingletonsInstantiated();

            var jsonPayload = "{\"name\":\"oleg\"}";
            var outputMessage = DoSendReceive(provider, jsonPayload);

            Assert.Equal(MimeTypeUtils.TEXT_PLAIN, outputMessage.Headers.Get<MimeType>(MessageHeaders.CONTENT_TYPE));
            var payload = outputMessage.Payload as byte[];
            Assert.NotNull(payload);
            Assert.Equal("oleg", Encoding.UTF8.GetString(payload));
        }

        [Fact(Skip = "Requires ServiceActivator impl")]
        public async Task ByteArrayMessageToStringJsonMessageServiceActivator()
        {
            container.AddServiceActivators<ByteArrayMessageToStringJsonMessageServiceActivator>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
            var activatorProcessor = provider.GetRequiredService<ServiceActivatorAttributeProcessor>();
            activatorProcessor.AfterSingletonsInstantiated();

            var jsonPayload = "{\"name\":\"oleg\"}";
            var outputMessage = DoSendReceive(provider, jsonPayload);

            Assert.Equal(MimeTypeUtils.APPLICATION_JSON, outputMessage.Headers.Get<MimeType>(MessageHeaders.CONTENT_TYPE));
            var payload = outputMessage.Payload as byte[];
            Assert.NotNull(payload);
            Assert.Equal("{\"name\":\"bob\"}", Encoding.UTF8.GetString(payload));
        }

        [Fact(Skip = "Requires ServiceActivator impl")]
        public async Task ByteArrayMessageToStringMessageServiceActivator()
        {
            container.AddServiceActivators<StringMessageToStringMessageServiceActivator>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
            var activatorProcessor = provider.GetRequiredService<ServiceActivatorAttributeProcessor>();
            activatorProcessor.AfterSingletonsInstantiated();

            var jsonPayload = "{\"name\":\"oleg\"}";
            var outputMessage = DoSendReceive(provider, jsonPayload);

            Assert.Equal(MimeTypeUtils.TEXT_PLAIN, outputMessage.Headers.Get<MimeType>(MessageHeaders.CONTENT_TYPE));
            var payload = outputMessage.Payload as byte[];
            Assert.NotNull(payload);
            Assert.Equal("oleg", Encoding.UTF8.GetString(payload));
        }

        [Fact]
        public async Task OverrideMessageConverter_DefaultContentTypeBinding()
        {
            var searchDirectories = GetSearchDirectories("TestBinder");
            container = CreateStreamsContainerWithDefaultBindings(
                searchDirectories,
                "spring.cloud.stream.default.contentType=application/x-java-object");
            container.AddStreamListeners<StringToStringStreamListener>();
            container.AddSingleton<IMessageConverter, AlwaysStringMessageConverter>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

            var jsonPayload = "{\"name\":\"oleg\"}";
            var outputMessage = DoSendReceive(provider, jsonPayload);

            var payload = outputMessage.Payload as byte[];
            Assert.NotNull(payload);
            Assert.Equal("AlwaysStringMessageConverter", Encoding.UTF8.GetString(payload));
            Assert.Equal(MimeType.ToMimeType("application/x-java-object"), outputMessage.Headers.Get<MimeType>(MessageHeaders.CONTENT_TYPE));
        }

        [Fact]
        public async Task CustomMessageConverter_DefaultContentTypeBinding()
        {
            var searchDirectories = GetSearchDirectories("TestBinder");
            container = CreateStreamsContainerWithDefaultBindings(
                searchDirectories,
                "spring.cloud.stream.default.contentType=foo/bar");
            container.AddStreamListeners<StringToStringStreamListener>();
            container.AddSingleton<IMessageConverter, FooBarMessageConverter>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

            var jsonPayload = "{\"name\":\"oleg\"}";
            var outputMessage = DoSendReceive(provider, jsonPayload);

            var payload = outputMessage.Payload as byte[];
            Assert.NotNull(payload);
            Assert.Equal("FooBarMessageConverter", Encoding.UTF8.GetString(payload));
            Assert.Equal(MimeType.ToMimeType("foo/bar"), outputMessage.Headers.Get<MimeType>(MessageHeaders.CONTENT_TYPE));
        }

        [Fact]
        public async Task JsonToPojoWrongDefaultContentTypeProperty()
        {
            var searchDirectories = GetSearchDirectories("TestBinder");
            container = CreateStreamsContainerWithDefaultBindings(
                searchDirectories,
                "spring.cloud.stream.default.contentType=text/plain");
            container.AddStreamListeners<PojoToPojoStreamListener>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

            var jsonPayload = "{\"name\":\"oleg\"}";
            DoSendReceive(provider, jsonPayload, typeof(MessagingException));
        }

        [Fact]
        public async Task ToStringDefaultContentTypePropertyUnknownContentType()
        {
            var searchDirectories = GetSearchDirectories("TestBinder");
            container = CreateStreamsContainerWithDefaultBindings(
                searchDirectories,
                "spring.cloud.stream.default.contentType=foo/bar");
            container.AddStreamListeners<StringToStringStreamListener>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

            var jsonPayload = "{\"name\":\"oleg\"}";
            DoSendReceive(provider, jsonPayload, typeof(MessageConversionException));
        }

        [Fact]
        public async Task ToCollectionWithParameterizedType()
        {
            container.AddStreamListeners<CollectionWithParameterizedTypesStreamListener>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

            var jsonPayload = "[{\"person\":{\"name\":\"jon\"},\"id\":123},{\"person\":{\"name\":\"jane\"},\"id\":456}]";
            var outputMessage = DoSendReceive(provider, jsonPayload);

            var payload = outputMessage.Payload as byte[];
            Assert.NotNull(payload);
            var settings = new JsonSerializerSettings()
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
            container.AddStreamListeners<MapInputConfiguration>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

            var jsonPayload = "{\"name\":\"oleg\"}";
            var outputMessage = DoSendReceive(provider, jsonPayload);

            var payload = outputMessage.Payload as byte[];
            Assert.NotNull(payload);
            Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
        }

        [Fact]
        public async Task TestWithMapPayloadParameter()
        {
            container.AddStreamListeners<MapPayloadConfiguration>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

            var jsonPayload = "{\"name\":\"oleg\"}";
            var outputMessage = DoSendReceive(provider, jsonPayload);

            var payload = outputMessage.Payload as byte[];
            Assert.NotNull(payload);
            Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
        }

        [Fact]
        public async Task TestWithListInputParameter()
        {
            container.AddStreamListeners<ListInputConfiguration>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

            var jsonPayload = "[\"foo\",\"bar\"]";
            var outputMessage = DoSendReceive(provider, jsonPayload);

            var payload = outputMessage.Payload as byte[];
            Assert.NotNull(payload);
            Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
        }

        [Fact]
        public async Task TestWithMessageHeadersInputParameter()
        {
            container.AddStreamListeners<MessageHeadersInputConfiguration>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

            var jsonPayload = "{\"name\":\"oleg\"}";
            var outputMessage = DoSendReceive(provider, jsonPayload);

            var payload = outputMessage.Payload as byte[];
            Assert.NotNull(payload);
            Assert.NotEqual(jsonPayload, Encoding.UTF8.GetString(payload));
            Assert.True(outputMessage.Headers.ContainsKey(MessageHeaders.ID));
            Assert.True(outputMessage.Headers.ContainsKey(MessageHeaders.CONTENT_TYPE));
        }

        [Fact]
        public async Task TestWithTypelessInputParameterAndOctetStream()
        {
            container.AddStreamListeners<TypelessPayloadConfiguration>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

            var jsonPayload = "[\"foo\",\"bar\"]";
            var message = Steeltoe.Messaging.Support.MessageBuilder<byte[]>
                .WithPayload(Encoding.UTF8.GetBytes(jsonPayload))
                .SetHeader(MessageHeaders.CONTENT_TYPE, MimeTypeUtils.APPLICATION_JSON)
                .Build();
            var outputMessage = DoSendReceive(provider, message);

            var payload = outputMessage.Payload as byte[];
            Assert.NotNull(payload);
            Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
        }

        [Fact(Skip = "Requires ServiceActivator impl")]
        public async Task TestWithTypelessInputParameterAndServiceActivator()
        {
            container.AddServiceActivators<TypelessPayloadConfigurationSA>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

            var jsonPayload = "[\"foo\",\"bar\"]";
            var message = Steeltoe.Messaging.Support.MessageBuilder<byte[]>
                .WithPayload(Encoding.UTF8.GetBytes(jsonPayload))
                .Build();
            var outputMessage = DoSendReceive(provider, message);

            var payload = outputMessage.Payload as byte[];
            Assert.NotNull(payload);
            Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
        }

        [Fact(Skip = "Requires ServiceActivator impl")]
        public async Task TestWithTypelessMessageInputParameterAndServiceActivator()
        {
            container.AddServiceActivators<TypelessMessageConfigurationSA>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

            var jsonPayload = "[\"foo\",\"bar\"]";
            var message = Steeltoe.Messaging.Support.MessageBuilder<byte[]>
                .WithPayload(Encoding.UTF8.GetBytes(jsonPayload))
                .Build();
            var outputMessage = DoSendReceive(provider, message);

            var payload = outputMessage.Payload as byte[];
            Assert.NotNull(payload);
            Assert.Equal(jsonPayload, Encoding.UTF8.GetString(payload));
        }

        private void DoSendReceive(ServiceProvider provider, string jsonPayload, Type lastError)
        {
            var source = provider.GetService<InputDestination>();
            _ = provider.GetService<OutputDestination>();
            source.Send(new GenericMessage<byte[]>(Encoding.UTF8.GetBytes(jsonPayload)));
            var binder = provider.GetService<IBinder>() as TestChannelBinder;
            Assert.Equal(lastError, binder.LastError?.Payload?.GetType());
        }

        private IMessage DoSendReceive(ServiceProvider provider, string jsonPayload)
        {
            var source = provider.GetService<InputDestination>();
            var target = provider.GetService<OutputDestination>();
            source.Send(new GenericMessage<byte[]>(Encoding.UTF8.GetBytes(jsonPayload)));
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
            var builder = Steeltoe.Messaging.Support.MessageBuilder<byte[]>.WithPayload(Encoding.UTF8.GetBytes(jsonPayload));
            foreach (var header in headers)
            {
                builder.SetHeader(header.Key, header.Value);
            }

            source.Send(builder.Build());
            var outputMessage = target.Receive();
            Assert.NotNull(outputMessage);
            return outputMessage;
        }
    }
}
