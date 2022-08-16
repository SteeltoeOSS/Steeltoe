// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Steeltoe.Common.Expression.Internal.Spring.Support;

public class StandardTypeLocator : ITypeLocator
{
    private readonly IList<string> _knownNamespacePrefixes = new List<string>(1);

    public virtual IList<string> ImportPrefixes => new List<string>(_knownNamespacePrefixes);

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

    public virtual Type FindType(string typeName)
    {
        Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        Type result = null;
        typeName = typeName.Replace("$", "+"); // Handle nested type syntax  a.b.C$Nested

        foreach (Assembly assembly in loadedAssemblies)
        {
            try
            {
                result = assembly.GetType(typeName, false);

                if (result == null)
                {
                    foreach (string prefix in _knownNamespacePrefixes)
                    {
                        try
                        {
                            string nameToLookup = $"{prefix}.{typeName}";
                            result = assembly.GetType(nameToLookup, false);

                            if (result != null)
                            {
                                break;
                            }
                        }
                        catch (Exception)
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

        throw new SpelEvaluationException(SpelMessage.TypeNotFound, typeName);
    }
}
