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

using System;
using System.Reflection;

namespace Steeltoe.Common.Reflection
{
    public static class ReflectionHelpers
    {
        /// <summary>
        /// Find an assembly
        /// </summary>
        /// <param name="name">Name of the assembly to find</param>
        /// <returns>A representation of the assembly</returns>
        public static Assembly FindAssembly(string name)
        {
            try
            {
                Assembly a = Assembly.Load(new AssemblyName(name));

                return a;
            }
            catch (Exception)
            {
            }

            return null;
        }

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
                Assembly assembly = FindAssembly(assemblyName);
                if (assembly != null)
                {
                    foreach (var type in typeNames)
                    {
                        Type result = FindType(assembly, type);
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
                throw new Exception($"Unable to find {typeName}, are you missing {assemblyShortDescription}?");
            }

            return type;
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
            }

            return null;
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
    }
}
