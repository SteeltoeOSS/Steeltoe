// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Castle.DynamicProxy;
using System;
using System.Reflection;

namespace Steeltoe.Stream.Binding
{
    public class BindingProxyGenerator
    {
        public static object CreateProxy(IBindableProxyFactory factory)
        {
            return GenerateProxy(factory);
        }

        internal static object GenerateProxy(IBindableProxyFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            var generator = new ProxyGenerator();
            Func<MethodInfo, object> del = (m) => factory.Invoke(m);
            var proxy = generator.CreateInterfaceProxyWithoutTarget(factory.BindingType, new BindingInterceptor(del));
            return proxy;
        }

        private class BindingInterceptor : IInterceptor
        {
            private readonly Delegate _impl;

            public BindingInterceptor(Delegate impl)
            {
                _impl = impl;
            }

            public void Intercept(IInvocation invocation)
            {
                var result = _impl.DynamicInvoke(invocation.Method);
                invocation.ReturnValue = result;
            }
        }
    }
}
