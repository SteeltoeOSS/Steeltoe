// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Reflection;

namespace Steeltoe.CloudFoundry.Connector
{
    public static class ConnectorHelpers
    {
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

        public static Type FindType(string[] assemblyNames, string[] typeNames)
        {
            foreach (var assemblyName in assemblyNames)
            {
                Assembly assembly = ConnectorHelpers.FindAssembly(assemblyName);
                if (assembly != null)
                {
                    foreach (var type in typeNames)
                    {
                        Type result = ConnectorHelpers.FindType(assembly, type);
                        if (result != null)
                        {
                            return result;
                        }
                    }
                }
            }

            return null;
        }

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

        public static MethodInfo FindMethod(Type type, string methodName, Type[] parameters)
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

        public static object CreateInstance(Type t, object[] args)
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
    }
}