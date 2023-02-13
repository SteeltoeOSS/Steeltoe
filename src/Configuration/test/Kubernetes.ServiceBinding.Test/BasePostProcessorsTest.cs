// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding.Test;

public abstract class BasePostProcessorsTest
{
    protected const string TestBindingName = "test-name";
    protected const string TestBindingName1 = "test-name-1";
    protected const string TestBindingName2 = "test-name-2";
    protected const string TestBindingName3 = "test-name-3";
    protected const string TestMissingProvider = "test-missing-provider";

    protected void GetConfigurationData(Dictionary<string, string> dictionary, string bindingName, string bindingType, params Tuple<string, string>[] secrets)
    {
        foreach (Tuple<string, string> kv in secrets)
        {
            dictionary.Add(MakeSecretKey(bindingName, kv.Item1), kv.Item2);
        }

        dictionary.Add(MakeTypeKey(bindingName), bindingType);
    }

    protected Dictionary<string, string> GetConfigurationData(string bindingName, string bindingType, params Tuple<string, string>[] secrets)
    {
        var dictionary = new Dictionary<string, string>();
        GetConfigurationData(dictionary, bindingName, bindingType, secrets);
        return dictionary;
    }

    protected void GetConfigurationData(Dictionary<string, string> dictionary, string bindingName, string bindingType, string bindingProvider,
        params Tuple<string, string>[] secrets)
    {
        foreach (Tuple<string, string> kv in secrets)
        {
            dictionary.Add(MakeSecretKey(bindingName, kv.Item1), kv.Item2);
        }

        dictionary.Add(MakeTypeKey(bindingName), bindingType);
        dictionary.Add(MakeProviderKey(bindingName), bindingProvider);
    }

    protected Dictionary<string, string> GetConfigurationData(string bindingName, string bindingType, string bindingProvider,
        params Tuple<string, string>[] secrets)
    {
        var dictionary = new Dictionary<string, string>();
        GetConfigurationData(dictionary, bindingName, bindingType, bindingProvider, secrets);
        return dictionary;
    }

    private string MakeTypeKey(string bindingName)
    {
        return ServiceBindingConfigurationProvider.KubernetesBindingsPrefix + ConfigurationPath.KeyDelimiter + bindingName + ConfigurationPath.KeyDelimiter +
            ServiceBindingConfigurationProvider.TypeKey;
    }

    private string MakeProviderKey(string bindingName)
    {
        return ServiceBindingConfigurationProvider.KubernetesBindingsPrefix + ConfigurationPath.KeyDelimiter + bindingName + ConfigurationPath.KeyDelimiter +
            ServiceBindingConfigurationProvider.ProviderKey;
    }

    private string MakeSecretKey(string bindingName, string key)
    {
        return ServiceBindingConfigurationProvider.KubernetesBindingsPrefix + ConfigurationPath.KeyDelimiter + bindingName + ConfigurationPath.KeyDelimiter +
            key;
    }

    internal PostProcessorConfigurationProvider GetConfigurationProvider(IConfigurationPostProcessor postProcessor, string bindingTypeKey, bool isEnabled)
    {
        var source = new TestPostProcessorConfigurationSource
        {
            ParentConfiguration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                { $"steeltoe:kubernetes:bindings:{bindingTypeKey}:enable", isEnabled.ToString(CultureInfo.InvariantCulture) }
            }).Build()
        };

        source.RegisterPostProcessor(postProcessor);

        return new TestPostProcessorConfigurationProvider(source);
    }

    private sealed class TestPostProcessorConfigurationProvider : PostProcessorConfigurationProvider
    {
        public TestPostProcessorConfigurationProvider(PostProcessorConfigurationSource source)
            : base(source)
        {
        }
    }

    private sealed class TestPostProcessorConfigurationSource : PostProcessorConfigurationSource
    {
    }
}
