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

namespace Steeltoe.CloudFoundry.Connector.GemFire
{
    public class GemFireTypeLocator
    {
        public static string[] Assemblies = new string[] { "Pivotal.GemFire" };

        public static string[] CacheFactoryTypeNames = new string[] { "Apache.Geode.Client.CacheFactory" };

        public static string[] CacheTypeNames = new string[] { "Apache.Geode.Client.Cache" };

        public static string[] PoolFactoryTypeNames = new string[] { "Apache.Geode.Client.PoolFactory" };

        public static string[] RegionFactoryTypeNames = new string[] { "Apache.Geode.Client.RegionFactory" };

        public static Type CacheFactory => ConnectorHelpers.FindTypeOrThrow(Assemblies, CacheFactoryTypeNames, "CacheFactory", "the Pivotal GemFire dll");

        public static MethodInfo CacheInitializer => ConnectorHelpers.FindMethod(CacheFactory, "Create");

        public static MethodInfo CachePropertySetter => ConnectorHelpers.FindMethod(CacheFactory, "Set", new Type[] { typeof(string), typeof(string) });

        public static Type Cache => ConnectorHelpers.FindTypeOrThrow(Assemblies, CacheTypeNames, "Cache", "the Pivotal GemFire dll");

        public static MethodInfo PoolFactoryInitializer => ConnectorHelpers.FindMethod(Cache, "GetPoolFactory");

        public static Type PoolFactory => ConnectorHelpers.FindTypeOrThrow(Assemblies, PoolFactoryTypeNames, "PoolFactory", "the Pivotal GemFire dll");

        public static MethodInfo AddLocatorToPoolFactory => ConnectorHelpers.FindMethod(PoolFactory, "AddLocator", new Type[] { typeof(string), typeof(int) });

        public static Type RegionFactory => ConnectorHelpers.FindTypeOrThrow(Assemblies, RegionFactoryTypeNames, "RegionFactory", "the Pivotal GemFire dll");

        public static MethodInfo GetCacheAuthInitializer(Type authInitializer)
        {
            return ConnectorHelpers.FindMethod(CacheFactory, "SetAuthInitialize", new Type[] { authInitializer.GetInterface("IAuthInitialize") });
        }
    }
}
