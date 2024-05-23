// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding.Test;

public abstract class BasePostProcessorsTest
{
    protected const string TestBindingName = "test-name";

    protected Dictionary<string, string?> GetConfigurationData(string bindingName, string bindingType, params Tuple<string, string>[] secrets)
    {
        var dictionary = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        string typeKey = MakeTypeKey(bindingName);
        dictionary.Add(typeKey, bindingType);

        foreach ((string name, string? value) in secrets)
        {
            string secretKey = MakeSecretKey(bindingName, name);
            dictionary.Add(secretKey, value);
        }

        return dictionary;
    }

    private string MakeTypeKey(string bindingName)
    {
        return ConfigurationPath.Combine(KubernetesServiceBindingConfigurationProvider.FromKeyPrefix, bindingName,
            KubernetesServiceBindingConfigurationProvider.TypeKey);
    }

    private string MakeSecretKey(string bindingName, string key)
    {
        return ConfigurationPath.Combine(KubernetesServiceBindingConfigurationProvider.FromKeyPrefix, bindingName, key);
    }

    internal string GetOutputKeyPrefix(string bindingName, string bindingType)
    {
        return ConfigurationPath.Combine(KubernetesServiceBindingConfigurationProvider.ToKeyPrefix, bindingType, bindingName);
    }

    internal PostProcessorConfigurationProvider GetConfigurationProvider(IConfigurationPostProcessor postProcessor)
    {
        var source = new TestPostProcessorConfigurationSource();
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

    private sealed class TestPostProcessorConfigurationSource : PostProcessorConfigurationSource;
}
