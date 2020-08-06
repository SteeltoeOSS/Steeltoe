// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Xunit;

namespace Steeltoe.Stream.Binder
{
    public class DefaultBinderTypeRegistryTest
    {
        [Fact(Skip = "TypeRegistryTests")]
        public void LoadAndCheckAssembly_WithValidPath_ReturnsBinderType()
        {
            var context = new DefaultBinderTypeRegistry.SearchingAssemblyLoadContext();
            var path = Environment.CurrentDirectory + Path.DirectorySeparatorChar + "Steeltoe.Stream.TestBinder.dll";
            var result = DefaultBinderTypeRegistry.LoadAndCheckAssembly(context, path);
            Assert.Equal(path, result.AssemblyPath);
            Assert.Equal("Steeltoe.Stream.TestBinder.Startup, Steeltoe.Stream.TestBinder, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", result.ConfigureClass);
            Assert.Equal("testbinder", result.Name);
            context.Unload();
        }

        [Fact(Skip = "TypeRegistryTests")]
        public void LoadAndCheckAssembly_WithInValidPath_ReturnsBinderType()
        {
            var context = new DefaultBinderTypeRegistry.SearchingAssemblyLoadContext();
            var path = Environment.CurrentDirectory + Path.DirectorySeparatorChar + "Steeltoe.Stream.FooBar.dll";
            var result = DefaultBinderTypeRegistry.LoadAndCheckAssembly(context, path);
            Assert.Null(result);
            context.Unload();
        }

        [Fact(Skip = "TypeRegistryTests")]
        public void AddBinderTypes_WithValidDirectory_ReturnsBinder()
        {
            var result = new Dictionary<string, IBinderType>();
            DefaultBinderTypeRegistry.AddBinderTypes(Environment.CurrentDirectory, result);
            Assert.Single(result);
            Assert.Equal("Steeltoe.Stream.TestBinder.Startup, Steeltoe.Stream.TestBinder, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", result["testbinder"].ConfigureClass);
        }

        [Fact(Skip = "TypeRegistryTests")]
        public void AddBinderTypes_WithInValidDirectory_ReturnsBinder()
        {
            var result = new Dictionary<string, IBinderType>();
            DefaultBinderTypeRegistry.AddBinderTypes(Path.GetTempPath(), result);
            Assert.Empty(result);
        }

        [Fact(Skip = "TypeRegistryTests")]
        public void AddBinderTypes_WithBinderAllreadyLoaded_ReturnsBinder()
        {
            var result = new Dictionary<string, IBinderType>();
            DefaultBinderTypeRegistry.AddBinderTypes(AppDomain.CurrentDomain.GetAssemblies(), result);
            Assert.Single(result);
            Assert.Equal("Steeltoe.Stream.TestBinder.Startup, Steeltoe.Stream.TestBinder, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", result["testbinder"].ConfigureClass);
        }

        [Fact(Skip = "TypeRegistryTests")]
        public void ShouldCheckFile_ReturnsExpected()
        {
            var fileInfo = new FileInfo("Steeltoe.Stream.Base.dll");
            Assert.False(DefaultBinderTypeRegistry.ShouldCheckFile(fileInfo));
            fileInfo = new FileInfo("foo.bar");
            Assert.True(DefaultBinderTypeRegistry.ShouldCheckFile(fileInfo));
        }

        [Fact(Skip = "TypeRegistryTests")]
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
            Assert.Equal("Steeltoe.Stream.TestBinder.Startup, Steeltoe.Stream.TestBinder, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", result["testbinder"].ConfigureClass);
        }

        [Fact(Skip = "TypeRegistryTests")]
        public void FindBinders_ReturnsLoadedBinder()
        {
            var searchDirectories = new List<string>();
            var result = DefaultBinderTypeRegistry.FindBinders(searchDirectories);
            Assert.Single(result);
            Assert.Equal("Steeltoe.Stream.TestBinder.Startup, Steeltoe.Stream.TestBinder, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", result["testbinder"].ConfigureClass);
        }

        [Fact(Skip = "TypeRegistryTests")]
        public void Constructor_FindsBinder()
        {
            var registry = new DefaultBinderTypeRegistry();
            Assert.Single(registry.GetAll());
            Assert.Equal("Steeltoe.Stream.TestBinder.Startup, Steeltoe.Stream.TestBinder, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", registry.Get("testbinder").ConfigureClass);
        }
    }
}
