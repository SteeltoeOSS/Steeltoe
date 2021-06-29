﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace Steeltoe.Stream.Binder
{
    public class DefaultBinderTypeRegistryTest : AbstractTest
    {
        [Fact]
        public void LoadAndCheckAssembly_WithValidPath_ReturnsBinderType()
        {
            var binderDir = GetSearchDirectories("TestBinder")[0];
            var paths = BuildPaths(binderDir);

            var context = new MetadataLoadContext(new PathAssemblyResolver(paths));
            var binderAssembly = binderDir + Path.DirectorySeparatorChar + "Steeltoe.Stream.TestBinder.dll";
            var result = DefaultBinderTypeRegistry.LoadAndCheckAssembly(context, binderAssembly);
            Assert.Equal(binderAssembly, result.AssemblyPath);
            Assert.Matches(@"Steeltoe.Stream.TestBinder.Startup, Steeltoe.Stream.TestBinder, Version=[\d.]+, Culture=neutral, PublicKeyToken=null", result.ConfigureClass);
            Assert.Equal("testbinder", result.Name);
            context.Dispose();
        }

        [Fact]
        public void LoadAndCheckAssembly_WithDefaultPath_ReturnsBinderType()
        {
            var binderDir = GetSearchDirectories("TestBinder")[0];
            var paths = BuildPaths(binderDir);

            var context = new MetadataLoadContext(new PathAssemblyResolver(paths));
            var binderAssembly = binderDir + Path.DirectorySeparatorChar + "Steeltoe.Stream.TestBinder.dll";
            var result = DefaultBinderTypeRegistry.LoadAndCheckAssembly(context, binderAssembly);
            Assert.Equal(binderAssembly, result.AssemblyPath);
            Assert.Matches(@"Steeltoe.Stream.TestBinder.Startup, Steeltoe.Stream.TestBinder, Version=[\d.]+, Culture=neutral, PublicKeyToken=null", result.ConfigureClass);
            Assert.Equal("testbinder", result.Name);
            context.Dispose();
        }

        [Fact]
        public void LoadAndCheckAssembly_WithInValidPath_DoesNotReturnsBinderType()
        {
            var paths = BuildPaths(null);
            var context = new MetadataLoadContext(new PathAssemblyResolver(paths));
            var binderAssembly = Environment.CurrentDirectory + Path.DirectorySeparatorChar + "Steeltoe.Stream.FooBar.dll";
            var result = DefaultBinderTypeRegistry.LoadAndCheckAssembly(context, binderAssembly);
            Assert.Null(result);
            context.Dispose();
        }

        [Fact]
        public void AddBinderTypes_WithValidDirectory_ReturnsBinder()
        {
            var binderDir = GetSearchDirectories("TestBinder")[0];
            var result = new Dictionary<string, IBinderType>();
            DefaultBinderTypeRegistry.AddBinderTypes(binderDir, result);
            Assert.Single(result);
            Assert.Matches(@"Steeltoe.Stream.TestBinder.Startup, Steeltoe.Stream.TestBinder, Version=[\d.]+, Culture=neutral, PublicKeyToken=null", result["testbinder"].ConfigureClass);
        }

        [Fact]
        public void AddBinderTypes_WithInValidDirectory_ReturnsNoBinders()
        {
            var result = new Dictionary<string, IBinderType>();
            DefaultBinderTypeRegistry.AddBinderTypes(Path.GetTempPath(), result);
            Assert.Empty(result);
        }

        [Fact]
        public void AddBinderTypes_WithBinderAllreadyLoaded_ReturnsBinder()
        {
            var paths = BuildPaths(null);

            var result = new Dictionary<string, IBinderType>();
            var binderPath = GetSearchDirectories("TestBinder")[0] + Path.DirectorySeparatorChar + "Steeltoe.Stream.TestBinder.dll";
            Assembly.LoadFrom(binderPath);

            DefaultBinderTypeRegistry.AddBinderTypes(AppDomain.CurrentDomain.GetAssemblies(), result);

            Assert.Single(result, r => r.Key == "testbinder");
            Assert.Matches(@"Steeltoe.Stream.TestBinder.Startup, Steeltoe.Stream.TestBinder, Version=[\d.]+, Culture=neutral, PublicKeyToken=null", result["testbinder"].ConfigureClass);
         }

        [Fact]
        public void ShouldCheckFile_ReturnsExpected()
        {
            var fileInfo = new FileInfo("Steeltoe.Stream.StreamBase.dll");
            Assert.False(DefaultBinderTypeRegistry.ShouldCheckFile(fileInfo));
            fileInfo = new FileInfo("foo.bar");
            Assert.True(DefaultBinderTypeRegistry.ShouldCheckFile(fileInfo));
        }

        [Fact]
        public void CheckAssembly_ReturnsExpected()
        {
            Assert.Null(DefaultBinderTypeRegistry.CheckAssembly(Assembly.GetExecutingAssembly()));
        }

        [Fact(Skip = "TypeRegistryTests")]
        public void ParseBinderConfigurations_ReturnsBinder()
        {
            var result = new Dictionary<string, IBinderType>();
            DefaultBinderTypeRegistry.AddBinderTypes(AppDomain.CurrentDomain.GetAssemblies(), result);
            Assert.Single(result);
            Assert.Matches(@"Steeltoe.Stream.TestBinder.Startup, Steeltoe.Stream.TestBinder, Version=[\d.]+, Culture=neutral, PublicKeyToken=null", result["testbinder"].ConfigureClass);
        }

        [Fact(Skip = "TypeRegistryTests")]
        public void FindBinders_ReturnsLoadedBinder()
        {
            var searchDirectories = new List<string>();
            var result = DefaultBinderTypeRegistry.FindBinders(searchDirectories);
            Assert.Single(result);
            Assert.Matches(@"Steeltoe.Stream.TestBinder.Startup, Steeltoe.Stream.TestBinder, Version=[\d.]+, Culture=neutral, PublicKeyToken=null", result["testbinder"].ConfigureClass);
        }

        [Fact]
        public void Constructor_FindsBinder()
        {
            var registry = new DefaultBinderTypeRegistry();
            Assert.Single(registry.GetAll(), r => r.Key == "testbinder");
            Assert.Matches(@"Steeltoe.Stream.TestBinder.Startup, Steeltoe.Stream.TestBinder, Version=[\d.]+, Culture=neutral, PublicKeyToken=null", registry.Get("testbinder").ConfigureClass);
        }

        private List<string> BuildPaths(string binderPath)
        {
            var paths = new List<string>();
            paths.AddRange(Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll"));
            if (!Environment.CurrentDirectory.Equals(binderPath, StringComparison.InvariantCultureIgnoreCase))
            {
                paths.AddRange(Directory.GetFiles(Environment.CurrentDirectory, "*.dll"));
            }

            return paths;
        }
    }
}
