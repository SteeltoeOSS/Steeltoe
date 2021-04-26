// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Messaging;
using Steeltoe.Stream.Binder;
using Steeltoe.Stream.Config;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Steeltoe.Stream.Binding
{
    public class BindingService : IBindingService
    {
        internal IDictionary<string, IBinding> _producerBindings = new Dictionary<string, IBinding>();
        internal IDictionary<string, List<IBinding>> _consumerBindings = new Dictionary<string, List<IBinding>>();
        private readonly IBinderFactory _binderFactory;
        private readonly ILogger<BindingService> _logger;
        private readonly BindingServiceOptions _bindingServiceOptions;
        private readonly IOptionsMonitor<BindingServiceOptions> _optionsMonitor;

        public BindingServiceOptions Options
        {
            get
            {
                if (_optionsMonitor != null)
                {
                    return _optionsMonitor.CurrentValue;
                }

                return _bindingServiceOptions;
            }
        }

        public BindingService(IOptionsMonitor<BindingServiceOptions> optionsMonitor, IBinderFactory binderFactory, ILogger<BindingService> logger = null)
        {
            _optionsMonitor = optionsMonitor;
            _binderFactory = binderFactory;
            _logger = logger;
        }

        internal BindingService(BindingServiceOptions bindingServiceOptions, IBinderFactory binderFactory, ILogger<BindingService> logger = null)
        {
            _bindingServiceOptions = bindingServiceOptions;
            _binderFactory = binderFactory;
            _logger = logger;
        }

        public ICollection<IBinding> BindConsumer<T>(T inputChannel, string name)
        {
            var bindings = new List<IBinding>();
            var binder = GetBinder<T>(name);
            IConsumerOptions consumerOptions = Options.GetConsumerOptions(name);

            ValidateOptions(consumerOptions);

            var bindingTarget = Options.GetBindingDestination(name);
            if (consumerOptions.Multiplex)
            {
                bindings.Add(DoBindConsumer(inputChannel, name, binder, consumerOptions, bindingTarget));
            }
            else
            {
                var bindingTargets = bindingTarget == null ? Array.Empty<string>() : bindingTarget.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var target in bindingTargets)
                {
                    var binding = DoBindConsumer(inputChannel, name, binder, consumerOptions, target);
                    bindings.Add(binding);
                }
            }

            _consumerBindings[name] = new List<IBinding>(bindings);
            return bindings;
        }

        public IBinding BindProducer<T>(T outputChannel, string name)
        {
            var bindingTarget = Options.GetBindingDestination(name);
            var binder = GetBinder<T>(name);
            var producerOptions = Options.GetProducerOptions(name);
            ValidateOptions(producerOptions);
            var binding = DoBindProducer(outputChannel, bindingTarget, binder, producerOptions);
            _producerBindings[name] = binding;
            return binding;
        }

        public IBinding DoBindConsumer<T>(T inputTarget, string name, IBinder binder, IConsumerOptions consumerOptions, string bindingTarget)
        {
            if (Options.BindingRetryInterval <= 0)
            {
                return binder.BindConsumer(bindingTarget, Options.GetGroup(name), inputTarget, consumerOptions);
            }
            else
            {
                return DoBindConsumerWithRetry(inputTarget, name, binder, consumerOptions, bindingTarget);
            }
        }

        public IBinding DoBindConsumerWithRetry<T>(T inputChan, string name, IBinder binder, IConsumerOptions consumerOptions, string bindingTarget)
        {
            // TODO: Java code never stops retrying the bind
            do
            {
                try
                {
                    return binder.BindConsumer(bindingTarget, Options.GetGroup(name), inputChan, consumerOptions);
                }
                catch (Exception ex)
                {
                    _logger?.LogDebug(ex, ex.Message);
                    Thread.Sleep(Options.BindingRetryInterval * 1000);
                }
            }
            while (true);
        }

        public IBinding DoBindProducer<T>(T outputChan, string bindingTarget, IBinder binder, IProducerOptions producerOptions)
        {
            if (Options.BindingRetryInterval <= 0)
            {
                return binder.BindProducer(bindingTarget, outputChan, producerOptions);
            }
            else
            {
                return DoBindProducerWithRetry(outputChan, bindingTarget, binder, producerOptions);
            }
        }

        public IBinding DoBindProducerWithRetry<T>(T outputChan, string bindingTarget, IBinder binder, IProducerOptions producerOptions)
        {
            // TODO: Java code never stops retrying the bind
            do
            {
                try
                {
                    return binder.BindProducer(bindingTarget, outputChan, producerOptions);
                }
                catch (Exception)
                {
                    // log
                    Thread.Sleep(Options.BindingRetryInterval * 1000);
                }
            }
            while (true);
        }

        public void UnbindProducers(string outputName)
        {
            if (_producerBindings.TryGetValue(outputName, out var binding))
            {
                _producerBindings.Remove(outputName);
                binding.Unbind();
            }
            else
            {
                // Log failure
            }
        }

        public void UnbindConsumers(string inputName)
        {
            if (_consumerBindings.TryGetValue(inputName, out var bindings))
            {
                _consumerBindings.Remove(inputName);
                foreach (var binding in bindings)
                {
                    binding.Unbind();
                }
            }
            else
            {
                // Log
            }
        }

        protected IBinder GetBinder<T>(string channelName)
        {
            var configName = Options.GetBinder(channelName);
            return _binderFactory.GetBinder(configName, typeof(T));
        }

        private static void ValidateOptions(IProducerOptions producerOptions)
        {
            if (producerOptions.PartitionCount <= 0)
            {
                throw new InvalidOperationException("Partition count should be greater than zero.");
            }
        }

        private static void ValidateOptions(IConsumerOptions consumerOptions)
        {
            if (consumerOptions.Concurrency <= 0)
            {
                throw new InvalidOperationException("Concurrency should be greater than zero.");
            }

            if (consumerOptions.InstanceCount <= -1)
            {
                throw new InvalidOperationException("Instance count should be greater than or equal to -1.");
            }

            if (consumerOptions.InstanceIndex <= -1)
            {
                throw new InvalidOperationException("Instance index should be greater than or equal to -1.");
            }

            if (consumerOptions.MaxAttempts <= 0)
            {
                throw new InvalidOperationException("Max attempts should be greater than zero.");
            }

            if (consumerOptions.BackOffInitialInterval <= 0)
            {
                throw new InvalidOperationException("Backoff initial interval should be greater than zero.");
            }

            if (consumerOptions.BackOffMaxInterval <= 0)
            {
                throw new InvalidOperationException("Backoff max interval should be greater than zero.");
            }

            if (consumerOptions.BackOffMultiplier <= 0)
            {
                throw new InvalidOperationException("Backoff multiplier should be greater than zero.");
            }
        }
    }
}
