// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.InteropServices;
using Steeltoe.Stream.Attributes;

namespace Steeltoe.Stream.Binder;

public class DefaultBinderTypeRegistry : IBinderTypeRegistry
{
    private static readonly string ThisAssemblyName = typeof(DefaultBinderTypeRegistry).Assembly.GetName().Name;
    private readonly Dictionary<string, IBinderType> _binderTypes;

    internal List<string> SearchDirectories { get; }

    public DefaultBinderTypeRegistry()
    {
        var searchDirectories = new List<string>();
        string executingDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        searchDirectories.Add(executingDirectory);

        if (executingDirectory != Environment.CurrentDirectory)
        {
            searchDirectories.Add(Environment.CurrentDirectory);
        }

        SearchDirectories = searchDirectories;

        _binderTypes = FindBinders(searchDirectories);
    }

    public DefaultBinderTypeRegistry(List<string> searchDirectories, bool checkLoadedAssemblies = true)
    {
        SearchDirectories = searchDirectories;
        _binderTypes = FindBinders(searchDirectories, checkLoadedAssemblies);
    }

    internal DefaultBinderTypeRegistry(Dictionary<string, IBinderType> binderTypes)
    {
        _binderTypes = binderTypes;
    }

    public IBinderType Get(string name)
    {
        _binderTypes.TryGetValue(name, out IBinderType result);
        return result;
    }

    public IDictionary<string, IBinderType> GetAll()
    {
        return _binderTypes;
    }

    internal static Dictionary<string, IBinderType> FindBinders(List<string> searchDirectories, bool checkLoadedAssemblies = true)
    {
        var binderTypes = new Dictionary<string, IBinderType>();

        ParseBinderConfigurations(searchDirectories, binderTypes, checkLoadedAssemblies);

        return binderTypes;
    }

    internal static void ParseBinderConfigurations(List<string> searchDirectories, Dictionary<string, IBinderType> registrations,
        bool checkLoadedAssemblies = true)
    {
        if (checkLoadedAssemblies)
        {
            AddBinderTypes(AppDomain.CurrentDomain.GetAssemblies(), registrations);
        }

        foreach (string path in searchDirectories)
        {
            AddBinderTypes(path, registrations);
        }
    }

    internal static void AddBinderTypes(Assembly[] assemblies, Dictionary<string, IBinderType> registrations)
    {
        foreach (Assembly assembly in assemblies)
        {
            IBinderType binderType = CheckAssembly(assembly);

            if (binderType != null)
            {
                registrations.TryAdd(binderType.Name, binderType);
            }
        }
    }

    internal static void AddBinderTypes(string directory, Dictionary<string, IBinderType> registrations)
    {
        var context = new MetadataLoadContext(GetAssemblyResolver(directory));
        var directoryInfo = new DirectoryInfo(directory);

        foreach (FileInfo file in directoryInfo.EnumerateFiles("*.dll"))
        {
            try
            {
                if (ShouldCheckFile(file))
                {
                    IBinderType reg = LoadAndCheckAssembly(context, file.FullName);

                    if (reg != null)
                    {
                        registrations.TryAdd(reg.Name, reg);
                    }
                }
            }
            catch (Exception)
            {
                // log
            }
        }

        context.Dispose();
    }

    internal static bool ShouldCheckFile(FileInfo file)
    {
        string fileName = Path.GetFileNameWithoutExtension(file.Name);

        if (fileName.Equals(ThisAssemblyName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    internal static IBinderType LoadAndCheckAssembly(MetadataLoadContext context, string assemblyPath)
    {
        BinderType result = null;

        try
        {
            Assembly assembly = context.LoadFromAssemblyPath(assemblyPath);

            if (assembly != null)
            {
                return CheckAssembly(assembly);
            }
        }
        catch
        {
            // most failures here are situations that aren't relevant, so just fail silently
        }

        return result;
    }

    internal static IBinderType CheckAssembly(Assembly assembly)
    {
        foreach (CustomAttributeData data in assembly.GetCustomAttributesData())
        {
            if (data.AttributeType.FullName == typeof(BinderAttribute).FullName)
            {
                return new BinderType(GetName(data), GetConfigureClass(data), assembly.Location);
            }
        }

        return null;
    }

    internal static PathAssemblyResolver GetAssemblyResolver(string directory)
    {
        var paths = new List<string>();
        paths.AddRange(Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll"));
        paths.AddRange(Directory.GetFiles(directory, "*.dll"));
        return new PathAssemblyResolver(paths);
    }

    internal static string GetName(CustomAttributeData data)
    {
        if (data.ConstructorArguments[0].Value is not string result)
        {
            result = GetNamedArgument<string>(data.NamedArguments, "Name");
        }

        return result;
    }

    internal static T GetNamedArgument<T>(IList<CustomAttributeNamedArgument> namedArguments, string name)
        where T : class
    {
        foreach (CustomAttributeNamedArgument arg in namedArguments)
        {
            if (arg.MemberName == name)
            {
                return (T)arg.TypedValue.Value;
            }
        }

        return default;
    }

    internal static string GetConfigureClass(CustomAttributeData data)
    {
        Type type = data.ConstructorArguments[1].Value as Type ?? GetNamedArgument<Type>(data.NamedArguments, "ConfigureType");

        return type?.AssemblyQualifiedName;
    }
}
