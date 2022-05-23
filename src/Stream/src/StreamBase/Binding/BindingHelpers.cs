// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;
using Steeltoe.Stream.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Steeltoe.Stream.Binding
{
    public static class BindingHelpers
    {
        public static T GetBindable<T>(IApplicationContext context, string name)
        {
            return context.GetService<T>(name);
        }

        public static object GetBindableTarget(IApplicationContext context, string name)
        {
            return context.GetService<IMessageChannel>(name);
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
                        var target = new Bindable
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
                        var target = new Bindable
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
                    var target = new Bindable
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
                    var target = new Bindable
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
                throw new InvalidOperationException($"Duplicate bindable target with name: {component.Name}");
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