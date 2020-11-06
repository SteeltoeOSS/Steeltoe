// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Channel;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Stream.Binding;
using Steeltoe.Stream.Config;
using Steeltoe.Stream.Converter;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Stream.Binder
{
    public abstract class AbstractBinderTests<B, T>
        where B : AbstractTestBinder<T>
        where T : AbstractBinder<IMessageChannel>
    {
        protected virtual ISmartMessageConverter MessageConverter { get; set; }

        protected virtual double TimeoutMultiplier { get; set; } = 1.0D;

        protected virtual ITestOutputHelper Output { get; set; }

        protected virtual ServiceCollection Services { get; set; }

        protected virtual ConfigurationBuilder ConfigBuilder { get; set; }

        public AbstractBinderTests(ITestOutputHelper output)
        {
            MessageConverter = new CompositeMessageConverterFactory().MessageConverterForAllRegistered;
            Output = output;
            Services = new ServiceCollection();
            ConfigBuilder = new ConfigurationBuilder();
        }

        public void Cleanup()
        {
            //if(this .TestClean)
        }

        protected BindingOptions CreateConsumerBindingOptions(ConsumerOptions consumerOptions)
        {
            var bindingOptions = new BindingOptions();
            bindingOptions.Consumer = consumerOptions;
            return bindingOptions;
        }

        protected BindingOptions CreateProducerBindingOptions(ProducerOptions producerOptions)
        {
            var bindingOptions = new BindingOptions();
            bindingOptions.Producer = producerOptions;
            return bindingOptions;
        }

        [Fact]
        public void TestClean()
        {
            var binder = GetBinder();
            var foo0ProducerBinding = binder.BindProducer(
                string.Format("foo{0}", GetDestinationNameDelimiter()),
                CreateBindableChannel("output", new BindingOptions()),
                CreateProducerOptions());
        }

        protected IMessage Receive(IPollableChannel channel)
        {
            return Receive(channel, 1);
        }

        protected IMessage Receive(IPollableChannel channel, int additionalMultiplier)
        {
            long startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var receive = channel.Receive((int)(1000 * TimeoutMultiplier * additionalMultiplier));
            long elapsed = DateTimeOffset.Now.ToUnixTimeMilliseconds() - startTime;
            Output.WriteLine("receive() took " + (elapsed / 1000) + " seconds");
            return receive;
        }

        protected DirectChannel CreateBindableChannel(string channelName, BindingOptions bindingProperties)
        {
            // The 'channelName.contains("input")' is strictly for convenience to avoid
            // modifications in multiple tests
            return CreateBindableChannel(channelName, bindingProperties, channelName.Contains("input"));
        }

        protected DirectChannel CreateBindableChannel(string channelName, BindingOptions bindingProperties, bool inputChannel)
        {
            
            var messageConverterConfigurer = CreateConverterConfigurer(channelName, bindingProperties);
            var channel = new DirectChannel();
            channel.ServiceName = channelName;
            if (inputChannel)
            {
                messageConverterConfigurer.ConfigureInputChannel(channel, channelName);
            }
            else
            {
                messageConverterConfigurer.ConfigureOutputChannel(channel, channelName);
            }

            return channel;
        }

        protected string GetDestinationNameDelimiter()
        {
            return ".";
        }

        protected abstract B GetBinder();

        protected abstract ConsumerOptions CreateConsumerOptions();

        protected abstract ProducerOptions CreateProducerOptions();

        private MessageConverterConfigurer CreateConverterConfigurer(string channelName, BindingOptions bindingProperties)
        {
             var bindingServiceProperties = new BindingServiceOptions();
             bindingServiceProperties.Bindings.Add(channelName, bindingProperties);
            // var serviceCollection = new ServiceCollection();

            //var configuration = new ConfigurationBuilder().Build();
            //var applicationContext = new GenericApplicationContext(serviceCollection.BuildServiceProvider(), configuration);
            //applicationContext.refresh();
            //  bindingServiceProperties.con
            //     bindingServiceProperties.setConversionService(new DefaultConversionService());
            // bindingServiceProperties.afterPropertiesSet();
            var applicationContext = GetBinder().ApplicationContext;

            var extractors = applicationContext.GetServices<IPartitionKeyExtractorStrategy>();
            var selectors = applicationContext.GetServices<IPartitionSelectorStrategy>();
            var bindingServiceOptionsMonitor = new BindingServiceOptionsMonitor(bindingServiceProperties);
            
            MessageConverterConfigurer messageConverterConfigurer = new MessageConverterConfigurer(applicationContext, bindingServiceOptionsMonitor, new CompositeMessageConverterFactory(), extractors, selectors);
            // messageConverterConfigurer.setBeanFactory(applicationContext.getBeanFactory());
            return messageConverterConfigurer;
            
        }

        private class BindingServiceOptionsMonitor : IOptionsMonitor<BindingServiceOptions>
        {
            public BindingServiceOptionsMonitor(BindingServiceOptions options)
            {
                CurrentValue = options;
            }

            public BindingServiceOptions CurrentValue { get; set; }

            public BindingServiceOptions Get(string name)
            {
                return CurrentValue;
            }

            public IDisposable OnChange(Action<BindingServiceOptions, string> listener)
            {
                return null;
            }
        }
    }
}
