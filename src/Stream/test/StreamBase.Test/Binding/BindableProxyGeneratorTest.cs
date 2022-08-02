// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Stream.Messaging;
using Xunit;

namespace Steeltoe.Stream.Binding;

public class BindableProxyGeneratorTest
{
    [Fact]
    public void GenerateProxy_ThrowsOnNulls()
    {
        Assert.Throws<ArgumentNullException>(() => BindingProxyGenerator.GenerateProxy(null));
    }

    [Fact]
    public void GenerateProxy_ForISink_CreatesProxy_CallsFactory()
    {
        var bindableFactory = new TestBindableFactory(typeof(ISink));
        var proxy = BindingProxyGenerator.GenerateProxy(bindableFactory) as ISink;
        Assert.NotNull(proxy);
        _ = proxy.Input;
        Assert.NotNull(bindableFactory.Method);
        Assert.Equal("get_Input", bindableFactory.Method.Name);
    }

    [Fact]
    public void GenerateProxy_ForISource_CreatesProxy_CallsFactory()
    {
        var bindableFactory = new TestBindableFactory(typeof(ISource));
        var proxy = BindingProxyGenerator.GenerateProxy(bindableFactory) as ISource;
        Assert.NotNull(proxy);
        _ = proxy.Output;
        Assert.NotNull(bindableFactory.Method);
        Assert.Equal("get_Output", bindableFactory.Method.Name);
    }

    [Fact]
    public void GenerateProxy_ForIProcessor_CreatesProxy_CallsFactory()
    {
        var bindableFactory = new TestBindableFactory(typeof(IProcessor));
        var proxy = BindingProxyGenerator.GenerateProxy(bindableFactory) as IProcessor;
        Assert.NotNull(proxy);
        _ = proxy.Output;
        Assert.NotNull(bindableFactory.Method);
        Assert.Equal("get_Output", bindableFactory.Method.Name);
        _ = proxy.Input;
        Assert.NotNull(bindableFactory.Method);
        Assert.Equal("get_Input", bindableFactory.Method.Name);
    }

    [Fact]
    public void GenerateProxy_ForIBarista_CreatesProxy_CallsFactory()
    {
        var bindableFactory = new TestBindableFactory(typeof(IBarista));
        var proxy = BindingProxyGenerator.GenerateProxy(bindableFactory) as IBarista;
        Assert.NotNull(proxy);
        _ = proxy.ColdDrinks();
        Assert.NotNull(bindableFactory.Method);
        Assert.Equal("ColdDrinks", bindableFactory.Method.Name);
        _ = proxy.HotDrinks();
        Assert.NotNull(bindableFactory.Method);
        Assert.Equal("HotDrinks", bindableFactory.Method.Name);
        _ = proxy.Orders();
        Assert.NotNull(bindableFactory.Method);
        Assert.Equal("Orders", bindableFactory.Method.Name);
    }

    private sealed class TestBindableFactory : IBindableProxyFactory
    {
        public MethodInfo Method { get; private set; }

        public Type BindingType { get; }

        public TestBindableFactory(Type binding)
        {
            BindingType = binding;
        }

        public object Invoke(MethodInfo info)
        {
            Method = info;
            return null;
        }
    }
}
