// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Stream.Binder;
using System;
using System.Collections.Generic;

namespace Steeltoe.Stream.Config;

public class BinderConfigurations : IBinderConfigurations
{
    private IBinderTypeRegistry Registry { get; set; }

    private BindingServiceOptions Options { get; set; }

    private Dictionary<string, BinderConfiguration> _configurations;

    public BinderConfigurations(IBinderTypeRegistry binderTypeRegistry, IConfiguration config)
    {
        Registry = binderTypeRegistry;
        Options = new BindingServiceOptions(config);
    }

    public BinderConfigurations(IBinderTypeRegistry binderTypeRegistry, BindingServiceOptions options)
    {
        Registry = binderTypeRegistry;
        Options = options;
    }

    public Dictionary<string, BinderConfiguration> Configurations
    {
        get
        {
            if (_configurations == null)
            {
                _configurations = GetBinderConfigurations();
            }

            return _configurations;
        }
    }

    public List<string> FindMatchingConfigurationsIfAny(IBinder binder)
    {
        var results = new List<string>();
        var registryEntry = Registry.Get(binder.ServiceName);
        if (registryEntry != null)
        {
            foreach (var config in Configurations)
            {
                if (registryEntry.AssemblyPath == config.Value.ResolvedAssembly)
                {
                    results.Add(config.Key);
                }
            }
        }

        return results;
    }

    internal Dictionary<string, BinderConfiguration> GetBinderConfigurations()
    {
        var binderConfigurations = new Dictionary<string, BinderConfiguration>();
        var declaredBinders = Options.Binders;
        var defaultCandidatesExist = false;

        // Check to see if configuration has any declared default binders
        foreach (var binder in declaredBinders)
        {
            if (binder.Value.DefaultCandidate.Value)
            {
                defaultCandidatesExist = true;
                break;
            }
        }

        // Go thru and match up configured binders with what was discovered in "classpath"
        var existingBinderConfigurations = new List<string>();
        foreach (var declBinder in declaredBinders)
        {
            var binderOptions = declBinder.Value;
            if (Registry.Get(declBinder.Key) != null)
            {
                // Declared binder also found on "classpath"
                var binder = Registry.Get(declBinder.Key);
                binderConfigurations.Add(binder.Name, new BinderConfiguration(binder.ConfigureClass, binder.AssemblyPath, binderOptions));
            }
            else
            {
                // Declared binder, but not found on 'classpath'

                // Must have a Type configured for the configured binder
                if (string.IsNullOrEmpty(binderOptions.ConfigureClass))
                {
                    throw new InvalidOperationException("No 'Type' setting present for custom binder: " + declBinder.Key);
                }

                // Log, configured binder not found on "classpath"
                binderConfigurations.Add(declBinder.Key, new BinderConfiguration(binderOptions.ConfigureClass, binderOptions.ConfigureAssembly, binderOptions));
            }

            existingBinderConfigurations.Add(declBinder.Key);
        }

        // Check for a default binder in the matched up configuration
        foreach (var configurationEntry in binderConfigurations)
        {
            if (configurationEntry.Value.IsDefaultCandidate)
            {
                defaultCandidatesExist = true;
            }
        }

        // No Default candidate, make them all default
        if (!defaultCandidatesExist)
        {
            foreach (var binderEntry in Registry.GetAll())
            {
                if (!existingBinderConfigurations.Contains(binderEntry.Key))
                {
                    binderConfigurations.Add(binderEntry.Key, new BinderConfiguration(binderEntry.Value.ConfigureClass, binderEntry.Value.AssemblyPath, new BinderOptions() { DefaultCandidate = true, InheritEnvironment = true }));
                }
            }
        }

        return binderConfigurations;
    }
}