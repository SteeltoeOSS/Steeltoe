﻿// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Stream.Config;
using Steeltoe.Stream.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Stream.Binder
{
    public class DefaultBinderFactory : IBinderFactory
    {
        private readonly object _lock = new object();
        private readonly IBinderConfigurations _binderConfigurations;
        private readonly List<IBinderFactoryListener> _listeners; // TODO: implement listener callback
        private readonly IServiceProvider _services;
        private readonly IOptionsMonitor<BindingServiceOptions> _optionsMonitor;
        private Dictionary<string, IBinder> _binderInstanceCache;

        private BindingServiceOptions Options
        {
            get
            {
                return _optionsMonitor.CurrentValue;
            }
        }

        private string DefaultBinder
        {
            get
            {
                return Options.DefaultBinder;
            }
        }

        public DefaultBinderFactory(IServiceProvider services, IOptionsMonitor<BindingServiceOptions> optionsMonitor, IBinderConfigurations binderConfigurations, IEnumerable<IBinderFactoryListener> listeners = null)
        {
            if (binderConfigurations == null)
            {
                throw new ArgumentNullException(nameof(binderConfigurations));
            }

            _services = services;
            _optionsMonitor = optionsMonitor;
            _binderConfigurations = binderConfigurations;
            _listeners = listeners?.ToList();
        }

        // TODO: Figure out Disposable/Closable/DisposableBean usages
        // public void Dispose()
        // {
        //    if (_binderInstanceCache != null)
        //    {
        //        foreach (var binder in _binderInstanceCache)
        //        {
        //            binder.Value.Dispose();
        //        }

        // _binderInstanceCache = null;
        //    }
        // }
        public IBinder GetBinder(string name)
        {
            return GetBinder(name, typeof(object));
        }

        public IBinder GetBinder(string name, Type bindableType)
        {
            var binderName = !string.IsNullOrEmpty(name) ? name : DefaultBinder;
            IBinder result = null;
            var binders = _services.GetServices<IBinder>();

            if (!string.IsNullOrEmpty(binderName))
            {
                result = binders.SingleOrDefault((b) => b.Name == binderName);
            }
            else if (binders.Count() == 1)
            {
                result = binders.Single();
            }

            if (result == null && string.IsNullOrEmpty(binderName) && binders.Count() > 1)
            {
                throw new InvalidOperationException("Multiple binders are available, however neither default nor per-destination binder name is provided.");
            }

            if (result == null)
            {
                result = DoGetBinder(binderName, bindableType);
            }

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
                    foreach (var config in _binderConfigurations.Configurations)
                    {
                        if (config.Value.IsDefaultCandidate)
                        {
                            defaultCandidateConfigurations.Add(config.Key);
                        }
                    }

                    if (defaultCandidateConfigurations.Count == 1)
                    {
                        // Single defualt candidate
                        configurationName = defaultCandidateConfigurations.Single();
                    }
                    else
                    {
                        // Multiple defualt candidates, find by target type match
                        var candidatesForBindableType = new List<string>();
                        foreach (var defaultCandidateConfiguration in defaultCandidateConfigurations)
                        {
                            var binder = GetBinderInstance(defaultCandidateConfiguration);
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

            var binderInstance = GetBinderInstance(configurationName);
            if (binderInstance == null)
            {
                throw new InvalidOperationException("Unknown binder configuration: " + configurationName);
            }

            if (!VerifyBinderTypeMatchesTarget(binderInstance, bindableType))
            {
                throw new InvalidOperationException("The binder " + configurationName + " cannot bind a " + bindableType.FullName);
            }

            return binderInstance;
        }

        private IBinder GetBinderInstance(string configurationName)
        {
            BuildBinderCache();
            _binderInstanceCache.TryGetValue(configurationName, out var binder);

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
                        var binders = _services.GetServices<IBinder>();
                        foreach (var binder in binders)
                        {
                            var binderName = binder.Name;
                            _binderInstanceCache.TryAdd(binderName, binder);

                            var names = _binderConfigurations.FindMatchingConfigurationsIfAny(binder);
                            foreach (var name in names)
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
            return (binderInstance is IPollableConsumerBinder &&
                        GenericsUtils.CheckCompatiblePollableBinder(binderInstance, bindingTargetType)) ||
                        GenericsUtils.GetParameterType(binderInstance.GetType(), typeof(IBinder<>), 0)
                            .IsAssignableFrom(bindingTargetType);
        }
    }
}
