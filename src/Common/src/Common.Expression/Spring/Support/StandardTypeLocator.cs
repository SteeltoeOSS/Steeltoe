// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;

// using System.Runtime.Loader;
namespace Steeltoe.Common.Expression.Internal.Spring.Support
{
    public class StandardTypeLocator : ITypeLocator
    {
        // private readonly AssemblyLoadContext _assemblyLoadContext;
        private readonly List<string> _knownNamespacePrefixes = new List<string>(1);

        // public StandardTypeLocator(AssemblyLoadContext assemblyLoadContext = null)
        // {
        //    _assemblyLoadContext = assemblyLoadContext ?? AssemblyLoadContext.Default;
        //    RegisterNamespace("System");
        // }
        public StandardTypeLocator()
        {
            _knownNamespacePrefixes.Add("System");
        }

        public virtual void RegisterImport(string prefix)
        {
            _knownNamespacePrefixes.Add(prefix);
        }

        public virtual void RemoveImport(string prefix)
        {
            _knownNamespacePrefixes.Remove(prefix);
        }

        public virtual List<string> ImportPrefixes => new List<string>(_knownNamespacePrefixes);

        public virtual Type FindType(string typeName)
        {
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            Type result = null;
            typeName = typeName.Replace("$", "+"); // Handle nested type synatax  a.b.C$Nested
            foreach (var assembly in loadedAssemblies)
            {
                try
                {
                    result = assembly.GetType(typeName, false);
                    if (result == null)
                    {
                        foreach (var prefix in _knownNamespacePrefixes)
                        {
                            try
                            {
                                var nameToLookup = prefix + '.' + typeName;
                                result = assembly.GetType(nameToLookup, false);
                                if (result != null)
                                {
                                    break;
                                }
                            }
                            catch (Exception ex)
                            {
                                // might be a different prefix
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Eat exceptions
                }

                if (result != null)
                {
                    return result;
                }
            }

            throw new SpelEvaluationException(SpelMessage.TYPE_NOT_FOUND, typeName);
        }
    }
}
