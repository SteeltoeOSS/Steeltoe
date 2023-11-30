// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Services;

public sealed class ServiceRegistration
{
    [JsonIgnore]
    internal string Name { get; }

    [JsonPropertyName("scope")]
    public string Scope { get; }

    [JsonPropertyName("type")]
    public string Type { get; }

    [JsonPropertyName("resource")]
    public string AssemblyName { get; }

    [JsonPropertyName("dependencies")]
    public ISet<string> Dependencies { get; }

    public ServiceRegistration(ServiceDescriptor descriptor)
    {
        ArgumentGuard.NotNull(descriptor);

        Name = descriptor.ServiceType.Name;
        Scope = descriptor.Lifetime.ToString();
        Type = descriptor.ImplementationInstance?.GetType().ToString() ?? descriptor.ServiceType.ToString();
        AssemblyName = descriptor.ServiceType.AssemblyQualifiedName!;
        Dependencies = GetDependencies(descriptor);
    }

    private static ISet<string> GetDependencies(ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationType == null)
        {
            return ImmutableHashSet<string>.Empty;
        }

        var dependencies = new HashSet<string>();
        ConstructorInfo[] constructors = descriptor.ImplementationType.GetConstructors();

        ConstructorInfo? preferredConstructor = Array.Find(constructors,
            constructor => constructor.GetCustomAttribute(typeof(ActivatorUtilitiesConstructorAttribute)) != null);

        if (preferredConstructor != null)
        {
            IncludeParametersFrom(preferredConstructor, dependencies);
        }
        else
        {
            foreach (ConstructorInfo constructor in constructors)
            {
                IncludeParametersFrom(constructor, dependencies);
            }
        }

        return dependencies;
    }

    private static void IncludeParametersFrom(ConstructorInfo constructor, ISet<string> dependencies)
    {
        foreach (ParameterInfo parameter in constructor.GetParameters())
        {
            string dependency = parameter.ParameterType.ToString();
            dependencies.Add(dependency);
        }
    }

    public override string ToString()
    {
        return $"{nameof(Name)}: {Name} {nameof(Scope)}: {Scope} {nameof(Type)}: {Type}";
    }
}
