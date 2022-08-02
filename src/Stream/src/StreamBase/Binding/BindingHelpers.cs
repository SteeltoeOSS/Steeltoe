// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging;
using Steeltoe.Stream.Attributes;

namespace Steeltoe.Stream.Binding;

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
        PropertyInfo[] infos = binding.GetProperties(BindingFlags.Instance | BindingFlags.Public);

        foreach (PropertyInfo info in infos)
        {
            MethodInfo getMethod = info.GetGetMethod();

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

        foreach (Type @interface in binding.GetInterfaces())
        {
            CollectFromProperties(@interface, targets);
        }
    }

    internal static void CollectFromMethods(Type binding, IDictionary<string, Bindable> components)
    {
        MethodInfo[] methods = binding.GetMethods(BindingFlags.Instance | BindingFlags.Public);

        foreach (MethodInfo method in methods)
        {
            if (method.GetCustomAttribute(typeof(InputAttribute)) is InputAttribute attribute)
            {
                var target = new Bindable
                {
                    IsInput = true,
                    Name = attribute.Name ?? method.Name,
                    BindingTargetType = method.ReturnType,
                    FactoryMethod = method
                };

                AddBindableComponent(target, components);
            }

            if (method.GetCustomAttribute(typeof(OutputAttribute)) is OutputAttribute attribute2)
            {
                var target = new Bindable
                {
                    IsInput = false,
                    Name = attribute2.Name ?? method.Name,
                    BindingTargetType = method.ReturnType,
                    FactoryMethod = method
                };

                AddBindableComponent(target, components);
            }
        }

        foreach (Type @interface in binding.GetInterfaces())
        {
            CollectFromMethods(@interface, components);
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
