// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;

namespace Steeltoe.CloudFoundry.Connector.GemFire
{
    public static class GemFireTypeLocator
    {
        public static string[] Assemblies { get; internal set; } = new string[] { "Pivotal.GemFire" };

        public static string[] CacheFactoryTypeNames { get; internal set; } = new string[] { "Apache.Geode.Client.CacheFactory" };

        public static string[] CacheTypeNames { get; internal set; } = new string[] { "Apache.Geode.Client.Cache" };

        public static string[] PoolFactoryTypeNames { get; internal set; } = new string[] { "Apache.Geode.Client.PoolFactory" };

        public static string[] RegionFactoryTypeNames { get; internal set; } = new string[] { "Apache.Geode.Client.RegionFactory" };

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
