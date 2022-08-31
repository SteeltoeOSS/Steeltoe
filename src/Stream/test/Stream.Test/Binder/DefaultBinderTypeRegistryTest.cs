// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace Steeltoe.Stream.Binder;

public class DefaultBinderTypeRegistryTest : AbstractTest
{
    [Fact]
    public void LoadAndCheckAssembly_WithValidPath_ReturnsBinderType()
    {
        string binderDir = GetSearchDirectories("TestBinder")[0];
        List<string> paths = BuildPaths(binderDir);

        var context = new MetadataLoadContext(new PathAssemblyResolver(paths));
        string binderAssembly = $"{binderDir}{Path.DirectorySeparatorChar}Steeltoe.Stream.TestBinder.dll";
        IBinderType result = DefaultBinderTypeRegistry.LoadAndCheckAssembly(context, binderAssembly);
        Assert.Equal(binderAssembly, result.AssemblyPath);

        Assert.Matches(@"Steeltoe.Stream.TestBinder.Startup, Steeltoe.Stream.TestBinder, Version=[\d.]+, Culture=neutral, PublicKeyToken=null",
            result.ConfigureClass);

        Assert.Equal("testbinder", result.Name);
        context.Dispose();
    }

    [Fact]
    public void LoadAndCheckAssembly_WithInValidPath_DoesNotReturnsBinderType()
    {
        List<string> paths = BuildPaths(null);
        var context = new MetadataLoadContext(new PathAssemblyResolver(paths));
        string binderAssembly = $"{Environment.CurrentDirectory}{Path.DirectorySeparatorChar}Steeltoe.Stream.FooBar.dll";
        IBinderType result = DefaultBinderTypeRegistry.LoadAndCheckAssembly(context, binderAssembly);
        Assert.Null(result);
        context.Dispose();
    }

    [Fact]
    public void AddBinderTypes_WithValidDirectory_ReturnsBinder()
    {
        string binderDir = GetSearchDirectories("TestBinder")[0];
        var result = new Dictionary<string, IBinderType>();
        DefaultBinderTypeRegistry.AddBinderTypes(binderDir, result);
        Assert.Single(result);

        Assert.Matches(@"Steeltoe.Stream.TestBinder.Startup, Steeltoe.Stream.TestBinder, Version=[\d.]+, Culture=neutral, PublicKeyToken=null",
            result["testbinder"].ConfigureClass);
    }

    [Fact]
    public void AddBinderTypes_WithInValidDirectory_ReturnsNoBinders()
    {
        var result = new Dictionary<string, IBinderType>();
        DefaultBinderTypeRegistry.AddBinderTypes(Path.GetTempPath(), result);
        Assert.Empty(result);
    }

    [Fact]
    public void AddBinderTypes_WithBinderAlreadyLoaded_ReturnsBinder()
    {
        bool isAlreadyLoaded = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(a => a.FullName == "Steeltoe.Stream.TestBinder") != null;
        var result = new Dictionary<string, IBinderType>();

        DefaultBinderTypeRegistry.AddBinderTypes(AppDomain.CurrentDomain.GetAssemblies(), result);
        CheckExpectedResult(isAlreadyLoaded, result);
    }

    [Fact]
    public void ShouldCheckFile_ReturnsExpected()
    {
        var fileInfo = new FileInfo("Steeltoe.Stream.dll");
        Assert.False(DefaultBinderTypeRegistry.ShouldCheckFile(fileInfo));
        fileInfo = new FileInfo("foo.bar");
        Assert.True(DefaultBinderTypeRegistry.ShouldCheckFile(fileInfo));
    }

    [Fact]
    public void CheckAssembly_ReturnsExpected()
    {
        Assert.Null(DefaultBinderTypeRegistry.CheckAssembly(Assembly.GetExecutingAssembly()));
    }

    [Fact]
    public void ParseBinderConfigurations_ReturnsBinder()
    {
        List<string> binderDir = GetSearchDirectories("TestBinder");
        bool isAlreadyLoaded = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(a => a.FullName == "Steeltoe.Stream.TestBinder") != null;
        var result = new Dictionary<string, IBinderType>();
        DefaultBinderTypeRegistry.ParseBinderConfigurations(binderDir, result);
        CheckExpectedResult(isAlreadyLoaded, result);
    }

    [Fact]
    public void FindBinders_ReturnsLoadedBinder()
    {
        bool isAlreadyLoaded = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(a => a.FullName == "Steeltoe.Stream.TestBinder") != null;
        var searchDirectories = new List<string>();
        Dictionary<string, IBinderType> result = DefaultBinderTypeRegistry.FindBinders(searchDirectories);
        CheckExpectedResult(isAlreadyLoaded, result);
    }

    [Fact]
    public void Constructor_FindsBinder()
    {
        var registry = new DefaultBinderTypeRegistry();
        Assert.Single(registry.GetAll(), r => r.Key == "testbinder");

        Assert.Matches(@"Steeltoe.Stream.TestBinder.Startup, Steeltoe.Stream.TestBinder, Version=[\d.]+, Culture=neutral, PublicKeyToken=null",
            registry.Get("testbinder").ConfigureClass);
    }

    private void CheckExpectedResult(bool isAlreadyLoaded, Dictionary<string, IBinderType> results)
    {
        if (isAlreadyLoaded)
        {
            Assert.Matches(@"Steeltoe.Stream.TestBinder.Startup, Steeltoe.Stream.TestBinder, Version=[\d.]+, Culture=neutral, PublicKeyToken=null",
                results["testbinder"].ConfigureClass);
        }
    }

    private List<string> BuildPaths(string binderPath)
    {
        var paths = new List<string>();
        paths.AddRange(Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll"));

        if (!Environment.CurrentDirectory.Equals(binderPath, StringComparison.OrdinalIgnoreCase))
        {
            paths.AddRange(Directory.GetFiles(Environment.CurrentDirectory, "*.dll"));
        }

        return paths;
    }
}
