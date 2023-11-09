// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Common;

namespace Steeltoe.Bootstrap.AutoConfiguration;

internal sealed class AssemblyLoader
{
    public IReadOnlySet<string> AssemblyNamesToExclude { get; }

    static AssemblyLoader()
    {
        AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolver.LoadAnyVersion;
    }

    public AssemblyLoader(IReadOnlySet<string> assemblyNamesToExclude)
    {
        ArgumentGuard.NotNull(assemblyNamesToExclude);
        ArgumentGuard.ElementsNotNullOrEmpty(assemblyNamesToExclude);

        // Take a copy to ensure comparisons are case insensitive.
        AssemblyNamesToExclude = assemblyNamesToExclude.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public bool IsAssemblyLoaded(string assemblyName)
    {
        ArgumentGuard.NotNullOrEmpty(assemblyName);

        if (AssemblyNamesToExclude.Contains(assemblyName))
        {
            return false;
        }

        return TryLoadAssembly(assemblyName);
    }

    private static bool TryLoadAssembly(string assemblyName)
    {
        try
        {
            _ = Assembly.Load(assemblyName);
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or BadImageFormatException)
        {
            return false;
        }
    }

    private static class AssemblyResolver
    {
        private static readonly HashSet<string> FailedAssemblyNames = new(StringComparer.OrdinalIgnoreCase);

        public static Assembly? LoadAnyVersion(object? sender, ResolveEventArgs args)
        {
            // Workaround for Sonar bug at https://github.com/SonarSource/sonar-dotnet/issues/8371.
            _ = sender;

            // Load whatever version is available (ignore Version, Culture and PublicKeyToken).
            string assemblySimpleName = new AssemblyName(args.Name).Name!;

            if (FailedAssemblyNames.Contains(assemblySimpleName))
            {
                return null;
            }

            // AssemblyName.Equals() returns false when path and full name are identical, so the code below
            // avoids a crash caused by inserting duplicate keys in the dictionary.
            Dictionary<string, Assembly> assembliesBySimpleName = AppDomain.CurrentDomain.GetAssemblies().GroupBy(nextAssembly => nextAssembly.GetName().Name!)
                .ToDictionary(grouping => grouping.Key, grouping => grouping.First(), StringComparer.OrdinalIgnoreCase);

            if (assembliesBySimpleName.TryGetValue(assemblySimpleName, out Assembly? assembly))
            {
                return assembly;
            }

            if (args.Name.Contains(".resources", StringComparison.Ordinal))
            {
                return args.RequestingAssembly;
            }

            // Prevent recursive attempts to resolve.
            FailedAssemblyNames.Add(assemblySimpleName);
            assembly = Assembly.Load(assemblySimpleName);
            FailedAssemblyNames.Remove(assemblySimpleName);

            return assembly;
        }
    }
}
