// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Messaging.Support;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static Steeltoe.Integration.Channel.Test.DirectChannelTest;

namespace Steeltoe.Integration.Channel.Test
{
    public class DirectChannelWriterTest
    {
        [Fact]
        public async Task TestWriteAsync()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            var provider = services.BuildServiceProvider();
            var target = new ThreadNameExtractingTestTarget();
            var channel = new DirectChannel(provider);
            channel.Subscribe(target);
            var message = new GenericMessage("test");
            var currentId = Task.CurrentId;
            var curThreadId = Thread.CurrentThread.ManagedThreadId;
            await channel.Writer.WriteAsync(message);
            Assert.Equal(currentId, target.TaskId);
            Assert.Equal(curThreadId, target.ThreadId);
        }

        [Fact]
        public void TestTryWrite()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            var provider = services.BuildServiceProvider();
            var target = new ThreadNameExtractingTestTarget();
            var channel = new DirectChannel(provider);
            channel.Subscribe(target);
            var message = new GenericMessage("test");
            var currentId = Task.CurrentId;
            var curThreadId = Thread.CurrentThread.ManagedThreadId;
            Assert.True(channel.Writer.TryWrite(message));
            Assert.Equal(currentId, target.TaskId);
            Assert.Equal(curThreadId, target.ThreadId);
        }

        [Fact]
        public async Task TestWaitToWriteAsync()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            var provider = services.BuildServiceProvider();
            var target = new ThreadNameExtractingTestTarget();
            var channel = new DirectChannel(provider);
            channel.Subscribe(target);
            Assert.True(await channel.Writer.WaitToWriteAsync());
            channel.Unsubscribe(target);
            Assert.False(await channel.Writer.WaitToWriteAsync());
        }

        [Fact]
        public void TestTryComplete()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            var provider = services.BuildServiceProvider();
            var channel = new DirectChannel(provider);
            Assert.False(channel.Writer.TryComplete());
        }
    }
}
