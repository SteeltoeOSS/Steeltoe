// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Steeltoe.Common.Reflection
{
    public static class ReflectionHelpers
    {
        /// <summary>
        /// Try to load an assembly
        /// </summary>
        /// <param name="assembly">Name of the assembly</param>
        /// <returns>Boolean indicating success/failure of finding the assembly</returns>
        public static bool IsAssemblyLoaded(string assembly)
        {
            try
            {
                Assembly.Load(assembly);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Find an assembly
        /// </summary>
        /// <param name="name">Name of the assembly to find</param>
        /// <returns>A representation of the assembly</returns>
        public static Assembly FindAssembly(string name)
        {
            try
            {
                var a = Assembly.Load(new AssemblyName(name));

                return a;
            }
            catch (Exception)
            {
                // Sometimes dependencies are missing... Should be handled later in framework code
            }

            return null;
        }

        /// <summary>
        /// Find assemblies matching a query
        /// </summary>
        /// <param name="assemblyQuery">Your assembly search query</param>
        /// <returns>Assemblies in <see cref="AppDomain.CurrentDomain" /> matching the query</returns>
        public static IEnumerable<Assembly> FindAssemblies(Func<Assembly, bool> assemblyQuery)
            => AppDomain.CurrentDomain.GetAssemblies().AsParallel().Where(assemblyQuery);

        /// <summary>
        /// Find types from assemblies matching the query that are based on a common type
        /// </summary>
        /// <param name="assemblyQuery">Your assembly search query</param>
        /// <param name="baseType">Base type to search for</param>
        /// <returns>A list of types that have the given type as a base type</returns>
        public static IEnumerable<Type> FindDescendantTypes(Func<Assembly, bool> assemblyQuery, Type baseType)
            => FindAssemblies(assemblyQuery).SelectMany(a => a.GetTypes().Where(t => t.BaseType == baseType));

        /// <summary>
        /// Find a type specified in an assembly attribute
        /// </summary>
        /// <typeparam name="T">The attribute that defines the type to get</typeparam>
        /// <returns>A list of matching types. Won't return more than one type per assembly</returns>
        public static IEnumerable<Type> FindTypeFromAssemblyAttribute<T>()
            where T : AssemblyContainsTypeAttribute
                => FindAssembliesWithAttribute<T>().Select(assembly => assembly.GetCustomAttribute<T>().ContainedType);

        /// <summary>
        /// Find a list of types specified in an assembly attribute
        /// </summary>
        /// <typeparam name="T">The attribute that defines the types to get</typeparam>
        /// <returns>A list of matching types</returns>
        public static IEnumerable<Assembly> FindAssembliesWithAttribute<T>()
            where T : AssemblyContainsTypeAttribute
        {
            TryLoadAssembliesWithAttribute<T>();
            return FindAssemblies(assembly => assembly.GetCustomAttribute<T>() != null);
        }

        /// <summary>
        /// Finds a list of types with <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">Type of <see cref="Attribute"/> to search for</typeparam>
        /// <param name="assembly">The assembly to search for the type(s)</param>
        /// <returns>A list of types with the specified attribute</returns>
        public static IEnumerable<Type> FindTypesWithAttribute<T>(Assembly assembly)
            where T : Attribute
                => assembly.GetTypes().Where(type => type.IsDefined(typeof(T)));

        /// <summary>
        /// Finds a list of assemblies with <typeparamref name="TTypeAttribute"/> contained within assemblies with <typeparamref name="TAssemblyAttribute"/>
        /// </summary>
        /// <typeparam name="TTypeAttribute">The Type attribute to locate</typeparam>
        /// <typeparam name="TAssemblyAttribute">The Assembly-level attribute to use to filter the assembly list</typeparam>
        /// <returns>Matching types from within matching assemblies</returns>
        public static IEnumerable<Type> FindTypesWithAttributeFromAssemblyAttribute<TTypeAttribute, TAssemblyAttribute>()
            where TTypeAttribute : Attribute
            where TAssemblyAttribute : AssemblyContainsTypeAttribute
                => FindAssembliesWithAttribute<TAssemblyAttribute>()
                    .SelectMany(FindTypesWithAttribute<TTypeAttribute>);

        /// <summary>
        /// Finds a list of types with the attributed identified by <typeparamref name="T"/><para></para>
        /// </summary>
        /// <typeparam name="T">The assembly attribute that defines the desired type</typeparam>
        /// <returns>Matching types from within matching assemblies</returns>
        public static IEnumerable<Type> FindAttributedTypesFromAssemblyAttribute<T>()
            where T : AssemblyContainsTypeAttribute
                => FindAssembliesWithAttribute<T>()
                        .SelectMany(assemblies =>
                            assemblies.GetTypes()
                                .Where(t => t.IsDefined(assemblies.GetCustomAttribute<T>()?.ContainedType)));

        /// <summary>
        /// Finds a list of types implementing the interface identified by <typeparamref name="TAttribute"/><para></para>
        /// </summary>
        /// <typeparam name="TAttribute">The assembly attribute that defines the desired interface type</typeparam>
        /// <returns>Matching types from within matching assemblies</returns>
        public static IEnumerable<Type> FindInterfacedTypesFromAssemblyAttribute<TAttribute>()
            where TAttribute : AssemblyContainsTypeAttribute
                => FindAssembliesWithAttribute<TAttribute>()
                    .SelectMany(assemblies =>
                        assemblies.GetTypes()
                            .Where(types => assemblies.GetCustomAttribute<TAttribute>().ContainedType.IsAssignableFrom(types)));

        /// <summary>
        /// Search a list of assemblies for the first matching type
        /// </summary>
        /// <param name="assemblyNames">List of assembly names to search</param>
        /// <param name="typeNames">List of suitable types</param>
        /// <returns>An appropriate type</returns>
        /// <remarks>Great for finding an implementation type that could have one or more names in one or more assemblies</remarks>
        public static Type FindType(string[] assemblyNames, string[] typeNames)
        {
            foreach (var assemblyName in assemblyNames)
            {
                var assembly = FindAssembly(assemblyName);
                if (assembly != null)
                {
                    foreach (var type in typeNames)
                    {
                        var result = FindType(assembly, type);
                        if (result != null)
                        {
                            return result;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Find a type from within an assembly
        /// </summary>
        /// <param name="assembly">The assembly to search</param>
        /// <param name="typeName">The name of the type to retrieve</param>
        /// <returns>The type</returns>
        public static Type FindType(Assembly assembly, string typeName)
        {
            try
            {
                return assembly.GetType(typeName);
            }
            catch (Exception)
            {
                // Sometimes dependencies are missing... Should be handled later in framework code
            }

            return null;
        }

        /// <summary>
        /// Search a list of assemblies for the first matching type
        /// </summary>
        /// <param name="assemblyNames">List of assembly names to search</param>
        /// <param name="typeNames">List of suitable types</param>
        /// <param name="typeName">To use in exception</param>
        /// <param name="assemblyShortDescription">Describe what might be missing</param>
        /// <returns>An appropriate type</returns>
        /// <remarks>Great for finding an implementation type that could have one or more names in one or more assemblies</remarks>
        /// <exception cref="Exception">When type isn't found</exception>
        public static Type FindTypeOrThrow(string[] assemblyNames, string[] typeNames, string typeName, string assemblyShortDescription)
        {
            var type = FindType(assemblyNames, typeNames);
            if (type == null)
            {
                throw new TypeLoadException($"Unable to find {typeName}, are you missing {assemblyShortDescription}?");
            }

            return type;
        }

        /// <summary>
        /// Find a method within a type
        /// </summary>
        /// <param name="type">The type to search</param>
        /// <param name="methodName">The name of the method</param>
        /// <param name="parameters">(Optional) The parameters in the signature</param>
        /// <returns>The method you're searching for</returns>
        public static MethodInfo FindMethod(Type type, string methodName, Type[] parameters = null)
        {
            try
            {
                if (parameters != null)
                {
                    return type.GetMethod(methodName, parameters);
                }

                return type.GetMethod(methodName);
            }
            catch (Exception)
            {
                // Sometimes dependencies are missing... Should be handled later in framework code
            }

            return null;
        }

        /// <summary>
        /// Invoke a function
        /// </summary>
        /// <param name="member">The method to execute</param>
        /// <param name="instance">Instance of an object, if required by the method</param>
        /// <param name="args">Arguments to pass to the method</param>
        /// <returns>Results of method call</returns>
        public static object Invoke(MethodBase member, object instance, object[] args)
        {
            try
            {
                return member.Invoke(instance, args);
            }
            catch (Exception)
            {
                // Sometimes dependencies are missing... Should be handled later in framework code
            }

            return null;
        }

        /// <summary>
        /// Create an instance of a type
        /// </summary>
        /// <param name="t">Type to instantiate</param>
        /// <param name="args">Constructor parameters</param>
        /// <returns>New instance of desired type</returns>
        public static object CreateInstance(Type t, object[] args = null)
        {
            try
            {
                if (args == null)
                {
                    return Activator.CreateInstance(t);
                }
                else
                {
                    return Activator.CreateInstance(t, args);
                }
            }
            catch (Exception)
            {
                // Sometimes dependencies are missing... Should be handled later in framework code
            }

            return null;
        }

        /// <summary>
        /// Try to set a property on an object
        /// </summary>
        /// <param name="obj">Object to set a value on</param>
        /// <param name="property">Property to set</param>
        /// <param name="value">Value to use</param>
        public static void TrySetProperty(object obj, string property, object value)
        {
            var prop = obj.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(obj, value, null);
            }
        }

        /// <summary>
        /// Try to make sure all assemblies with the given attribute have been loaded into the current AppDomain
        /// </summary>
        /// <typeparam name="T">Assembly Attribute Type</typeparam>
        /// <remarks>This method depends on assemblies existing as separate files on disk and will not work for applications published with /p:PublishSingleFile=true</remarks>
        private static void TryLoadAssembliesWithAttribute<T>()
            where T : AssemblyContainsTypeAttribute
        {
            var runtimeAssemblies = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");
            using var loadContext = new MetadataLoadContext(new PathAssemblyResolver(AllRelevantPaths(runtimeAssemblies, typeof(T))));
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            var assemblypaths = Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory).Where(f => f.EndsWith("dll", StringComparison.InvariantCultureIgnoreCase));
            foreach (var assembly in assemblypaths)
            {
                var filename = Path.GetFileNameWithoutExtension(assembly);
                if (!loadedAssemblies.Any(a => a.FullName.StartsWith(filename, StringComparison.InvariantCultureIgnoreCase)))
                {
                    try
                    {
                        var assemblyRef = loadContext.LoadFromAssemblyPath(assembly);

                        // haven't been able to get actual type comparison to work (assembly of the attribute not found?), falling back on matching the type name
                        if (CustomAttributeData.GetCustomAttributes(assemblyRef).Any(attr => attr.AttributeType.FullName.Equals(typeof(T).FullName)))
                        {
                            FindAssembly(filename);
                        }
                    }
                    catch
                    {
                        // most failures here are situations that aren't relevant, so just fail silently
                    }
                }
            }
        }

        /// <summary>
        /// Build a list of file paths that are relevant to this task
        /// </summary>
        /// <param name="runtimeAssemblies">Paths to dotnet runtime files</param>
        /// <param name="attributeType">The assembly attribute being searched for</param>
        /// <returns>A list of paths to the runtime, assembly and requested assembly type</returns>
        private static List<string> AllRelevantPaths(string[] runtimeAssemblies, Type attributeType)
        {
            var toReturn = new List<string>(runtimeAssemblies);
            var executingAssemblyLocation = Assembly.GetExecutingAssembly().Location;
            var typeAssemblyLocation = attributeType.Assembly.Location;
            if (string.IsNullOrEmpty(executingAssemblyLocation) || string.IsNullOrEmpty(typeAssemblyLocation))
            {
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                if (baseDirectory.EndsWith("\\"))
                {
                    baseDirectory = baseDirectory.Substring(0, baseDirectory.Length - 1);
                }

                toReturn.Add(baseDirectory);
                Console.WriteLine("File path path information for the assembly containing {0} is missing. Some Steeltoe functionality may not work with PublishSingleFile=true", attributeType.Name);
            }
            else
            {
                toReturn.Add(executingAssemblyLocation);
                toReturn.Add(attributeType.Assembly.Location);
            }

            return toReturn;
        }
    }
}
