// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Steeltoe.Configuration;

namespace Steeltoe.Connector;

internal abstract class ConnectionStringPostProcessor : IConfigurationPostProcessor
{
    public const string DefaultBindingName = "Default";

    private static readonly string ClientBindingsConfigurationKey = ConfigurationPath.Combine("Steeltoe", "Client");
    public static readonly string ServiceBindingsConfigurationKey = ConfigurationPath.Combine("steeltoe", "service-bindings");

    protected abstract string BindingType { get; }

    protected virtual IConnectionStringBuilder CreateConnectionStringBuilder()
    {
        return new DbConnectionStringBuilderWrapper(new DbConnectionStringBuilder());
    }

    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configurationData)
    {
        IDictionary<string, BindingInfo> bindingsByName = GetBindingsByName(provider.Source.ParentConfiguration);

        if (ShouldSetDefault(bindingsByName))
        {
            bindingsByName.TryGetValue(DefaultBindingName, out BindingInfo defaultBinding);

            string alternateBindingName = bindingsByName.Keys.SingleOrDefault(bindingName => bindingName != DefaultBindingName);
            BindingInfo alternateBinding = alternateBindingName == null ? null : bindingsByName[alternateBindingName];

            var bindingInfo = new BindingInfo
            {
                ServerBindingSection = alternateBinding?.ServerBindingSection,
                ClientBindingSection = defaultBinding?.ClientBindingSection
            };

            SetConnectionString(configurationData, DefaultBindingName, bindingInfo);

            if (alternateBindingName != null)
            {
                SetConnectionString(configurationData, alternateBindingName, bindingInfo);
            }
        }
        else
        {
            foreach ((string bindingName, BindingInfo bindingInfo) in bindingsByName.Where(binding => binding.Key != DefaultBindingName))
            {
                SetConnectionString(configurationData, bindingName, bindingInfo);
            }
        }
    }

    private static bool ShouldSetDefault(IDictionary<string, BindingInfo> bindingsByName)
    {
        if (bindingsByName.Count == 1)
        {
            (string bindingName, BindingInfo binding) = bindingsByName.Single();

            if (bindingName == DefaultBindingName && binding.IsClientOnly)
            {
                return true;
            }

            return binding.IsServerOnly;
        }

        if (bindingsByName.Count == 2 && bindingsByName.TryGetValue(DefaultBindingName, out BindingInfo defaultBinding) && defaultBinding.IsClientOnly)
        {
            BindingInfo alternateBinding = bindingsByName.Single(binding => binding.Key != DefaultBindingName).Value;

            if (alternateBinding.IsServerOnly)
            {
                return true;
            }
        }

        return false;
    }

    private IDictionary<string, BindingInfo> GetBindingsByName(IConfiguration configuration)
    {
        Dictionary<string, BindingInfo> bindingsByName = new();

        foreach (IConfigurationSection clientBinding in GetBindingSections(configuration, ClientBindingsConfigurationKey))
        {
            bindingsByName.TryAdd(clientBinding.Key, new BindingInfo());
            bindingsByName[clientBinding.Key].ClientBindingSection = clientBinding;
        }

        foreach (IConfigurationSection serverBinding in GetBindingSections(configuration, ServiceBindingsConfigurationKey))
        {
            bindingsByName.TryAdd(serverBinding.Key, new BindingInfo());
            bindingsByName[serverBinding.Key].ServerBindingSection = serverBinding;
        }

        return bindingsByName;
    }

    private IEnumerable<IConfigurationSection> GetBindingSections(IConfiguration configuration, string keyPrefix)
    {
        IConfigurationSection bindingsSection = configuration.GetSection(keyPrefix);

        foreach (IConfigurationSection bindingTypeSection in bindingsSection.GetChildren()
            .Where(section => string.Equals(section.Key, BindingType, StringComparison.OrdinalIgnoreCase)))
        {
            foreach (IConfigurationSection bindingSection in bindingTypeSection.GetChildren())
            {
                yield return bindingSection;
            }
        }
    }

    private void SetConnectionString(IDictionary<string, string> configurationData, string bindingName, BindingInfo bindingInfo)
    {
        IConnectionStringBuilder connectionStringBuilder = CreateConnectionStringBuilder();

        if (bindingInfo.ClientBindingSection != null)
        {
            string existingConnectionString = bindingInfo.ClientBindingSection.Value;

            if (!string.IsNullOrEmpty(existingConnectionString))
            {
                connectionStringBuilder.ConnectionString = existingConnectionString;
            }
        }

        if (bindingInfo.ServerBindingSection != null)
        {
            foreach (IConfigurationSection secretSection in bindingInfo.ServerBindingSection.GetChildren())
            {
                string secretName = secretSection.Key;
                string secretValue = secretSection.Value;

                connectionStringBuilder[secretName] = secretValue;
            }
        }

        string connectionStringKey = ConfigurationPath.Combine(ServiceBindingsConfigurationKey, BindingType, bindingName, "ConnectionString");
        configurationData[connectionStringKey] = connectionStringBuilder.ConnectionString;
    }

    private sealed class BindingInfo
    {
        public IConfigurationSection ServerBindingSection { get; set; }
        public IConfigurationSection ClientBindingSection { get; set; }

        public bool IsServerOnly => ServerBindingSection != null && ClientBindingSection == null;
        public bool IsClientOnly => ServerBindingSection == null && ClientBindingSection != null;
    }
}
