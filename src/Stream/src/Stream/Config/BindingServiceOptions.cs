// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Stream.Config;

public class BindingServiceOptions // : IBindingServiceOptions
{
    private const int InstanceIndexDefault = 0;
    private const int InstanceCountDefault = 1;
    private const int BindingRetryIntervalDefault = 30;
    private const bool OverrideCloudConnectorsDefault = false;
    public const string Prefix = "spring:cloud:stream";

    public int InstanceIndex { get; set; } = int.MinValue;

    public int InstanceCount { get; set; } = int.MinValue;

    public string DefaultBinder { get; set; }

    public int BindingRetryInterval { get; set; } = int.MinValue;

    public bool? OverrideCloudConnectors { get; set; }

    public List<string> DynamicDestinations { get; set; }

    public Dictionary<string, BinderOptions> Binders { get; set; }

    public Dictionary<string, BindingOptions> Bindings { get; set; }

    public BindingOptions Default { get; set; }

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

    internal void PostProcess()
    {
        if (InstanceIndex == int.MinValue)
        {
            InstanceIndex = InstanceIndexDefault;
        }

        if (InstanceCount == int.MinValue)
        {
            InstanceCount = InstanceCountDefault;
        }

        if (BindingRetryInterval == int.MinValue)
        {
            BindingRetryInterval = BindingRetryIntervalDefault;
        }

        OverrideCloudConnectors ??= OverrideCloudConnectorsDefault;
        DynamicDestinations ??= new List<string>();
        Binders ??= new Dictionary<string, BinderOptions>();
        Bindings ??= new Dictionary<string, BindingOptions>();
        Default ??= new BindingOptions();

        if (Default != null)
        {
            Default.PostProcess("default", null);
        }

        foreach (KeyValuePair<string, BindingOptions> binding in Bindings)
        {
            binding.Value.PostProcess(binding.Key, Default);
        }

        foreach (KeyValuePair<string, BinderOptions> binder in Binders)
        {
            binder.Value.PostProcess();
        }
    }

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

        foreach (KeyValuePair<string, BindingOptions> entry in Bindings)
        {
            options.Add(entry.Key, entry.Value);
        }

        foreach (KeyValuePair<string, BinderOptions> entry in Binders)
        {
            options.Add(entry.Key, entry.Value);
        }

        return options;
    }

    public BindingOptions GetBindingOptions(string name)
    {
        MakeBindingIfNecessary(name);
        BindingOptions options = Bindings[name];
        options.Destination ??= name;

        return options;
    }

    public ConsumerOptions GetConsumerOptions(string inputBindingName)
    {
        if (inputBindingName == null)
        {
            throw new ArgumentNullException(nameof(inputBindingName));
        }

        BindingOptions bindingOptions = GetBindingOptions(inputBindingName);
        ConsumerOptions consumerOptions = bindingOptions.Consumer;

        if (consumerOptions == null)
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

        BindingOptions bindingOptions = GetBindingOptions(outputBindingName);
        ProducerOptions producerOptions = bindingOptions.Producer;

        if (producerOptions == null)
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
        BindingOptions options = Default.Clone(true);
        Bindings[binding] = options;
    }
}
