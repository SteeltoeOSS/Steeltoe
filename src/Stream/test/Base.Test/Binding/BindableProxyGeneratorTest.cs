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

using Steeltoe.Stream.Messaging;
using System;
using System.Reflection;
using Xunit;

namespace Steeltoe.Stream.Binding
{
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

        private class TestBindableFactory : IBindableProxyFactory
        {
            public TestBindableFactory(Type binding)
            {
                BindingType = binding;
            }

            public MethodInfo Method { get; private set; }

            public Type BindingType { get; }

            public object Invoke(MethodInfo info)
            {
                Method = info;
                return null;
            }
        }
    }
}
