// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Stream.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Steeltoe.Stream.Binder
{
    public class DefaultBinderTypeRegistry : IBinderTypeRegistry
    {
        private static readonly string _thisAssemblyName = typeof(DefaultBinderTypeRegistry).Assembly.GetName().Name;
        private readonly Dictionary<string, IBinderType> _binderTypes;

        public DefaultBinderTypeRegistry()
        {
            var searchDirectories = new List<string>();
            var executingDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            searchDirectories.Add(executingDirectory);
            if (executingDirectory != Environment.CurrentDirectory)
            {
                searchDirectories.Add(Environment.CurrentDirectory);
            }

            SearchDirectories = searchDirectories;

            _binderTypes = FindBinders(searchDirectories);
        }

        public DefaultBinderTypeRegistry(List<string> searchDirectories, bool checkLoadedAssemblys = true)
        {
            SearchDirectories = searchDirectories;
            _binderTypes = FindBinders(searchDirectories, checkLoadedAssemblys);
        }

        internal DefaultBinderTypeRegistry(Dictionary<string, IBinderType> binderTypes)
        {
            _binderTypes = binderTypes;
        }

        internal List<string> SearchDirectories { get; }

        public IBinderType Get(string name)
        {
            _binderTypes.TryGetValue(name, out var result);
            return result;
        }

        public IDictionary<string, IBinderType> GetAll()
        {
            return _binderTypes;
        }

        internal static Dictionary<string, IBinderType> FindBinders(List<string> searchDirectories, bool checkLoadedAssemblys = true)
        {
            var binderTypes = new Dictionary<string, IBinderType>();

            ParseBinderConfigurations(searchDirectories, binderTypes, checkLoadedAssemblys);

            return binderTypes;
        }

        internal static void ParseBinderConfigurations(List<string> searchDirectories, Dictionary<string, IBinderType> registrations, bool checkLoadedAssemblys = true)
        {
            if (checkLoadedAssemblys)
            {
                AddBinderTypes(AppDomain.CurrentDomain.GetAssemblies(), registrations);
            }

            foreach (var path in searchDirectories)
            {
                AddBinderTypes(path, registrations);
            }
        }

        internal static void AddBinderTypes(Assembly[] assemblies, Dictionary<string, IBinderType> registrations)
        {
            foreach (var assembly in assemblies)
            {
                var binderType = CheckAssembly(assembly);
                if (binderType != null)
                {
                    registrations.TryAdd(binderType.Name, binderType);
                }
            }
        }

        internal static void AddBinderTypes(string directory, Dictionary<string, IBinderType> registrations)
        {
            var context = new SearchingAssemblyLoadContext();

            var dirinfo = new DirectoryInfo(directory);

            foreach (var file in dirinfo.EnumerateFiles("*.dll"))
            {
                try
                {
                    if (ShouldCheckFile(file))
                    {
                        var reg = LoadAndCheckAssembly(context, file.FullName);
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

            context.Unload();
        }

        internal static bool ShouldCheckFile(FileInfo file)
        {
            var fileName = Path.GetFileNameWithoutExtension(file.Name);
            if (fileName.Equals(_thisAssemblyName, StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }

            return true;
        }

        internal static IBinderType LoadAndCheckAssembly(AssemblyLoadContext context, string assemblyPath)
        {
            BinderType result = null;
            try
            {
                var assembly = context.LoadFromAssemblyPath(assemblyPath);
                if (assembly != null)
                {
                    return CheckAssembly(assembly);
                }
            }
            catch (Exception)
            {
                // Log
            }

            return result;
        }

        internal static IBinderType CheckAssembly(Assembly assembly)
        {
            var attr = assembly.GetCustomAttribute<BinderAttribute>();
            if (attr != null)
            {
                return new BinderType(attr.Name, attr.ConfigureClass, assembly.Location);
            }

            return null;
        }

        internal class SearchingAssemblyLoadContext : AssemblyLoadContext
        {
            public SearchingAssemblyLoadContext()
                : base("SearchingLoadContext", true)
            {
            }
        }
    }
}
