// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration.CloudFoundry.ServiceBinding.Test;

public abstract class BasePostProcessorsTest
{
    protected const string TestBindingName = "test-name";
    protected const string TestProviderName = "test-provider";

    protected Dictionary<string, string?> GetConfigurationData(string bindingType, string bindingProvider, string bindingName,
        params Tuple<string, string>[] secrets)
    {
        var dictionary = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [$"vcap:services:{bindingProvider}:0:tags:0"] = bindingType,
            [$"vcap:services:{bindingProvider}:0:name"] = bindingName
        };

        foreach (Tuple<string, string> tuple in secrets)
        {
            string secretKey = MakeSecretKey(bindingProvider, tuple.Item1);
            dictionary.Add(secretKey, tuple.Item2);
        }

        return dictionary;
    }

    private static string MakeSecretKey(string bindingProvider, string key)
    {
        return ConfigurationPath.Combine(CloudFoundryServiceBindingConfigurationProvider.FromKeyPrefix, bindingProvider, "0", key);
    }

    internal string GetOutputKeyPrefix(string bindingName, string bindingType)
    {
        return ConfigurationPath.Combine(CloudFoundryServiceBindingConfigurationProvider.ToKeyPrefix, bindingType, bindingName);
    }

    internal PostProcessorConfigurationProvider GetConfigurationProvider(IConfigurationPostProcessor postProcessor)
    {
        var source = new TestPostProcessorConfigurationSource();
        source.RegisterPostProcessor(postProcessor);

        return new TestPostProcessorConfigurationProvider(source);
    }

    protected string? GetFileContentAtKey(Dictionary<string, string?> configurationData, string key)
    {
        if (configurationData.TryGetValue(key, out string? value) && value != null)
        {
            return File.ReadAllText(value);
        }

        return null;
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
