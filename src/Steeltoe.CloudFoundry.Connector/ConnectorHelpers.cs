//
// Copyright 2015 the original author or authors.
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
//


using System;
using System.Reflection;
#if NET451
#else
using System.Runtime.Loader;
#endif

namespace Steeltoe.CloudFoundry.Connector
{
    public static class ConnectorHelpers
    {
        public static Assembly FindAssembly(string name) 
        {
            try {
#if NET451                
                Assembly a = Assembly.Load(new AssemblyName(name));
#else
                Assembly a = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(name));
#endif
                return a;
            } catch (Exception) {

            }
            return null;
        }

        public static Type FindType(string[] assemblyNames, string[] typeNames) 
        {
      
            for(int i = 0; i < assemblyNames.Length; i++) 
            {
                Assembly a = ConnectorHelpers.FindAssembly(assemblyNames[i]);
                if (a != null)
                {
                    Type result =  ConnectorHelpers.FindType(a, typeNames[i]);
                    if (result != null)
                        return result;
                }
            }
            return null;
        }
        public static Type FindType(Assembly assembly, string typeName) 
        {
            try {
                return assembly.GetType(typeName);
            } catch(Exception) {

            }
            return null;
        }

        public static object Invoke(MethodBase member, object instance, object[] args)
        {
            try {
                return member.Invoke(instance, args);
            } catch(Exception) {

            }
            return null;
        }

        public static object CreateInstance(Type t, object[] args)
        {
            try {
                return Activator.CreateInstance(t, args);
            } catch(Exception) {
        
            }
            return null;
        }

    }
}