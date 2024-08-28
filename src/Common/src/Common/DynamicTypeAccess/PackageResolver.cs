// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection;

namespace Steeltoe.Common.DynamicTypeAccess;

/// <summary>
/// Dynamically loads <see cref="Type" />s at runtime from a list of candidates.
/// </summary>
internal abstract class PackageResolver
{
    private static readonly IReadOnlySet<string> EmptySet = ImmutableHashSet<string>.Empty;

    private readonly IReadOnlyList<string> _assemblyNames;
    private readonly IReadOnlyList<string> _packageNames;

    protected PackageResolver(string assemblyName, string packageName)
        : this([assemblyName], [packageName])
    {
    }

    protected PackageResolver(IReadOnlyList<string> assemblyNames, IReadOnlyList<string> packageNames)
    {
        ArgumentNullException.ThrowIfNull(assemblyNames);
        ArgumentGuard.ElementsNotNullOrWhiteSpace(assemblyNames);
        ArgumentNullException.ThrowIfNull(packageNames);
        ArgumentGuard.ElementsNotNullOrWhiteSpace(packageNames);

        _assemblyNames = assemblyNames;
        _packageNames = packageNames;
    }

    public bool IsAvailable()
    {
        return IsAvailable(EmptySet);
    }

    public bool IsAvailable(IReadOnlySet<string> assemblyNamesToExclude)
    {
        return IsAssemblyAvailable(assemblyNamesToExclude);
    }

    protected virtual bool IsAssemblyAvailable(IReadOnlySet<string> assemblyNamesToExclude)
    {
        foreach (string assemblyName in _assemblyNames)
        {
            if (!assemblyNamesToExclude.Contains(assemblyName))
            {
                try
                {
                    Assembly.Load(new AssemblyName(assemblyName));
                    return true;
                }
                catch (Exception exception) when (exception is ArgumentException or IOException or BadImageFormatException)
                {
                    // Intentionally left empty.
                }
            }
        }

        return false;
    }

    protected TypeAccessor ResolveType(params string[] typeNames)
    {
        ArgumentNullException.ThrowIfNull(typeNames);
        ArgumentGuard.ElementsNotNullOrWhiteSpace(typeNames);

        List<Exception> exceptions = [];

        // A type can be moved to a different assembly in a future NuGet version, so probe all combinations to be resilient against that.
        foreach (string assemblyName in _assemblyNames)
        {
            try
            {
                Assembly assembly = Assembly.Load(new AssemblyName(assemblyName));

                foreach (string typeName in typeNames)
                {
                    try
                    {
                        Type type = assembly.GetType(typeName, true)!;
                        return new TypeAccessor(type);
                    }
                    catch (Exception exception) when (exception is ArgumentException or IOException or BadImageFormatException or TypeLoadException)
                    {
                        exceptions.Add(exception);
                    }
                }
            }
            catch (Exception exception) when (exception is ArgumentException or IOException or BadImageFormatException)
            {
                exceptions.Add(exception);
            }
        }

        throw new AggregateException(
            _packageNames.Count == 1
                ? $"Unable to load a required type. Please add the '{_packageNames[0]}' NuGet package to your project."
                : $"Unable to load a required type. Please add one of these NuGet packages to your project: '{string.Join("', '", _packageNames)}'.",
            exceptions);
    }
}
