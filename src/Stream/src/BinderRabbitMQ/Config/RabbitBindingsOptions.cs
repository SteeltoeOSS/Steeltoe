// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Stream.Binder.Rabbit.Config;

public class RabbitBindingsOptions
{
    public const string Prefix = "spring:cloud:stream:rabbit";

    public Dictionary<string, RabbitBindingOptions> Bindings { get; set; }

    public RabbitBindingOptions Default { get; set; }

    // spring.cloud.stream.rabbit.bindings.<channelName>.consumer
    // spring.cloud.stream.rabbit.bindings.<channelName>.producer
    // spring.cloud.stream.rabbit.default.consumer   NOTE: Different from Spring
    // spring.cloud.stream.rabbit.default.producer NOTE: Different from Spring
    public RabbitBindingsOptions()
    {
        PostProcess();
    }

    public RabbitBindingsOptions(IConfiguration config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        config.Bind(this);
        PostProcess();
    }

    public RabbitConsumerOptions GetRabbitConsumerOptions(string binding)
    {
        RabbitConsumerOptions results = Default.Consumer;

        if (binding == null)
        {
            return results;
        }

        Bindings.TryGetValue(binding, out RabbitBindingOptions options);

        if (options != null && options.Consumer != null)
        {
            results = options.Consumer;
        }

        return results;
    }

    public RabbitProducerOptions GetRabbitProducerOptions(string binding)
    {
        RabbitProducerOptions results = Default.Producer;
        Bindings.TryGetValue(binding, out RabbitBindingOptions options);

        if (options != null && options.Producer != null)
        {
            results = options.Producer;
        }

        return results;
    }

    internal void PostProcess()
    {
        Default ??= new RabbitBindingOptions();
        Default.Consumer ??= new RabbitConsumerOptions();
        Default.Consumer.PostProcess();
        Default.Producer ??= new RabbitProducerOptions();
        Default.Producer.PostProcess();
        Bindings ??= new Dictionary<string, RabbitBindingOptions>();

        foreach (KeyValuePair<string, RabbitBindingOptions> binding in Bindings)
        {
            RabbitBindingOptions bo = binding.Value;

            if (bo.Consumer != null)
            {
                bo.Consumer.PostProcess(Default.Consumer);
            }

            if (bo.Producer != null)
            {
                bo.Producer.PostProcess(Default.Producer);
            }
        }
    }
}
