// Copyright 2017 the original author or authors.
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
using Steeltoe.Messaging.Core;
using Steeltoe.Stream.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Steeltoe.Stream.Binding
{
    public static class BindingHelpers
    {
        public static T GetBindable<T>(IServiceProvider provider, string name)
        {
            return (T)GetBindableTarget(provider, name);
        }

        public static object GetBindableTarget(IServiceProvider provider, string name)
        {
            var registry = provider.GetRequiredService<IDestinationRegistry>();
            return registry.Lookup(name);
        }

        public static IDictionary<string, Bindable> CollectBindables(Type binding)
        {
            IDictionary<string, Bindable> bindables = new Dictionary<string, Bindable>();
            CollectFromProperties(binding, bindables);
            CollectFromMethods(binding, bindables);
            return bindables;
        }

        internal static void CollectFromProperties(Type binding, IDictionary<string, Bindable> targets)
        {
            var infos = binding.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var info in infos)
            {
                var getMethod = info.GetGetMethod();
                if (getMethod != null)
                {
                    if (info.GetCustomAttribute(typeof(InputAttribute)) is InputAttribute attribute)
                    {
                        var target = new Bindable()
                        {
                            IsInput = true,
                            Name = attribute.Name ?? info.Name,
                            BindingTargetType = getMethod.ReturnType,
                            FactoryMethod = getMethod
                        };

                        AddBindableComponent(target, targets);
                    }

                    if (info.GetCustomAttribute(typeof(OutputAttribute)) is OutputAttribute attribute2)
                    {
                        var target = new Bindable()
                        {
                            IsInput = false,
                            Name = attribute2.Name ?? info.Name,
                            BindingTargetType = getMethod.ReturnType,
                            FactoryMethod = getMethod
                        };

                        AddBindableComponent(target, targets);
                    }
                }
            }

            foreach (var iface in binding.GetInterfaces())
            {
                CollectFromProperties(iface, targets);
            }
        }

        internal static void CollectFromMethods(Type binding, IDictionary<string, Bindable> components)
        {
            var meths = binding.GetMethods(BindingFlags.Instance | BindingFlags.Public);
            foreach (var meth in meths)
            {
                if (meth.GetCustomAttribute(typeof(InputAttribute)) is InputAttribute attribute)
                {
                    var target = new Bindable()
                    {
                        IsInput = true,
                        Name = attribute.Name ?? meth.Name,
                        BindingTargetType = meth.ReturnType,
                        FactoryMethod = meth
                    };

                    AddBindableComponent(target, components);
                }

                if (meth.GetCustomAttribute(typeof(OutputAttribute)) is OutputAttribute attribute2)
                {
                    var target = new Bindable()
                    {
                        IsInput = false,
                        Name = attribute2.Name ?? meth.Name,
                        BindingTargetType = meth.ReturnType,
                        FactoryMethod = meth
                    };

                    AddBindableComponent(target, components);
                }
            }

            foreach (var iface in binding.GetInterfaces())
            {
                CollectFromMethods(iface, components);
            }
        }

        internal static void AddBindableComponent(Bindable component, IDictionary<string, Bindable> components)
        {
            if (components.ContainsKey(component.Name))
            {
                throw new InvalidOperationException("Duplicate bindable target with name: " + component.Name);
            }

            components.Add(component.Name, component);
        }
    }

    public struct Bindable
    {
        public bool IsInput { get; set; }

        public string Name { get; set; }

        public Type BindingTargetType { get; set; }

        public MethodInfo FactoryMethod { get; set; }
    }
}