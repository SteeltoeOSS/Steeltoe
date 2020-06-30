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
            streamProcessor.AfterSingletonsInstantiated();
            var message = Message.Create<byte[]>(Encoding.UTF8.GetBytes("foo"));
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
            streamProcessor.AfterSingletonsInstantiated();
            var message = Message.Create<byte[]>(Encoding.UTF8.GetBytes("foo"));
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
