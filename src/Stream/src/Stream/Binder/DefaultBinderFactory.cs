// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Contexts;
using Steeltoe.Stream.Configuration;
using Steeltoe.Stream.Util;

namespace Steeltoe.Stream.Binder;

public class DefaultBinderFactory : IBinderFactory, IDisposable
{
    private readonly object _lock = new();
    private readonly IBinderConfigurations _binderConfigurations;
    private readonly List<IBinderFactoryListener> _listeners; // TODO: implement listener callback
    private readonly IApplicationContext _context;
    private readonly IOptionsMonitor<BindingServiceOptions> _optionsMonitor;
    private Dictionary<string, IBinder> _binderInstanceCache;

    private BindingServiceOptions Options => _optionsMonitor.CurrentValue;

    private string DefaultBinder => Options.DefaultBinder;

    public DefaultBinderFactory(IApplicationContext context, IOptionsMonitor<BindingServiceOptions> optionsMonitor, IBinderConfigurations binderConfigurations,
        IEnumerable<IBinderFactoryListener> listeners = null)
    {
        ArgumentGuard.NotNull(binderConfigurations);

        _listeners = listeners?.ToList();
        _context = context;
        _optionsMonitor = optionsMonitor;
        _binderConfigurations = binderConfigurations;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_binderInstanceCache != null)
            {
                foreach (KeyValuePair<string, IBinder> binder in _binderInstanceCache)
                {
                    binder.Value.Dispose();
                }

                _binderInstanceCache = null;
            }

            _context?.Dispose();
        }
    }

    public IBinder GetBinder(string name)
    {
        return GetBinder(name, typeof(object));
    }

    public IBinder GetBinder(string name, Type bindableType)
    {
        string binderName = !string.IsNullOrEmpty(name) ? name : DefaultBinder;
        IBinder result = null;
        IEnumerable<IBinder> binders = _context.GetServices<IBinder>();

        if (!string.IsNullOrEmpty(binderName))
        {
            result = binders.SingleOrDefault(b => b.ServiceName == binderName);
        }
        else if (binders.Count() == 1)
        {
            result = binders.Single();
        }

        if (result == null && string.IsNullOrEmpty(binderName) && binders.Count() > 1)
        {
            throw new InvalidOperationException("Multiple binders are available, however neither default nor per-destination binder name is provided.");
        }

        result ??= DoGetBinder(binderName, bindableType);
        return result;
    }

    private IBinder DoGetBinder(string name, Type bindableType)
    {
        string configurationName;

        if (string.IsNullOrEmpty(name))
        {
            var defaultCandidateConfigurations = new HashSet<string>();

            if (string.IsNullOrEmpty(DefaultBinder))
            {
                foreach (KeyValuePair<string, BinderConfiguration> configuration in _binderConfigurations.Configurations)
                {
                    if (configuration.Value.IsDefaultCandidate)
                    {
                        defaultCandidateConfigurations.Add(configuration.Key);
                    }
                }

                if (defaultCandidateConfigurations.Count == 1)
                {
                    // Single default candidate
                    configurationName = defaultCandidateConfigurations.Single();
                }
                else
                {
                    // Multiple default candidates, find by target type match
                    var candidatesForBindableType = new List<string>();

                    foreach (string defaultCandidateConfiguration in defaultCandidateConfigurations)
                    {
                        IBinder binder = GetBinderInstance(defaultCandidateConfiguration);

                        if (VerifyBinderTypeMatchesTarget(binder, bindableType))
                        {
                            candidatesForBindableType.Add(defaultCandidateConfiguration);
                        }
                    }

                    if (candidatesForBindableType.Count == 1)
                    {
                        configurationName = candidatesForBindableType.Single();
                    }
                    else
                    {
                        throw new InvalidOperationException("A default binder has been requested, but there are too many candidates or none available");
                    }
                }
            }
            else
            {
                configurationName = DefaultBinder;
            }
        }
        else
        {
            configurationName = name;
        }

        IBinder binderInstance = GetBinderInstance(configurationName);

        if (binderInstance == null)
        {
            throw new InvalidOperationException($"Unknown binder configuration: {configurationName}");
        }

        if (!VerifyBinderTypeMatchesTarget(binderInstance, bindableType))
        {
            throw new InvalidOperationException($"The binder {configurationName} cannot bind a {bindableType.FullName}");
        }

        return binderInstance;
    }

    private IBinder GetBinderInstance(string configurationName)
    {
        BuildBinderCache();
        _binderInstanceCache.TryGetValue(configurationName, out IBinder binder);

        return binder;
    }

    private void BuildBinderCache()
    {
        if (_binderInstanceCache == null)
        {
            lock (_lock)
            {
                if (_binderInstanceCache == null)
                {
                    _binderInstanceCache = new Dictionary<string, IBinder>();
                    IEnumerable<IBinder> binders = _context.GetServices<IBinder>();

                    foreach (IBinder binder in binders)
                    {
                        string binderName = binder.ServiceName;
                        _binderInstanceCache.TryAdd(binderName, binder);

                        List<string> names = _binderConfigurations.FindMatchingConfigurationsIfAny(binder);

                        foreach (string name in names)
                        {
                            _binderInstanceCache.TryAdd(name, binder);
                        }
                    }
                }
            }
        }
    }

    private bool VerifyBinderTypeMatchesTarget(IBinder binderInstance, Type bindingTargetType)
    {
        return (binderInstance is IPollableConsumerBinder && GenericsUtils.CheckCompatiblePollableBinder(binderInstance, bindingTargetType)) ||
            GenericsUtils.GetParameterType(binderInstance.GetType(), typeof(IBinder<>), 0).IsAssignableFrom(bindingTargetType);
    }
}
