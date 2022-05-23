// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using Steeltoe.Stream.Binding;
using Steeltoe.Stream.Extensions;
using Steeltoe.Stream.TestBinder;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Stream.Tck
{
    public class ErrorHandlingTest : AbstractTest
    {
        private readonly IServiceCollection container;

        public ErrorHandlingTest()
        {
            var searchDirectories = GetSearchDirectories("TestBinder");
            container = CreateStreamsContainerWithDefaultBindings(searchDirectories);
        }

        [Fact]
        public async Task TestGlobalErrorWithMessage()
        {
            container.AddStreamListeners<GlobalErrorHandlerWithErrorMessageConfig>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.Initialize();
            var message = Message.Create(Encoding.UTF8.GetBytes("foo"));
            DoSend(provider, message);

            var config = provider.GetService<GlobalErrorHandlerWithErrorMessageConfig>();
            Assert.True(config.GlobalErroInvoked);
        }

        [Fact]
        public async Task TestGlobalErrorWithThrowable()
        {
            container.AddStreamListeners<GlobalErrorHandlerWithExceptionConfig>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.Initialize();
            var message = Message.Create(Encoding.UTF8.GetBytes("foo"));
            DoSend(provider, message);

            var config = provider.GetService<GlobalErrorHandlerWithExceptionConfig>();
            Assert.True(config.GlobalErroInvoked);
        }

        private void DoSend(ServiceProvider provider, IMessage<byte[]> message)
        {
            var source = provider.GetService<InputDestination>();
            source.Send(message);
        }
    }
}
