// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace Steeltoe.Stream.Config
{
    public class BindingServiceOptions // : IBindingServiceOptions
    {
        public const string PREFIX = "spring:cloud:stream";

        private const int InstanceIndex_Default = 0;
        private const int InstanceCount_Default = 1;
        private const int BindingRetryInterval_Default = 30;
        private const bool OverrideCloudConnectors_Default = false;

        internal void PostProcess()
        {
            if (InstanceIndex == int.MinValue)
            {
                InstanceIndex = InstanceIndex_Default;
            }

            if (InstanceCount == int.MinValue)
            {
                InstanceCount = InstanceCount_Default;
            }

            if (BindingRetryInterval == int.MinValue)
            {
                BindingRetryInterval = BindingRetryInterval_Default;
            }

            if (!OverrideCloudConnectors.HasValue)
            {
                OverrideCloudConnectors = OverrideCloudConnectors_Default;
            }

            if (DynamicDestinations == null)
            {
                DynamicDestinations = new List<string>();
            }

            if (Binders == null)
            {
                Binders = new Dictionary<string, BinderOptions>();
            }

            if (Bindings == null)
            {
                Bindings = new Dictionary<string, BindingOptions>();
            }

            if (Default == null)
            {
                Default = new BindingOptions();
            }

            if (Default != null)
            {
                Default.PostProcess("default", null);
            }

            foreach (var binding in Bindings)
            {
                binding.Value.PostProcess(binding.Key, Default);
            }

            foreach (var binder in Binders)
            {
                binder.Value.PostProcess();
            }
        }

        public BindingServiceOptions()
        {
            PostProcess();
        }

        internal BindingServiceOptions(IConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            config.Bind(this);
            PostProcess();
        }

        public int InstanceIndex { get; set; } = int.MinValue;

        public int InstanceCount { get; set; } = int.MinValue;

        public string DefaultBinder { get; set; }

        public int BindingRetryInterval { get; set; } = int.MinValue;

        public bool? OverrideCloudConnectors { get; set; }

        public List<string> DynamicDestinations { get; set; }

        public Dictionary<string, BinderOptions> Binders { get; set; }

        public Dictionary<string, BindingOptions> Bindings { get; set; }

        public BindingOptions Default { get; set; }

        public string GetBinder(string name)
        {
            return GetBindingOptions(name).Binder;
        }

        public IDictionary<string, object> AsDictionary()
        {
            IDictionary<string, object> options = new Dictionary<string, object>
            {
                ["instanceIndex"] = InstanceIndex,
                ["instanceCount"] = InstanceCount,
                ["defaultBinder"] = DefaultBinder,

                ["dynamicDestinations"] = DynamicDestinations
            };
            foreach (var entry in Bindings)
            {
                options.Add(entry.Key, entry.Value);
            }

            foreach (var entry in Binders)
            {
                options.Add(entry.Key, entry.Value);
            }

            return options;
        }

        public BindingOptions GetBindingOptions(string name)
        {
            MakeBindingIfNecessary(name);
            var options = Bindings[name];
            if (options.Destination == null)
            {
                options.Destination = name;
            }

            return options;
        }

        public ConsumerOptions GetConsumerOptions(string inputBindingName)
        {
            if (inputBindingName == null)
            {
                throw new ArgumentNullException(nameof(inputBindingName));
            }

            var bindingOptions = GetBindingOptions(inputBindingName);
            if (bindingOptions.Consumer is not ConsumerOptions consumerOptions)
            {
                consumerOptions = new ConsumerOptions();
                consumerOptions.PostProcess(inputBindingName, Default.Consumer);
                bindingOptions.Consumer = consumerOptions;
            }

            // propagate instance count and instance index if not already set
            if (consumerOptions.InstanceCount < 0)
            {
                consumerOptions.InstanceCount = InstanceCount;
            }

            if (consumerOptions.InstanceIndex < 0)
            {
                consumerOptions.InstanceIndex = InstanceIndex;
            }

            return consumerOptions;
        }

        public ProducerOptions GetProducerOptions(string outputBindingName)
        {
            if (outputBindingName == null)
            {
                throw new ArgumentNullException(nameof(outputBindingName));
            }

            var bindingOptions = GetBindingOptions(outputBindingName);
            if (bindingOptions.Producer is not ProducerOptions producerOptions)
            {
                producerOptions = new ProducerOptions();
                producerOptions.PostProcess(outputBindingName, Default.Producer);
                bindingOptions.Producer = producerOptions;
            }

            return producerOptions;
        }

        public string GetGroup(string bindingName)
        {
            return GetBindingOptions(bindingName).Group;
        }

        public string GetBindingDestination(string bindingName)
        {
            return GetBindingOptions(bindingName).Destination;
        }

        public void UpdateProducerOptions(string bindingName, ProducerOptions producerOptions)
        {
            if (Bindings.ContainsKey(bindingName))
            {
                Bindings[bindingName].Producer = producerOptions;
            }
        }

        internal void MakeBindingIfNecessary(string bindingName)
        {
            if (!Bindings.ContainsKey(bindingName))
            {
                BindToDefaults(bindingName);
            }
        }

        internal void BindToDefaults(string binding)
        {
            var options = Default.Clone(true);
            Bindings[binding] = options;
        }
    }
}
