// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Configuration.CloudFoundry.ServiceBindings;

namespace Steeltoe.Configuration.CloudFoundry.Test.ServiceBindings;

public abstract class BasePostProcessorsTest
{
    protected const string TestBindingName = "test-name";
    protected const string TestProviderName = "test-provider";

    protected Dictionary<string, string?> GetConfigurationData(string bindingProvider, string bindingName, string[] tags, string? label,
        params Tuple<string, string>[] secrets)
    {
        var dictionary = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [$"vcap:services:{bindingProvider}:0:name"] = bindingName
        };

        for (int index = 0; index < tags.Length; index++)
        {
            dictionary[$"vcap:services:{bindingProvider}:0:tags:{index}"] = tags[index];
        }

        if (!string.IsNullOrEmpty(label))
        {
            dictionary[$"vcap:services:{bindingProvider}:0:label"] = label;
        }

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

    private sealed class TestPostProcessorConfigurationProvider(PostProcessorConfigurationSource source) : PostProcessorConfigurationProvider(source);

    private sealed class TestPostProcessorConfigurationSource : PostProcessorConfigurationSource;
}
