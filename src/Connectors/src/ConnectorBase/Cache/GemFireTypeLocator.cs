﻿// Copyright 2017 the original author or authors.
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

using Steeltoe.Common.Reflection;
using System;
using System.Reflection;

namespace Steeltoe.Connector.GemFire
{
    public static class GemFireTypeLocator
    {
        public static string[] Assemblies { get; internal set; } = new string[] { "GemfireDotNet" };

        public static string[] CacheFactoryTypeNames { get; internal set; } = new string[] { "Apache.Geode.DotNetCore.CacheFactory" };

        public static string[] CacheTypeNames { get; internal set; } = new string[] { "Apache.Geode.DotNetCore.Cache" };

        public static string[] PoolFactoryTypeNames { get; internal set; } = new string[] { "Apache.Geode.DotNetCore.PoolFactory" };

        public static string[] RegionFactoryTypeNames { get; internal set; } = new string[] { "Apache.Geode.DotNetCore.RegionFactory" };

        public static string[] RegionShortcutTypeNames { get; internal set; } = new string[] { "Apache.Geode.DotNetCore.RegionShortcut" };

        public static Type CacheFactory => ReflectionHelpers.FindTypeOrThrow(Assemblies, CacheFactoryTypeNames, "CacheFactory", "the GemfireDotNet dll");

        public static MethodInfo CacheInitializer => ReflectionHelpers.FindMethod(CacheFactory, "CreateCache");

        public static MethodInfo CachePropertySetter => ReflectionHelpers.FindMethod(CacheFactory, "Set", new Type[] { typeof(string), typeof(string) });

        public static Type Cache => ReflectionHelpers.FindTypeOrThrow(Assemblies, CacheTypeNames, "Cache", "the GemfireDotNet dll");

        public static PropertyInfo GetPoolFactoryInitializer => GemFireTypeLocator.Cache.GetProperty("PoolFactory");

        public static Type PoolFactory => ReflectionHelpers.FindTypeOrThrow(Assemblies, PoolFactoryTypeNames, "PoolFactory", "the GemfireDotNet dll");

        public static MethodInfo AddLocatorToPoolFactory => ReflectionHelpers.FindMethod(PoolFactory, "AddLocator", new Type[] { typeof(string), typeof(int) });

        public static Type RegionFactory => ReflectionHelpers.FindTypeOrThrow(Assemblies, RegionFactoryTypeNames, "RegionFactory", "the GemfireDotNet dll");
        public static MethodInfo RegionFactoryInitializer => ReflectionHelpers.FindMethod(Cache, "CreateRegionFactory");

        public static Type RegionShortcutType => ReflectionHelpers.FindTypeOrThrow(Assemblies, RegionShortcutTypeNames, "RegionShortcut", "the GemfireDotNet dll");


        public static PropertyInfo GetCacheAuthInitializer(Type authInitializer)
        {
            return GemFireTypeLocator.CacheFactory.GetProperty("AuthInitialize");
        }
    }
}
